using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego: Size Sort.
/// Se muestra una palabra en ingles (TALL, SHORT, WIDE, NARROW, BIG, SMALL) y un rectangulo
/// objetivo (contorno gris). El niño ajusta su cuerpo:
///   - WIDTH   = envergadura de brazos (distancia entre muñecas / hombros)
///   - HEIGHT  = distancia vertical entre nariz y caderas (estirado vs agachado)
/// Un segundo rectangulo "vivo" refleja sus dimensiones actuales. Cuando ambas coinciden
/// con el objetivo (tolerancia), se pintan verde, se mantiene HOLD y se da por correcta.
///
/// Landmarks: 0 (nariz), 11/12 (hombros), 15/16 (muñecas), 23/24 (caderas).
///
/// Setup minimo en Unity:
///   1. GameObject "SizeSortManager" con este script.
///   2. GameObject "SizeContour" con SizeContourDisplay.cs en posicion (0, 2, 0).
///   3. Canvas con: wordText, scoreText, feedbackText, countdownText, holdFillBar.
///   4. Arrastra SizeContourDisplay al campo "contour" y los textos a los suyos.
///   5. Opcional: DifficultySelector con boton que llame SizeSortGameUDP.StartGame(level).
/// </summary>
public class SizeSortGameUDP : MonoBehaviour
{
    enum Target { Tall, Short, Wide, Narrow, Big, Small }

    struct TargetSize
    {
        public string name;
        public float  width;   // ancho (en "unidades" donde 1.0 = shoulderWidth * scale)
        public float  height;
        public TargetSize(string n, float w, float h) { name = n; width = w; height = h; }
    }

    [Header("UI")]
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;
    public Image           holdFillBar;

    [Header("Referencias")]
    public SizeContourDisplay contour;

    [Header("Config base")]
    public float roundTime    = 8f;
    public float holdTime     = 1.2f;
    public float feedbackTime = 1.8f;

    [Header("Escala live (norm -> unidades del mundo)")]
    [Tooltip("Factor que convierte (distancia / shoulderWidth) en unidades visuales.")]
    public float worldScale = 0.6f;

    [Header("Tolerancia")]
    [Tooltip("Porcentaje aceptado como 'match'. 0.18 = +/-18% en ancho Y alto.")]
    public float matchTolerance = 0.18f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    [Header("Colores feedback")]
    public Color colorOk  = new Color(0.2f, 1f, 0.3f, 1f);
    public Color colorBad = new Color(1f, 0.2f, 0.25f, 1f);

    // Vocabulario: dimensiones en "unidades normalizadas" (x shoulderWidth).
    // width ~= armSpan/sw, height ~= (nose-hipMid)/sw. Shoulder-width ≈ 1.0 de base.
    // Typical armSpan en niño relajado ~= 2.2*sw. Typical (nose-hip) ~= 2.8*sw de pie.
    private readonly TargetSize[] targets =
    {
        new TargetSize("TALL",    2.0f, 3.4f),
        new TargetSize("SHORT",   2.0f, 1.4f),
        new TargetSize("WIDE",    3.4f, 2.4f),
        new TargetSize("NARROW",  1.0f, 2.4f),
        new TargetSize("BIG",     3.0f, 3.2f),
        new TargetSize("SMALL",   1.2f, 1.4f),
    };

    private int    score        = 0;
    private int    currentIdx   = 0;
    private int    lastIdx      = -1;
    private bool   roundActive  = false;
    private float  holdTimer    = 0f;
    private float  tolMult      = 1f;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        if (holdFillBar)  holdFillBar.fillAmount = 0f;
        UpdateScoreUI();

