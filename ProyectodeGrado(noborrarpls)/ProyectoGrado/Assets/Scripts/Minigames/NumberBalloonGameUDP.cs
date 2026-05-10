using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Balloon Pop simplificado: 4 globos fijos que flotan suavemente.
/// El jugador revienta el que coincide con la palabra mostrada.
/// Al reventar un globo, reaparece uno nuevo en su lugar.
/// </summary>
public class NumberBalloonGameUDP : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject balloonPrefab;

    [Header("Layout")]
    [Tooltip("Cuantos globos hay en pantalla a la vez.")]
    public int   balloonCount  = 4;
    [Tooltip("Separacion horizontal entre globos.")]
    public float slotSpacingX  = 3f;
    [Tooltip("Altura Y de los globos.")]
    public float anchorY       = 1f;
    [Tooltip("Profundidad Z de los globos.")]
    public float anchorZ       = 0f;
    [Tooltip("Amplitud del balanceo vertical.")]
    public float bobAmplitude  = 0.18f;
    [Tooltip("Velocidad del balanceo.")]
    public float bobFrequency  = 1.5f;
    [Tooltip("Radio para reventar con la muñeca.")]
    public float popRadius     = 0.8f;
    [Tooltip("Segundos antes de que reaparezca el globo reventado.")]
    public float respawnDelay  = 0.5f;

    [Header("UI")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Game")]
    public float totalGameTime     = 60f;
    public float targetSwitchEvery = 8f;

    [Header("Session")]
    public ResultsScreen results;
    public int           expectedMaxScore = 150;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;

    private readonly Color[] _palette = {
        new Color(0.94f, 0.33f, 0.31f),
        new Color(0.31f, 0.76f, 0.97f),
        new Color(0.40f, 0.73f, 0.42f),
        new Color(1.00f, 0.84f, 0.31f),
        new Color(1.00f, 0.60f, 0.20f),
        new Color(0.67f, 0.28f, 0.74f),
        new Color(0.96f, 0.45f, 0.69f),
        new Color(0.40f, 0.80f, 0.80f),
        new Color(0.55f, 0.35f, 0.20f),
        new Color(0.55f, 0.55f, 0.55f),
    };

    private static readonly string[] NumberWords = {
        "ONE", "TWO", "THREE", "FOUR", "FIVE",
        "SIX", "SEVEN", "EIGHT", "NINE", "TEN"
    };

    private int     _activeRange  = 5;
    private int     _targetIdx    = 0;
    private int     _score        = 0;
    private int     _wrongPenalty = 5;
    private bool    _running      = false;

    private Balloon[] _slots;
    private Vector3[] _anchors;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        UpdateScoreUI();
        if (FindObjectOfType<DifficultySelector>() == null)
            StartGame(1);
    }

    public void StartGame(int level)
    {
        ApplyDifficulty(level);
        _running = true;
        _slots   = new Balloon[balloonCount];
        _anchors = new Vector3[balloonCount];

        float totalWidth = (balloonCount - 1) * slotSpacingX;
        float startX     = -totalWidth / 2f;
        for (int i = 0; i < balloonCount; i++)
            _anchors[i] = new Vector3(startX + i * slotSpacingX, anchorY, anchorZ);

        PickNewTarget();
        for (int i = 0; i < balloonCount; i++)
            SpawnSlot(i);

        StartCoroutine(GameLoop());
    }

    void ApplyDifficulty(int level)
    {
        switch (level)
        {
            case 0: _activeRange = 5;  _wrongPenalty = 0;  break;
            case 2: _activeRange = 10; _wrongPenalty = 10; break;
            default: _activeRange = 7; _wrongPenalty = 5;  break;
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

    void SpawnSlot(int slot)
    {
        if (balloonPrefab == null) return;
        int idx = Random.value < 0.5f ? _targetIdx : Random.Range(0, _activeRange);
        var go  = Instantiate(balloonPrefab, _anchors[slot], Quaternion.identity);
        var b   = go.GetComponent<Balloon>() ?? go.AddComponent<Balloon>();
        b.InitStatic(idx, _palette[idx], bobFrequency, bobAmplitude);
        _slots[slot] = b;
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
        for (int i = 0; i < _slots.Length; i++)
            if (_slots[i]) Destroy(_slots[i].gameObject);

        if (results != null)
            results.Show(_score, Mathf.RoundToInt(totalGameTime), expectedMaxScore);
        else
            ShowFeedback($"Final: {_score} pts", Color.cyan);
    }

    void CheckHandPops()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;
        Vector3 lw = LandmarkToWorld(15);
        Vector3 rw = LandmarkToWorld(16);

        for (int i = 0; i < _slots.Length; i++)
        {
            var b = _slots[i];
            if (b == null) continue;
            float dist = Mathf.Min(Vector3.Distance(b.transform.position, lw),
                                   Vector3.Distance(b.transform.position, rw));
            if (dist < popRadius) { PopSlot(i); break; }
        }
    }

    void PopSlot(int slot)
    {
        var b = _slots[slot];
        if (b == null) return;

        bool correct = b.NumberIndex == _targetIdx;
        Vector3 pos  = b.transform.position;
        Destroy(b.gameObject);
        _slots[slot] = null;

        if (correct)
        {
            _score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            ShowFeedback($"{NumberWords[_targetIdx]}! +10", Color.green);
            PlayClip(popClip);
            if (CelebrationBurst.Instance != null) CelebrationBurst.Instance.Trigger(pos);
        }
        else
        {
            _score = Mathf.Max(0, _score - _wrongPenalty);
            ShowFeedback("Wrong!", Color.red);
            PlayClip(wrongClip);
        }
        UpdateScoreUI();
        StartCoroutine(RespawnAfterDelay(slot));
    }

    IEnumerator RespawnAfterDelay(int slot)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (_running) SpawnSlot(slot);
    }

    Vector3 LandmarkToWorld(int idx)
    {
        Vector3 lm     = PoseReceiverUDP.Instance.GetLandmark(idx);
        const float sc = 5f;
        return new Vector3((lm.x - 0.5f) * sc, (0.5f - lm.y) * sc, lm.z * sc) + new Vector3(0f, 2f, 0f);
    }

    void UpdateScoreUI() { if (scoreText) scoreText.text = $"Score: {_score}"; }
    void ShowFeedback(string msg, Color c) { if (feedbackText) { feedbackText.text = msg; feedbackText.color = c; } }
    void PlayClip(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip); }

    public int  Score => _score;
    public void BackToMenu() { if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu(); }
}
