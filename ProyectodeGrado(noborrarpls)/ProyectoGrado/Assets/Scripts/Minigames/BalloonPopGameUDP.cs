using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Minijuego 3: Balloon Pop.
/// Globos suben desde abajo con colores. En pantalla aparece un color objetivo.
/// El jugador "pop" los globos del color correcto tocando con sus manos (munecas, landmarks 15 y 16).
/// Globos del color incorrecto restan puntos o se ignoran segun dificultad.
///
/// SETUP en Unity (Island o escena propia):
///  1. Crea un GameObject "BalloonManager" en la escena y pone este script.
///  2. Crea un prefab de globo:
///       - Sphere 3D o Sprite circular con escala ~0.6
///       - Tag "Balloon" (opcional)
///       - Script Balloon (ver abajo)
///       - Rigidbody? NO, este script lo mueve manualmente.
///       - Collider: SphereCollider (trigger OFF, radio 0.5)
///  3. Asigna en Inspector:
///       balloonPrefab     -> tu prefab
///       spawnArea         -> Transform que marca el centro horizontal donde aparecen (Y bajo)
///       targetColorText   -> TMP que muestra "POP RED!"
///       scoreText/feedbackText/countdownText -> TMP de UI
///       audioSource/popClip/wrongClip
///  4. DifficultySelector.balloonPopGame -> este GameObject, y llama StartGame(level) al presionar START.
///  5. StickFigureUDP debe existir en escena (las munecas dibujadas son las que tocan).
///
/// Interaccion: cada frame se compara la posicion mundial de cada globo con las munecas (landmarks 15 y 16).
/// Si la distancia < popRadius → globo "poped". Si era del color objetivo +10 puntos; si no, -5.
///
/// Dificultad:
///   Easy    — 3 colores activos, spawn cada 2.0s, sube a velocidad 1.5, globos rojos extra tiempo.
///   Medium  — 4 colores, spawn 1.4s, velocidad 2.0.
///   Hard    — 6 colores, spawn 0.9s, velocidad 2.8, restan mas puntos al fallar.
/// </summary>
public class BalloonPopGameUDP : MonoBehaviour
{
    public enum DifficultyMode { Easy, Medium, Hard }

    [Header("Prefab & Spawn")]
    public GameObject    balloonPrefab;
    public Transform     spawnArea;
    public StickFigureUDP stickFigure;
    public float         spawnXRange   = 2.8f;
    public float         spawnStartY   = -6f;
    public float         floatUpSpeed  = 1f;
    public float         spawnInterval = 1.4f;
    public float         despawnY      = 6f;
    public float         popRadius     = 1.2f;
    public float         balloonScale  = 2.5f;

    [Header("UI")]
    public TextMeshProUGUI targetColorText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Game")]
    public float totalGameTime = 60f;
    public DifficultyMode difficulty = DifficultyMode.Medium;

    [Header("Session")]
    [Tooltip("Panel final. Arrastra el GameObject con ResultsScreen.")]
    public ResultsScreen results;
    [Tooltip("Score esperado (para calcular estrellas). 0 = se ignora.")]
    public int expectedMaxScore = 200;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;

    private readonly string[] colorNames = { "RED", "BLUE", "GREEN", "YELLOW", "ORANGE", "PURPLE" };
    private readonly Color[]  colorValues = UITheme.GameColors;

