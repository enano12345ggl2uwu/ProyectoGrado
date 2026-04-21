using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego: Mirror the Word
/// Muestra una palabra de pose en inglés. El niño imita la pose.
/// Las partes implicadas se pintan VERDE (ok) o ROJO (mal) en vivo.
/// Mantener 1.5 s → acierto (+10 pts).
/// </summary>
public class MirrorWordGameUDP : MonoBehaviour
{
    enum Pose { HandsUp, TPose, TouchHead, ArmsWide, HandsDown }

    [Header("UI")]
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;
    /// <summary>Image tipo Filled. Se llena durante el hold.</summary>
    public Image holdFillBar;

    [Header("Referencias")]
    public StickFigureUDP stickFigure;

    [Header("Config")]
    public float roundTime    = 8f;
    public float holdTime     = 1.5f;
    public float feedbackTime = 1.8f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    [Header("Colores feedback")]
    public Color colorOk  = new Color(0.2f, 1f, 0.3f, 1f);   // verde neon
    public Color colorBad = new Color(1f, 0.2f, 0.25f, 1f);  // rojo neon

    // Nombres legibles de las poses
    private readonly string[] poseNames = {
        "HANDS UP", "T POSE", "TOUCH HEAD", "ARMS WIDE", "HANDS DOWN"
    };

    // Estado
    private int   score       = 0;
    private Pose  currentPose;
    private int   lastPoseIdx = -1;
    private bool  roundActive = false;
    private float holdTimer   = 0f;

    // Partes pintadas este frame (para restaurar el siguiente)
    private readonly HashSet<int> paintedJoints = new HashSet<int>();
    private readonly HashSet<int> paintedBones  = new HashSet<int>();

    private class PosePart
    {
        public int[]      joints;
        public int[]      bones;
        public Func<bool> validator;
    }

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        if (holdFillBar)  holdFillBar.fillAmount = 0f;
        UpdateScoreUI();
        StartCoroutine(GameLoop());
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

                bool allOk = ValidateAndPaint();

