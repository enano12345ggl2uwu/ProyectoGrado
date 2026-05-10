using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Minijuego: Balloon Pop.
/// Globos suben desde abajo por los laterales (nunca en el centro donde esta el jugador).
/// El jugador extiende los brazos para reventar los globos del color pedido.
///
/// SETUP en Unity:
///  1. GameObject "BalloonManager" + este script + AudioSource.
///  2. Prefab de globo: Sphere con SphereCollider radio 0.5 + script Balloon.
///  3. Asignar en Inspector: balloonPrefab, stickFigure, textos UI.
///  4. DifficultySelector.balloonPopGame -> este componente.
/// </summary>
public class BalloonPopGameUDP : MonoBehaviour
{
    public enum DifficultyMode { Easy, Medium, Hard }

    [Header("Prefab & Spawn")]
    public GameObject     balloonPrefab;
    public StickFigureUDP stickFigure;

    [Tooltip("Mitad del ancho total donde pueden aparecer globos (unidades mundo).")]
    public float spawnXRange  = 2.0f;
    [Tooltip("Zona muerta central (0 a 1 como fraccion de spawnXRange). 0.35 = 35% del centro libre.")]
    public float deadZoneFrac = 0.35f;

    public float spawnStartY  = -3f;
    public float despawnY     =  5f;
    public float floatUpSpeed =  0.8f;
    public float spawnInterval = 2.5f;
    public float popRadius    =  1.8f;
    public float balloonScale =  4.0f;

    [Header("UI")]
    public TextMeshProUGUI targetColorText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Game")]
    public float          totalGameTime    = 60f;
    public DifficultyMode difficulty       = DifficultyMode.Medium;

    [Header("Session")]
    public ResultsScreen results;
    public int            expectedMaxScore = 150;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;

    private readonly string[] colorNames  = { "RED", "BLUE", "GREEN", "YELLOW", "ORANGE", "PURPLE" };
    private readonly Color[]  colorValues = UITheme.GameColors;

    // Un globo por lado — nunca en el centro
    private Balloon _leftBalloon;
    private Balloon _rightBalloon;

    private int   _score        = 0;
    private int   _targetColorIdx;
    private int   _activeColors;
    private bool  _running      = false;
    private float _wrongPenalty = 5f;

    void Start()
    {
        if (feedbackText)    feedbackText.text    = "";
        if (targetColorText) targetColorText.text = "";
        if (countdownText)   countdownText.text   = "";
        if (scoreText)       scoreText.text       = "";
        UpdateScoreUI();
    }

