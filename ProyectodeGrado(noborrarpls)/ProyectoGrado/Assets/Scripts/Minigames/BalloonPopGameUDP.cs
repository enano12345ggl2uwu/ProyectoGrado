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
    public GameObject balloonPrefab;
    public Transform  spawnArea;
    public float      spawnXRange   = 4f;
    public float      floatUpSpeed  = 2f;
    public float      spawnInterval = 1.4f;
    public float      despawnY      = 6f;
    public float      popRadius     = 0.8f;

    [Header("UI")]
    public TextMeshProUGUI targetColorText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Game")]
    public float totalGameTime = 60f;
    public DifficultyMode difficulty = DifficultyMode.Medium;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;

    private readonly string[] colorNames = { "RED", "BLUE", "GREEN", "YELLOW", "ORANGE", "PURPLE" };
    private readonly Color[]  colorValues = {
        new Color(0.94f, 0.33f, 0.31f),
        new Color(0.31f, 0.76f, 0.97f),
        new Color(0.40f, 0.73f, 0.42f),
        new Color(1.00f, 0.84f, 0.31f),
        new Color(1.00f, 0.60f, 0.20f),
        new Color(0.67f, 0.28f, 0.74f)
    };

    private readonly List<Balloon> _live = new List<Balloon>();
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
        _running = true;
        StartCoroutine(GameLoop());
        StartCoroutine(SpawnLoop());
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyMode.Easy:
                _activeColors = 3;
                floatUpSpeed  = 1.5f;
                spawnInterval = 2.0f;
                _wrongPenalty = 0f;
                break;
            case DifficultyMode.Medium:
                _activeColors = 4;
                floatUpSpeed  = 2.0f;
                spawnInterval = 1.4f;
                _wrongPenalty = 5f;
                break;
            case DifficultyMode.Hard:
                _activeColors = colorNames.Length;
                floatUpSpeed  = 2.8f;
                spawnInterval = 0.9f;
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
        ShowFeedback($"Final: {_score} pts", Color.cyan);
        foreach (var b in _live) if (b) Destroy(b.gameObject);
        _live.Clear();
    }

    IEnumerator SpawnLoop()
    {
        while (_running)
        {
            SpawnBalloon();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnBalloon()
    {
        if (balloonPrefab == null || spawnArea == null) return;
        float x = spawnArea.position.x + Random.Range(-spawnXRange, spawnXRange);
        Vector3 pos = new Vector3(x, spawnArea.position.y, spawnArea.position.z);
        GameObject go = Instantiate(balloonPrefab, pos, Quaternion.identity);
        var b = go.GetComponent<Balloon>();
        if (b == null) b = go.AddComponent<Balloon>();
        int colorIdx = Random.Range(0, _activeColors);
        b.Init(colorIdx, colorValues[colorIdx], floatUpSpeed, despawnY);
        _live.Add(b);
    }

    void CheckHandPops()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        Vector3 leftWrist  = LandmarkToWorld(15);
        Vector3 rightWrist = LandmarkToWorld(16);

        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var b = _live[i];
            if (b == null) { _live.RemoveAt(i); continue; }

            if (b.OffScreen) { Destroy(b.gameObject); _live.RemoveAt(i); continue; }

            float distL = Vector3.Distance(b.transform.position, leftWrist);
            float distR = Vector3.Distance(b.transform.position, rightWrist);
            if (Mathf.Min(distL, distR) < popRadius)
            {
                PopBalloon(b);
                _live.RemoveAt(i);
            }
        }
    }

    Vector3 LandmarkToWorld(int idx)
    {
        // convierte coords normalizadas de MediaPipe a mundo usando mismo mapeo que StickFigure
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(idx);
        const float scale = 5f;
        Vector3 offset = new Vector3(0f, 2f, 0f);
        return new Vector3((lm.x - 0.5f) * scale, (0.5f - lm.y) * scale, lm.z * scale) + offset;
    }

    void PopBalloon(Balloon b)
    {
        bool correct = b.ColorIndex == _targetColorIdx;
        if (correct)
        {
            _score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            ShowFeedback("Great!", Color.green);
            PlayClip(popClip);
            if (CelebrationBurst.Instance != null)
                CelebrationBurst.Instance.Trigger(b.transform.position);
        }
        else
        {
            _score = Mathf.Max(0, _score - (int)_wrongPenalty);
            ShowFeedback("Wrong color!", Color.yellow);
            PlayClip(wrongClip);
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
