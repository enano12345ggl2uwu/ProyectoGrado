using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Minijuego: Number Balloon Pop.
/// Globos con un numero (1-10) suben desde abajo. La UI muestra una palabra como "POP THREE!"
/// y el niño debe pinchar el globo con el numero correspondiente usando sus muñecas
/// (landmarks 15 y 16). Refuerza vocabulario de numeros en ingles + asociacion palabra/cantidad.
///
/// SETUP en Unity:
///  1. Crea un prefab de globo con:
///       - Sphere 3D + SphereCollider radio 0.5
///       - Hijo TextMeshPro (3D, NO UGUI) centrado, font size grande, color blanco
///       - Script Balloon
///  2. Crea GameObject "NumberBalloonManager" con este script.
///  3. Asigna en Inspector:
///       balloonPrefab, spawnArea (Transform en la parte baja de la escena),
///       targetText (TMP grande arriba), scoreText, feedbackText, countdownText,
///       audioSource, popClip, wrongClip.
///  4. En DifficultySelector arrastra este GameObject al campo numberBalloonGame.
///
/// Dificultad:
///   Easy   - numeros 1-5,  spawn lento, sin penalidad.
///   Medium - numeros 1-7,  spawn medio, -5 al fallar.
///   Hard   - numeros 1-10, spawn rapido, -10 al fallar.
/// </summary>
public class NumberBalloonGameUDP : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    public GameObject balloonPrefab;
    public Transform  spawnArea;
    public float      spawnXRange   = 4f;
    public float      floatUpSpeed  = 2f;
    public float      spawnInterval = 1.4f;
    public float      despawnY      = 6f;
    public float      popRadius     = 0.8f;

    [Header("UI")]
    public TextMeshProUGUI targetText;       // ej: "POP THREE!"
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Game")]
    public float totalGameTime     = 60f;
    public float targetSwitchEvery = 8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;

    // Colores asignados por indice de numero (1=rojo, 2=azul, ...). Solo para que cada numero
    // tenga un globo de color distinto y sea visualmente distinguible. La regla del juego
    // es por numero, no por color.
    private readonly Color[] _palette = {
        new Color(0.94f, 0.33f, 0.31f), // 1
        new Color(0.31f, 0.76f, 0.97f), // 2
        new Color(0.40f, 0.73f, 0.42f), // 3
        new Color(1.00f, 0.84f, 0.31f), // 4
        new Color(1.00f, 0.60f, 0.20f), // 5
        new Color(0.67f, 0.28f, 0.74f), // 6
        new Color(0.96f, 0.45f, 0.69f), // 7
        new Color(0.40f, 0.80f, 0.80f), // 8
        new Color(0.55f, 0.35f, 0.20f), // 9
        new Color(0.55f, 0.55f, 0.55f), // 10
    };

    private static readonly string[] NumberWords = {
        "ONE", "TWO", "THREE", "FOUR", "FIVE",
        "SIX", "SEVEN", "EIGHT", "NINE", "TEN"
    };

    private int  _activeRange  = 5;   // cuantos numeros distintos circulan (1.._activeRange)
    private int  _targetIdx    = 0;   // 0-based: 0 = "ONE", 9 = "TEN"
    private int  _score        = 0;
    private int  _wrongPenalty = 5;
    private bool _running      = false;

    private readonly List<Balloon> _live = new List<Balloon>();

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        UpdateScoreUI();

        if (FindObjectOfType<DifficultySelector>() == null)
            StartGame(1);
    }

    /// <summary>Llamado por DifficultySelector. level: 0=Easy 1=Medium 2=Hard</summary>
    public void StartGame(int level)
    {
        ApplyDifficulty(level);
        _running = true;
        PickNewTarget();
        StartCoroutine(GameLoop());
        StartCoroutine(SpawnLoop());
    }

    void ApplyDifficulty(int level)
    {
        switch (level)
        {
            case 0: // Easy
                _activeRange  = 5;
                floatUpSpeed  = 1.5f;
                spawnInterval = 2.0f;
                _wrongPenalty = 0;
                break;
            case 2: // Hard
                _activeRange  = 10;
                floatUpSpeed  = 2.8f;
                spawnInterval = 0.9f;
                _wrongPenalty = 10;
                break;
            default: // Medium
                _activeRange  = 7;
                floatUpSpeed  = 2.0f;
                spawnInterval = 1.4f;
                _wrongPenalty = 5;
                break;
        }
    }

    void PickNewTarget()
    {
        int prev = _targetIdx;
        do { _targetIdx = Random.Range(0, _activeRange); }
        while (_activeRange > 1 && _targetIdx == prev);

        if (targetText)
        {
            targetText.text  = $"POP {NumberWords[_targetIdx]}!";
            targetText.color = _palette[_targetIdx];
        }
    }

    IEnumerator GameLoop()
    {
        float timer       = totalGameTime;
        float switchTimer = targetSwitchEvery;

        while (_running && timer > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
            CheckHandPops();

            switchTimer -= Time.deltaTime;
            if (switchTimer <= 0f) { PickNewTarget(); switchTimer = targetSwitchEvery; }

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
        var pos = new Vector3(x, spawnArea.position.y, spawnArea.position.z);
        var go  = Instantiate(balloonPrefab, pos, Quaternion.identity);
        var b   = go.GetComponent<Balloon>() ?? go.AddComponent<Balloon>();

        // Sesgo: con probabilidad 0.5 spawneamos el numero objetivo, asi siempre hay opciones validas.
        int idx = Random.value < 0.5f ? _targetIdx : Random.Range(0, _activeRange);
        b.InitWithNumber(idx, _palette[idx], floatUpSpeed, despawnY);
        _live.Add(b);
    }

    void CheckHandPops()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;
        Vector3 lw = LandmarkToWorld(15);
        Vector3 rw = LandmarkToWorld(16);

        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var b = _live[i];
            if (b == null)    { _live.RemoveAt(i); continue; }
            if (b.OffScreen)  { Destroy(b.gameObject); _live.RemoveAt(i); continue; }

            float dist = Mathf.Min(Vector3.Distance(b.transform.position, lw),
                                   Vector3.Distance(b.transform.position, rw));
            if (dist < popRadius) { PopBalloon(b); _live.RemoveAt(i); }
        }
    }

    Vector3 LandmarkToWorld(int idx)
    {
        // Mismo mapeo que StickFigureUDP / BalloonPopGameUDP para que las muñecas
        // visualmente correspondan al espacio de los globos.
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(idx);
        const float scale = 5f;
        Vector3 offset = new Vector3(0f, 2f, 0f);
        return new Vector3((lm.x - 0.5f) * scale, (0.5f - lm.y) * scale, lm.z * scale) + offset;
    }

    void PopBalloon(Balloon b)
    {
        bool correct = b.NumberIndex == _targetIdx;
        if (correct)
        {
            _score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            ShowFeedback($"{NumberWords[_targetIdx]}! +10", Color.green);
            PlayClip(popClip);
            if (CelebrationBurst.Instance != null)
                CelebrationBurst.Instance.Trigger(b.transform.position);
        }
        else
        {
            _score = Mathf.Max(0, _score - _wrongPenalty);
            ShowFeedback("Wrong number!", Color.red);
            PlayClip(wrongClip);
        }
        UpdateScoreUI();
        Destroy(b.gameObject);
    }

    void UpdateScoreUI() { if (scoreText) scoreText.text = $"Score: {_score}"; }
    void ShowFeedback(string msg, Color c) { if (feedbackText) { feedbackText.text = msg; feedbackText.color = c; } }
    void PlayClip(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip); }

    public int  Score => _score;
    public void BackToMenu() { if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu(); }
}