                if (allOk)
                {
                    holdTimer += Time.deltaTime;
                    if (holdFillBar) holdFillBar.fillAmount = holdTimer / holdTime;

                    // Mostrar "HOLD IT!" mientras mantiene la pose
                    if (feedbackText)
                    {
                        feedbackText.text  = "HOLD IT!";
                        feedbackText.color = colorOk;
                    }

                    // Word text se pone verde gradualmente
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
                    if (holdFillBar) holdFillBar.fillAmount = 0f;
                    if (feedbackText) feedbackText.text = "";
                    if (wordText) wordText.color = Color.white;
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            // Tiempo agotado sin completar
            if (roundActive)
            {
                ShowFeedback("Try again!", Color.white);
                PlayClip(wrongClip);
                roundActive = false;
            }

            // Reset visual entre rondas
            if (stickFigure) stickFigure.ResetColors();
            paintedJoints.Clear();
            paintedBones.Clear();
            if (holdFillBar)  holdFillBar.fillAmount  = 0f;
            if (wordText)     wordText.color           = Color.white;

            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
        }
    }

    void SetupRound()
    {
        int poseIdx;
        do { poseIdx = UnityEngine.Random.Range(0, poseNames.Length); } while (poseIdx == lastPoseIdx);
        lastPoseIdx = poseIdx;
        currentPose = (Pose)poseIdx;

        if (wordText) { wordText.text = poseNames[poseIdx]; wordText.color = Color.white; }
        if (feedbackText) feedbackText.text = "";
        holdTimer = 0f;
        if (stickFigure) stickFigure.ResetColors();
    }

    bool ValidateAndPaint()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return false;

        PosePart[] parts = GetParts(currentPose);
        if (parts == null || parts.Length == 0) return false;

        bool allOk = true;

        // Restaurar color base de las partes pintadas el frame anterior
        if (stickFigure)
        {
            foreach (int j in paintedJoints) stickFigure.SetJointColor(j, stickFigure.jointColor);
            foreach (int b in paintedBones)  stickFigure.SetBoneColor(b,  stickFigure.boneColor);
        }
        paintedJoints.Clear();
        paintedBones.Clear();

        foreach (var p in parts)
        {
            bool ok = p.validator();
            if (!ok) allOk = false;
            Color c = ok ? colorOk : colorBad;

            if (stickFigure)
            {
                if (p.joints != null)
                    foreach (int j in p.joints) { stickFigure.SetJointColor(j, c); paintedJoints.Add(j); }
                if (p.bones != null)
                    foreach (int b in p.bones)  { stickFigure.SetBoneColor(b,  c); paintedBones.Add(b); }
            }
        }

        return allOk;
    }

    PosePart[] GetParts(Pose pose)
    {
        float sw = ShoulderDist();
        if (sw < 0.05f) return null;

        var I = PoseReceiverUDP.Instance;
        Vector3 lm(int k) => I.GetLandmark(k);

        switch (pose)
        {
            case Pose.HandsUp:
            {
                float tol = sw * 0.25f; // más permisivo
                return new[]
                {
                    new PosePart {
                        joints = new[] {11, 13, 15}, bones = new[] {1, 2},
                        validator = () => (lm(11).y - lm(15).y) > tol
                    },
                    new PosePart {
                        joints = new[] {12, 14, 16}, bones = new[] {3, 4},
                        validator = () => (lm(12).y - lm(16).y) > tol
                    }
                };
            }

            case Pose.TPose:
            {
                float tolY  = sw * 0.25f; // más permisivo
                float extTh = sw * 0.6f;  // más fácil de alcanzar
                return new[]
                {
                    new PosePart {
                        joints = new[] {11, 13, 15}, bones = new[] {1, 2},
                        validator = () =>
                            Mathf.Abs(lm(15).y - lm(11).y) < tolY &&
                            (lm(11).x - lm(15).x) > extTh
                    },
                    new PosePart {
                        joints = new[] {12, 14, 16}, bones = new[] {3, 4},
                        validator = () =>
                            Mathf.Abs(lm(16).y - lm(12).y) < tolY &&
                            (lm(16).x - lm(12).x) > extTh
                    }
                };
            }

            case Pose.TouchHead:
            {
                float tol = sw * 0.55f; // zona de toque más grande
                return new[]
                {
                    new PosePart {
                        joints = new[] {0, 15, 16}, bones = new[] {2, 4},
                        validator = () =>
                            Vector3.Distance(lm(15), lm(0)) < tol ||
                            Vector3.Distance(lm(16), lm(0)) < tol
                    }
                };
            }

            case Pose.ArmsWide:
            {
                float thr = sw * 1.1f; // más fácil
                return new[]
                {
                    new PosePart {
                        joints = new[] {11, 12, 15, 16}, bones = new[] {1, 2, 3, 4},
                        validator = () => Mathf.Abs(lm(16).x - lm(15).x) > thr
                    }
                };
            }

            case Pose.HandsDown:
            {
                float tol  = sw * 0.15f; // más permisivo
                float hipY = (lm(23).y + lm(24).y) * 0.5f;
                return new[]
                {
                    new PosePart {
                        joints = new[] {15, 23}, bones = new[] {1, 2},
                        validator = () => (lm(15).y - hipY) > tol
                    },
                    new PosePart {
                        joints = new[] {16, 24}, bones = new[] {3, 4},
                        validator = () => (lm(16).y - hipY) > tol
                    }
                };
            }
        }
        return null;
    }

    float ShoulderDist()
    {
        if (PoseReceiverUDP.Instance == null) return 0f;
        return Vector3.Distance(
            PoseReceiverUDP.Instance.GetLandmark(11),
            PoseReceiverUDP.Instance.GetLandmark(12)
        );
    }

    void EvaluateCorrect()
    {
        score += 10;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
        UpdateScoreUI();
        ShowFeedback("Perfect!", colorOk);
        PlayClip(correctClip);
        if (stickFigure)
        {
            stickFigure.SetAllJointsColor(colorOk);
            stickFigure.SetAllBonesColor(colorOk);
        }
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

    public void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}