    public void StartGame(int level)
    {
        difficulty = (DifficultyMode)level;
        ApplyDifficulty();
        CalcSpawnBounds();
        _running = true;
        StartCoroutine(GameLoop());
        StartCoroutine(SpawnLoop());
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyMode.Easy:
                _activeColors  = 3;
                floatUpSpeed   = 0.6f;
                spawnInterval  = 3.0f;
                balloonScale   = 4.5f;
                popRadius      = 2.0f;
                _wrongPenalty  = 0f;
                break;
            case DifficultyMode.Hard:
                _activeColors  = colorNames.Length;
                floatUpSpeed   = 1.4f;
                spawnInterval  = 1.6f;
                balloonScale   = 3.0f;
                popRadius      = 1.5f;
                _wrongPenalty  = 10f;
                break;
            default: // Medium
                _activeColors  = 4;
                floatUpSpeed   = 0.8f;
                spawnInterval  = 2.5f;
                balloonScale   = 4.0f;
                popRadius      = 1.8f;
                _wrongPenalty  = 5f;
                break;
        }
        PickNewTarget();
    }

    void CalcSpawnBounds()
    {
        // Usar la cámara para saber los bordes reales de pantalla en world space
        Camera cam = Camera.main;
        if (cam != null)
        {
            float zDist   = Mathf.Abs(cam.transform.position.z - 0f);
            if (zDist < 1f) zDist = 10f;

            Vector3 botCenter  = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f,  zDist));
            Vector3 topCenter  = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f,  zDist));
            Vector3 leftEdge   = cam.ViewportToWorldPoint(new Vector3(0f,   0.5f, zDist));
            Vector3 rightEdge  = cam.ViewportToWorldPoint(new Vector3(1f,   0.5f, zDist));

            spawnStartY = botCenter.y  - 1f;          // 1u bajo el borde inferior
            despawnY    = topCenter.y  + 1f;          // 1u sobre el borde superior
            spawnXRange = (rightEdge.x - leftEdge.x) * 0.38f; // 38% del ancho de pantalla
        }
        else if (stickFigure != null)
        {
            float s = stickFigure.scale;
            float oy = stickFigure.offset.y;
            spawnXRange = s * 0.45f;
            spawnStartY = oy - s * 0.5f;
            despawnY    = oy + s * 0.7f;
        }
    }

    void PickNewTarget()
    {
        _targetColorIdx = Random.Range(0, _activeColors);
        if (targetColorText)
        {
            targetColorText.text  = $"POP {colorNames[_targetColorIdx]}!";
            targetColorText.color = colorValues[_targetColorIdx];
        }
    }

    // -------------------------------------------------------------------------
    // Loops
    // -------------------------------------------------------------------------

    IEnumerator GameLoop()
    {
        float timer        = totalGameTime;
        float switchTimer  = 7f;

        while (_running && timer > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
            CheckHandPops();
            switchTimer -= Time.deltaTime;
            if (switchTimer <= 0f) { PickNewTarget(); switchTimer = 7f; }
            timer -= Time.deltaTime;
            yield return null;
        }

        _running = false;
        EndGame();
    }

    IEnumerator SpawnLoop()
    {
        // Pequeño delay inicial para que el canvas termine de cargar
        yield return new WaitForSeconds(0.5f);

        while (_running)
        {
            // Reponer lado izquierdo si está vacío o salió de pantalla
            if (_leftBalloon == null || _leftBalloon.OffScreen)
            {
                if (_leftBalloon != null) Destroy(_leftBalloon.gameObject);
                _leftBalloon = SpawnSide(left: true);
            }

            // Reponer lado derecho
            if (_rightBalloon == null || _rightBalloon.OffScreen)
            {
                if (_rightBalloon != null) Destroy(_rightBalloon.gameObject);
                _rightBalloon = SpawnSide(left: false);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // -------------------------------------------------------------------------
    // Spawn lateral
    // -------------------------------------------------------------------------

    Balloon SpawnSide(bool left)
    {
        if (balloonPrefab == null) return null;

        float dead = spawnXRange * deadZoneFrac;

        // X aleatoria dentro del lateral correspondiente (fuera de la zona muerta)
        float x = left
            ? Random.Range(-spawnXRange, -dead)
            :  Random.Range( dead,        spawnXRange);

        Vector3 pos = new Vector3(x, spawnStartY, 0f);
        GameObject go = Instantiate(balloonPrefab, pos, Quaternion.identity);
        go.transform.localScale = Vector3.one * balloonScale;

        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var col = go.GetComponent<Collider>();
        if (col == null) { var sc = go.AddComponent<SphereCollider>(); sc.radius = 0.5f; col = sc; }
        col.isTrigger = true;

        var b = go.GetComponent<Balloon>() ?? go.AddComponent<Balloon>();
        int colorIdx = Random.Range(0, _activeColors);
        b.Init(colorIdx, colorValues[colorIdx], floatUpSpeed, despawnY);
        return b;
    }

    // -------------------------------------------------------------------------
    // Detección manos
    // -------------------------------------------------------------------------

    void CheckHandPops()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;
        TryPop(LandmarkToWorld(15));
        TryPop(LandmarkToWorld(16));
    }

    void TryPop(Vector3 wristPos)
    {
        Collider[] hits = Physics.OverlapSphere(wristPos, popRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            Balloon b = hit.GetComponent<Balloon>();
            if (b == null) continue;
            if (b == _leftBalloon)  { PopBalloon(b); _leftBalloon  = null; return; }
            if (b == _rightBalloon) { PopBalloon(b); _rightBalloon = null; return; }
        }
    }

    Vector3 LandmarkToWorld(int idx)
    {
        Vector3 lm  = PoseReceiverUDP.Instance.GetLandmark(idx);
        float   s   = stickFigure != null ? stickFigure.scale  : 5f;
        Vector3 off = stickFigure != null ? stickFigure.offset : new Vector3(0f, 2f, 0f);
        return new Vector3((lm.x - 0.5f) * s, (0.5f - lm.y) * s + off.y, 0f);
    }

    // -------------------------------------------------------------------------
    // Pop & fin
    // -------------------------------------------------------------------------

    void PopBalloon(Balloon b)
    {
        bool correct = b.ColorIndex == _targetColorIdx;
        if (correct)
        {
            _score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            ShowFeedback("Great!", UITheme.Success);
            PlayClip(popClip);
            if (CelebrationBurst.Instance != null)
                CelebrationBurst.Instance.Trigger(b.transform.position);
        }
        else
        {
            _score = Mathf.Max(0, _score - (int)_wrongPenalty);
            ShowFeedback("Wrong color!", UITheme.Warning);
            PlayClip(wrongClip);
        }
        UpdateScoreUI();
        Destroy(b.gameObject);
    }

    void EndGame()
    {
        if (countdownText) countdownText.text = "";
        if (_leftBalloon)  Destroy(_leftBalloon.gameObject);
        if (_rightBalloon) Destroy(_rightBalloon.gameObject);
        _leftBalloon = _rightBalloon = null;

        if (results != null)
            results.Show(_score, Mathf.RoundToInt(totalGameTime), expectedMaxScore);
        else
            ShowFeedback($"Final: {_score} pts", Color.cyan);
    }

    void UpdateScoreUI() { if (scoreText) scoreText.text = $"Score: {_score}"; }

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText) { feedbackText.text = msg; feedbackText.color = color; }
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}
