using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Minijuego 1: Color Jump
/// El niño se mueve izquierda/derecha con el cuerpo para pararse sobre el color correcto.
/// Detección basada en caderas (landmarks 23 y 24).
/// </summary>
public class ColorJumpGameUDP : MonoBehaviour
{
    public enum DifficultyMode { Easy, Medium, Hard }

    [Header("Referencias")]
    public Renderer leftPlatform;
    public Renderer rightPlatform;

    [Header("UI")]
    public TextMeshProUGUI colorWordText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;

    [Header("Config")]
    public float roundTime = 4f;
    public float feedbackTime = 1.5f;
    public float moveThreshold = 0.15f;

    [Header("Difficulty")]
    public DifficultyMode difficulty = DifficultyMode.Medium;

    [Header("Session")]
    [Tooltip("Cantidad de rondas antes de mostrar el panel final.")]
    public int totalRounds = 8;
    [Tooltip("Panel final. Arrastra el GameObject con ResultsScreen.")]
    public ResultsScreen results;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private readonly string[] colorNames = { "RED", "BLUE", "GREEN", "YELLOW", "ORANGE", "PURPLE" };
    private readonly Color[] colorValues = UITheme.GameColors;

    private int score = 0;
    private int targetIndex;
    private int leftIndex, rightIndex;
    private bool roundActive = false;
    private bool targetOnLeft;
    private int activeColorCount;
    private int _roundsPlayed = 0;

    private readonly ActionDebouncer _answerDebouncer = new ActionDebouncer();
    private bool _lastAnswerCorrect = false;

    // Colores de highlight para la plataforma hovereada
    private Color _leftBaseColor;
    private Color _rightBaseColor;
    private bool _highlightLeft = false;
    private bool _highlightRight = false;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        UpdateScoreUI();
    }

    public void StartGame(int level)
    {
        difficulty = (DifficultyMode)level;
        ApplyDifficulty();
        StartCoroutine(GameLoop());
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyMode.Easy:
                roundTime        = 6f;
                feedbackTime     = 1.8f;
                moveThreshold    = 0.10f;
                activeColorCount = 4;
                break;
            case DifficultyMode.Medium:
                roundTime        = 4f;
                feedbackTime     = 1.3f;
                moveThreshold    = 0.15f;
                activeColorCount = colorNames.Length;
                break;
            case DifficultyMode.Hard:
                roundTime        = 2.5f;
                feedbackTime     = 1.0f;
                moveThreshold    = 0.20f;
                activeColorCount = colorNames.Length;
                break;
        }
    }

    IEnumerator GameLoop()
    {
        _roundsPlayed = 0;
        while (_roundsPlayed < totalRounds)
        {
            SetupRound();
            _answerDebouncer.Reset();

            float timer = roundTime;
            roundActive = true;

            while (timer > 0f && roundActive)
            {
                if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
                CheckPlayerPosition();
                timer -= Time.deltaTime;
                yield return null;
            }

            if (roundActive)
            {
                ClearHighlight();
                ShowFeedback("Try again!", Color.white);
                PlayClip(wrongClip);
                roundActive = false;
            }

            _roundsPlayed++;
            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
            if (_lastAnswerCorrect)
                yield return WaitForCenter();
        }

        if (countdownText) countdownText.text = "";
        ClearHighlight();
        if (results != null)
            results.Show(score, _roundsPlayed, totalRounds * 10);
        else
            Debug.LogError("[ColorJump] results NO esta asignado en el Inspector.");
    }

    void SetupRound()
    {
        int pool = activeColorCount > 0 ? activeColorCount : colorNames.Length;
        targetIndex = Random.Range(0, pool);
        int other;
        do { other = Random.Range(0, pool); } while (other == targetIndex);

        targetOnLeft = Random.value < 0.5f;
        leftIndex = targetOnLeft ? targetIndex : other;
        rightIndex = targetOnLeft ? other : targetIndex;

        if (leftPlatform)
        {
            leftPlatform.material.color = colorValues[leftIndex];
            _leftBaseColor = colorValues[leftIndex];
        }
        if (rightPlatform)
        {
            rightPlatform.material.color = colorValues[rightIndex];
            _rightBaseColor = colorValues[rightIndex];
        }

        if (colorWordText)
        {
            colorWordText.text = colorNames[targetIndex];
            colorWordText.color = colorValues[targetIndex];
        }

        ClearHighlight();
    }

    void CheckPlayerPosition()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        Vector3 leftHip  = PoseReceiverUDP.Instance.GetLandmark(23);
        Vector3 rightHip = PoseReceiverUDP.Instance.GetLandmark(24);
        float centerX = (leftHip.x + rightHip.x) / 2f - 0.5f;

        if (centerX < -moveThreshold)
        {
            EvaluateAnswer(true);
        }
        else if (centerX > moveThreshold)
        {
            EvaluateAnswer(false);
        }
        else if (centerX < 0f)
        {
            SetHighlight(true);
        }
        else if (centerX > 0f)
        {
            SetHighlight(false);
        }
        else
        {
            ClearHighlight();
        }
    }

    void EvaluateAnswer(bool playerWentLeft)
    {
        if (!_answerDebouncer.TryFire(feedbackTime + 0.3f)) return;

        bool correct = (playerWentLeft && targetOnLeft) || (!playerWentLeft && !targetOnLeft);
        _lastAnswerCorrect = correct;
        if (correct)
        {
            score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            UpdateScoreUI();
            ShowFeedback("Great job!", UITheme.Success);
            PlayClip(correctClip);
            if (CelebrationBurst.Instance != null)
                CelebrationBurst.Instance.Trigger(transform.position);
        }
        else
        {
            ShowFeedback("Try again!", UITheme.Warning);
            PlayClip(wrongClip);
        }
        ClearHighlight();
        roundActive = false;
    }

    IEnumerator WaitForCenter()
    {
        ShowFeedback("Come back!", Color.white);
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            if (PoseReceiverUDP.Instance != null && PoseReceiverUDP.Instance.poseDetected)
            {
                Vector3 lh = PoseReceiverUDP.Instance.GetLandmark(23);
                Vector3 rh = PoseReceiverUDP.Instance.GetLandmark(24);
                if (Mathf.Abs((lh.x + rh.x) / 2f - 0.5f) < moveThreshold)
                    break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (feedbackText) feedbackText.text = "";
    }

    void SetHighlight(bool highlightLeft)
    {
        if (_highlightLeft == highlightLeft && _highlightRight == !highlightLeft) return;

        _highlightLeft = highlightLeft;
        _highlightRight = !highlightLeft;

        if (leftPlatform)
            leftPlatform.material.color = highlightLeft
                ? Color.Lerp(_leftBaseColor, Color.white, 0.4f)
                : _leftBaseColor;

        if (rightPlatform)
            rightPlatform.material.color = !highlightLeft
                ? Color.Lerp(_rightBaseColor, Color.white, 0.4f)
                : _rightBaseColor;
    }

    void ClearHighlight()
    {
        _highlightLeft = false;
        _highlightRight = false;
        if (leftPlatform && _leftBaseColor != default) leftPlatform.material.color = _leftBaseColor;
        if (rightPlatform && _rightBaseColor != default) rightPlatform.material.color = _rightBaseColor;
    }

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text = msg;
            feedbackText.color = color;
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }
}