    private Balloon _leftBalloon;
    private Balloon _rightBalloon;
    private int   _score = 0;
    private int   _targetColorIdx;
    private int   _activeColors;
    private bool  _running = false;
    private float _wrongPenalty = 5f;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        UpdateScoreUI();
    }

    /// <summary>Llamado por DifficultySelector. level: 0=Easy 1=Medium 2=Hard</summary>
    public void StartGame(int level)
    {
        difficulty = (DifficultyMode)level;
        ApplyDifficulty();
        // Ajustar spawn según el stickfigure real de la escena
        if (stickFigure != null)
        {
            spawnXRange  = stickFigure.scale * 0.6f;
            // Pies del stickman ≈ offset.y - scale*0.5; spawneamos 2 escalas MÁS abajo
            float feetY  = stickFigure.offset.y - stickFigure.scale * 0.5f;
            spawnStartY  = feetY - stickFigure.scale * 2f;
            despawnY     = stickFigure.offset.y + stickFigure.scale * 1.2f;
        }

        _running = true;
        StartCoroutine(GameLoop());
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyMode.Easy:
                _activeColors = 3;
                floatUpSpeed  = 0.8f;
                _wrongPenalty = 0f;
                break;
            case DifficultyMode.Medium:
                _activeColors = 4;
                floatUpSpeed  = 1.2f;
                _wrongPenalty = 5f;
                break;
            case DifficultyMode.Hard:
                _activeColors = colorNames.Length;
                floatUpSpeed  = 1.8f;
                _wrongPenalty = 10f;
                break;
        }
        PickNewTarget();
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

    IEnumerator GameLoop()
    {
        float timer = totalGameTime;
        float targetSwitchTimer = 6f;

        while (_running && timer > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
            EnsureBalloons();
            CheckHandPops();

            targetSwitchTimer -= Time.deltaTime;
            if (targetSwitchTimer <= 0f)
            {
                PickNewTarget();
                targetSwitchTimer = 6f;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        _running = false;
        if (countdownText) countdownText.text = "";
        if (_leftBalloon)  Destroy(_leftBalloon.gameObject);
        if (_rightBalloon) Destroy(_rightBalloon.gameObject);
        _leftBalloon = _rightBalloon = null;

        if (results != null)
            results.Show(_score, Mathf.RoundToInt(totalGameTime), expectedMaxScore);
        else
            ShowFeedback($"Final: {_score} pts", Color.cyan);
    }

    void EnsureBalloons()
    {
        if (_leftBalloon == null || _leftBalloon.OffScreen)
        {
            if (_leftBalloon != null) Destroy(_leftBalloon.gameObject);
            _leftBalloon = SpawnAt(-spawnXRange);
        }
        if (_rightBalloon == null || _rightBalloon.OffScreen)
        {
            if (_rightBalloon != null) Destroy(_rightBalloon.gameObject);
            _rightBalloon = SpawnAt(spawnXRange);
        }
    }

    Balloon SpawnAt(float xOffset)
    {
        if (balloonPrefab == null || spawnArea == null) return null;
        // X centrado en 0, Y fija abajo para que los globos suban desde fuera de pantalla
        Vector3 pos = new Vector3(xOffset, spawnStartY, 0f);
        GameObject go = Instantiate(balloonPrefab, pos, Quaternion.identity);
        go.transform.localScale *= balloonScale;

        // Rigidbody cinemático: hace que el broadphase de física actualice la posición
        // cada frame aunque el objeto se mueva por transform.position (no por física).
        // Sin esto, Physics.OverlapSphere usa posiciones desactualizadas del collider.
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // SphereCollider trigger para que OverlapSphere lo detecte
        if (go.GetComponent<Collider>() == null)
        {
            var col = go.AddComponent<SphereCollider>();
            col.radius    = 0.5f;
            col.isTrigger = true;
        }
        else
        {
            go.GetComponent<Collider>().isTrigger = true;
        }

        var b = go.GetComponent<Balloon>() ?? go.AddComponent<Balloon>();
        int colorIdx = Random.Range(0, _activeColors);
        b.Init(colorIdx, colorValues[colorIdx], floatUpSpeed, despawnY);
        return b;
    }

    void CheckHandPops()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;
        CheckOverlapPop(LandmarkToWorld(15));
        CheckOverlapPop(LandmarkToWorld(16));
    }

    void CheckOverlapPop(Vector3 wristPos)
    {
        // OverlapSphere detecta cualquier collider dentro del radio sin importar Z
        Collider[] hits = Physics.OverlapSphere(wristPos, popRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            Balloon b = hit.GetComponent<Balloon>();
            if (b == null) continue;
            if (b == _leftBalloon)  { PopBalloon(_leftBalloon);  _leftBalloon  = null; return; }
            if (b == _rightBalloon) { PopBalloon(_rightBalloon); _rightBalloon = null; return; }
        }
    }

    Vector3 LandmarkToWorld(int idx)
    {
        Vector3 lm  = PoseReceiverUDP.Instance.GetLandmark(idx);
        float   s   = stickFigure != null ? stickFigure.scale  : 5f;
        Vector3 off = stickFigure != null ? stickFigure.offset : new Vector3(0f, 2f, 0f);
        return new Vector3((lm.x - 0.5f) * s, (0.5f - lm.y) * s + off.y, 0f);
    }

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
            if (CelebrationBurst.Instance != null)
                CelebrationBurst.Instance.Trigger(b.transform.position);
        }
        UpdateScoreUI();
        Destroy(b.gameObject);
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {_score}";
    }

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text  = msg;
            feedbackText.color = color;
        }
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}
