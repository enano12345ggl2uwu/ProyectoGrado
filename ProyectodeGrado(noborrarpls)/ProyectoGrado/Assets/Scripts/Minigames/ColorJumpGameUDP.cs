using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Minijuego 1: Color Jump
/// El niño se mueve izquierda o derecha para pararse sobre la plataforma del color correcto.
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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private readonly string[] colorNames = { "RED", "BLUE", "GREEN", "YELLOW", "ORANGE", "PURPLE" };
    private readonly Color[] colorValues = {
        new Color(0.94f, 0.33f, 0.31f),
        new Color(0.31f, 0.76f, 0.97f),
        new Color(0.40f, 0.73f, 0.42f),
        new Color(1.00f, 0.84f, 0.31f),
        new Color(1.00f, 0.60f, 0.20f),
        new Color(0.67f, 0.28f, 0.74f)
    };

    private int score = 0;
    private int targetIndex;
    private int leftIndex, rightIndex;
    private bool roundActive = false;
    private bool targetOnLeft;
    private int activeColorCount;

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        UpdateScoreUI();
        // No arranca solo: espera StartGame()
    }

    /// <summary>Llamado por DifficultySelector. level: 0=Easy 1=Medium 2=Hard</summary>
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
                moveThreshold    = 0.10f;
                activeColorCount = 4;
                break;
            case DifficultyMode.Medium:
                roundTime        = 4f;
                moveThreshold    = 0.15f;
                activeColorCount = colorNames.Length;
                break;
            case DifficultyMode.Hard:
                roundTime        = 2.5f;
                moveThreshold    = 0.20f;
                activeColorCount = colorNames.Length;
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

            while (timer > 0f && roundActive)
            {
                if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
                CheckPlayerPosition();
                timer -= Time.deltaTime;
                yield return null;
            }

            if (roundActive)
            {
                ShowFeedback("Try again!", Color.white);
                PlayClip(wrongClip);
                roundActive = false;
            }

            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
        }
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

        if (leftPlatform) leftPlatform.material.color = colorValues[leftIndex];
        if (rightPlatform) rightPlatform.material.color = colorValues[rightIndex];

        if (colorWordText)
        {
            colorWordText.text = colorNames[targetIndex];
            colorWordText.color = colorValues[targetIndex];
        }
    }

    void CheckPlayerPosition()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        Vector3 leftHip = PoseReceiverUDP.Instance.GetLandmark(23);
        Vector3 rightHip = PoseReceiverUDP.Instance.GetLandmark(24);
        float centerX = (leftHip.x + rightHip.x) / 2f - 0.5f;

        if (centerX < -moveThreshold)
            EvaluateAnswer(true);
        else if (centerX > moveThreshold)
            EvaluateAnswer(false);
    }

    void EvaluateAnswer(bool playerWentLeft)
    {
        bool correct = (playerWentLeft && targetOnLeft) || (!playerWentLeft && !targetOnLeft);
        if (correct)
        {
            score += 10;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            UpdateScoreUI();
            ShowFeedback("Great job!", Color.green);
            PlayClip(correctClip);
        }
        else
        {
            ShowFeedback("Try again!", Color.yellow);
            PlayClip(wrongClip);
        }
        roundActive = false;
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
