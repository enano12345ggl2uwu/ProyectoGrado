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
///   3. Canvas con: wordText, scoreText, feedbackText, countdownText, holdBar.
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
    public HoldFillBar     holdBar;

    [Header("Referencias")]
    public SizeContourDisplay contour;

    [Header("Config base")]
    public float roundTime    = 8f;
    public float holdTime     = 1.2f;
    public float feedbackTime = 1.8f;

    [Header("Session")]
    [Tooltip("Cantidad de rondas antes de mostrar el panel final.")]
    public int totalRounds = 6;
    [Tooltip("Panel final. Arrastra el GameObject con ResultsScreen.")]
    public ResultsScreen results;

    [Header("Escala live (norm -> unidades del mundo)")]
    [Tooltip("Factor que convierte (distancia / shoulderWidth) en unidades visuales.")]
    public float worldScale = 1.0f;

    [Header("Tolerancia")]
    [Tooltip("Porcentaje aceptado para ancho (wrist-wrist). 0.18 = +/-18%.")]
    public float widthTolerance  = 0.18f;
    [Tooltip("Porcentaje aceptado para alto (nariz-cadera). Mayor porque es mas dificil de controlar verticalmente.")]
    public float heightTolerance = 0.30f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    // Vocabulario: dimensiones normalizadas (/ shoulderWidth).
    // width = distancia muñeca-muñeca / sw.  Relajado ~2.0, brazos muy abiertos ~3.2.
    // height = distancia nariz-cadera / sw.   De pie normal ~2.0, agachado ~1.1, muy estirado ~2.7.
    private readonly TargetSize[] targets =
    {
        new TargetSize("TALL",    2.0f, 2.7f),   // de pie muy recto / de puntillas
        new TargetSize("SHORT",   2.0f, 1.1f),   // agacharse bien
        new TargetSize("WIDE",    3.2f, 2.0f),   // brazos muy abiertos, altura normal
        new TargetSize("NARROW",  1.0f, 2.0f),   // brazos pegados al cuerpo
        new TargetSize("BIG",     3.0f, 2.6f),   // brazos abiertos Y estirado
        new TargetSize("SMALL",   1.0f, 1.1f),   // brazos pegados Y agachado
    };

    private int    score        = 0;
    private int    currentIdx   = 0;
    private int    lastIdx      = -1;
    private bool   roundActive  = false;
    private float  holdTimer    = 0f;
    private float  tolMult      = 1f;
    private int    _roundsPlayed = 0;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        if (holdBar)  holdBar.ResetBar();
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
                widthTolerance  = 0.28f;
                heightTolerance = 0.38f;
                tolMult        = 1.5f;
                break;
            case 2: // Hard
                holdTime       = 0.9f;
                roundTime      = 6f;
                widthTolerance  = 0.14f;
                heightTolerance = 0.24f;
                tolMult        = 0.75f;
                break;
            default: // Medium
                holdTime       = 1.2f;
                roundTime      = 8f;
                widthTolerance  = 0.18f;
                heightTolerance = 0.30f;
                tolMult        = 1f;
                break;
        }
    }

    IEnumerator GameLoop()
    {
        _roundsPlayed = 0;
        while (_roundsPlayed < totalRounds)
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
                    if (holdBar) holdBar.SetProgress(holdTimer / holdTime);

                    if (feedbackText)
                    {
                        feedbackText.text  = "HOLD IT!";
                        feedbackText.color = UITheme.Success;
                    }
                    if (wordText) wordText.color = Color.Lerp(Color.white, UITheme.Success, holdTimer / holdTime);

                    if (holdTimer >= holdTime)
                    {
                        EvaluateCorrect();
                        roundActive = false;
                    }
                }
                else
                {
                    holdTimer = 0f;
                    if (holdBar)  holdBar.ResetBar();
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

            if (holdBar) holdBar.ResetBar();
            if (wordText)    wordText.color = Color.white;

            _roundsPlayed++;
            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
        }

        if (countdownText) countdownText.text = "";
        if (results != null)
            results.Show(score, _roundsPlayed, totalRounds * 10);
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

        bool okW = Mathf.Abs(widthNorm  - t.width)  / Mathf.Max(0.1f, t.width)  <= widthTolerance  * tolMult;
        bool okH = Mathf.Abs(heightNorm - t.height) / Mathf.Max(0.1f, t.height) <= heightTolerance * tolMult;
        bool matching = okW && okH;

        contour.SetLiveSize(widthNorm * worldScale, heightNorm * worldScale, matching);
        return matching;
    }

    void EvaluateCorrect()
    {
        score += 10;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
        UpdateScoreUI();
        ShowFeedback("Perfect!", UITheme.Success);
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