        if (FindObjectOfType<DifficultySelector>() == null)
            StartGame(1);
    }

    /// <summary>Llamado por DifficultySelector. level: 0=Easy 1=Medium 2=Hard</summary>
    public void StartGame(int level)
    {
        ApplyDifficulty(level);
        StartCoroutine(GameLoop());
    }

    void ApplyDifficulty(int level)
    {
        switch (level)
        {
            case 0: // Easy
                holdTime       = 1.8f;
                roundTime      = 10f;
                matchTolerance = 0.25f;
                tolMult        = 1.5f;
                break;
            case 2: // Hard
                holdTime       = 0.9f;
                roundTime      = 6f;
                matchTolerance = 0.12f;
                tolMult        = 0.75f;
                break;
            default: // Medium
                holdTime       = 1.2f;
                roundTime      = 8f;
                matchTolerance = 0.18f;
                tolMult        = 1f;
                break;
        }
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            SetupRound();
            float timer = roundTime;
            roundActive = true;
            holdTimer   = 0f;

            while (timer > 0f && roundActive)
            {
                if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();

                bool matching = UpdateContour();

                if (matching)
                {
                    holdTimer += Time.deltaTime;
                    if (holdFillBar) holdFillBar.fillAmount = holdTimer / holdTime;

                    if (feedbackText)
                    {
                        feedbackText.text  = "HOLD IT!";
                        feedbackText.color = colorOk;
                    }
                    if (wordText) wordText.color = Color.Lerp(Color.white, colorOk, holdTimer / holdTime);

                    if (holdTimer >= holdTime)
                    {
                        EvaluateCorrect();
                        roundActive = false;
                    }
                }
                else
                {
                    holdTimer = 0f;
                    if (holdFillBar)  holdFillBar.fillAmount = 0f;
                    if (feedbackText) feedbackText.text = "";
                    if (wordText)     wordText.color = Color.white;
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            if (roundActive)
            {
                ShowFeedback("Try again!", Color.white);
                PlayClip(wrongClip);
                roundActive = false;
            }

            if (holdFillBar) holdFillBar.fillAmount = 0f;
            if (wordText)    wordText.color = Color.white;

            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
        }
    }

    void SetupRound()
    {
        int idx;
        do { idx = Random.Range(0, targets.Length); } while (idx == lastIdx);
        lastIdx    = idx;
        currentIdx = idx;

        var t = targets[idx];
        if (wordText) { wordText.text = t.name; wordText.color = Color.white; }
        if (feedbackText) feedbackText.text = "";
        holdTimer = 0f;

        if (contour) contour.SetTargetSize(t.width * worldScale, t.height * worldScale);
    }

    /// <summary>Actualiza rectangulo live y devuelve true si match en width Y height.</summary>
    bool UpdateContour()
    {
        if (contour == null) return false;
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return false;

        var I = PoseReceiverUDP.Instance;
        float sw = Vector3.Distance(I.GetLandmark(11), I.GetLandmark(12));
        if (sw < 0.05f) return false;

        // Width = envergadura de brazos (wrist-wrist) normalizada
        float widthNorm = Vector3.Distance(I.GetLandmark(15), I.GetLandmark(16)) / sw;

        // Height = nariz -> punto medio de caderas, normalizado
        Vector3 midHip  = (I.GetLandmark(23) + I.GetLandmark(24)) * 0.5f;
        float heightNorm = Mathf.Abs(I.GetLandmark(0).y - midHip.y) / sw;

        var t = targets[currentIdx];
        float tol = matchTolerance * tolMult;

        bool okW = Mathf.Abs(widthNorm  - t.width)  / Mathf.Max(0.1f, t.width)  <= tol;
        bool okH = Mathf.Abs(heightNorm - t.height) / Mathf.Max(0.1f, t.height) <= tol;
        bool matching = okW && okH;

        contour.SetLiveSize(widthNorm * worldScale, heightNorm * worldScale, matching);
        return matching;
    }

    void EvaluateCorrect()
    {
        score += 10;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
        UpdateScoreUI();
        ShowFeedback("Perfect!", colorOk);
        PlayClip(correctClip);

        if (CelebrationBurst.Instance != null)
            CelebrationBurst.Instance.Trigger(transform.position);
    }

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText) { feedbackText.text = msg; feedbackText.color = color; }
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    public int  Score => score;
    public void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}
