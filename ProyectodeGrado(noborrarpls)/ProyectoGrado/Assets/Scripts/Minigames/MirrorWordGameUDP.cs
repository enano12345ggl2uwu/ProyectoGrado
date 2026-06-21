using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego: Mirror the Word
/// Muestra una palabra de pose en ingles (HANDS UP, T POSE, etc). El niño imita
/// la pose; se valida por geometria sobre landmarks MediaPipe.
/// NO se inicia solo: espera a que DifficultySelector llame StartGame(level).
/// </summary>
public class MirrorWordGameUDP : MonoBehaviour
{
    enum Pose { HandsUp, TPose, TouchFace, ArmsWide, HandsDown, Squat, OneArmUp, HandsOnHips, StrongMan }

    [Header("UI")]
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;
    public HoldFillBar holdBar;

    [Header("Referencias")]
    public StickFigureUDP  stickFigure;
    public PoseSilhouette  silhouette;

    [Header("Depth (Z)")]
    [Tooltip("Cuánto mover hacia atrás en Z al stickFigure y la silueta al arrancar.")]
    public float depthZ = 4f;
    [Tooltip("Posición X de la silueta (negativo = izquierda, positivo = derecha).")]
    public float silhouetteX = -3f;
    [Tooltip("Posición Y de la silueta (positivo = arriba).")]
    public float silhouetteY = 5f;

    [Header("Config base")]
    public float roundTime    = 8f;
    public float holdTime     = 1.5f;
    public float feedbackTime = 1.8f;

    [Header("Session")]
    [Tooltip("Cantidad de rondas antes de mostrar el panel final.")]
    public int totalRounds = 6;
    [Tooltip("Panel final. Arrastra el GameObject con ResultsScreen.")]
    public ResultsScreen results;

    [Header("Round Progress Bar")]
    [Tooltip("Anillo radial sobre el cursor que muestra el tiempo restante del round.")]
    public RoundProgressBar roundProgressBar;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    private float tolMult = 1f;

    private readonly string[] poseNames = {
        "HANDS UP", "T POSE", "TOUCH FACE", "ARMS WIDE", "HANDS DOWN",
        "SQUAT", "ONE ARM UP", "HANDS ON HIPS", "STRONG MAN"
    };

    // Estado
    private int   score       = 0;
    private Pose  currentPose;
    private int   lastPoseIdx = -1;
    private bool  roundActive = false;
    private float holdTimer   = 0f;

    private readonly HashSet<int> paintedJoints = new HashSet<int>();
    private readonly HashSet<int> paintedBones  = new HashSet<int>();
    private int _roundsPlayed = 0;

    private class PosePart
    {
        public int[]      joints;
        public int[]      bones;
        public Func<bool> validator;
    }

    void Start()
    {
        if (feedbackText) feedbackText.text = "";
        if (holdBar)  holdBar.ResetBar();
        UpdateScoreUI();

        if (stickFigure)
            stickFigure.offset = new Vector3(stickFigure.offset.x, stickFigure.offset.y + 5.0f, stickFigure.offset.z + depthZ);
        if (silhouette)
        {
            silhouette.scale  = 12f;
            silhouette.offset = new Vector3(silhouetteX, silhouette.offset.y + silhouetteY, silhouette.offset.z + depthZ);
        }

        // Si no hay DifficultySelector en la escena, arranca con Medium automaticamente
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
                holdTime  = 2.25f;
                roundTime = 10f;
                tolMult   = 1.5f;
                break;
            case 2: // Hard
                holdTime  = 1.2f;
                roundTime = 6f;
                tolMult   = 0.75f;
                break;
            default: // Medium
                holdTime  = 1.5f;
                roundTime = 8f;
                tolMult   = 1f;
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
            if (roundProgressBar) roundProgressBar.Show();

            while (timer > 0f && roundActive)
            {
                if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();
                if (roundProgressBar) roundProgressBar.SetProgress(timer / roundTime);

                bool allOk = ValidateAndPaint();

                if (allOk)
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
                    if (holdBar) holdBar.ResetBar();
                    if (feedbackText) feedbackText.text = "";
                    if (wordText) wordText.color = Color.white;
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            if (roundActive)
            {
                ShowFeedback("Try again!", Color.white);
                PlayClip(wrongClip);
                if (stickFigure) stickFigure.RegisterWrong();
                roundActive = false;
            }

            if (stickFigure) stickFigure.ResetColors();
            paintedJoints.Clear();
            paintedBones.Clear();
            if (holdBar)  holdBar.ResetBar();
            if (wordText)     wordText.color          = Color.white;
            if (roundProgressBar) roundProgressBar.Hide();

            _roundsPlayed++;
            yield return new WaitForSeconds(feedbackTime);
            if (feedbackText) feedbackText.text = "";
        }

        if (countdownText) countdownText.text = "";
        if (roundProgressBar) roundProgressBar.Hide();
        if (results != null)
            results.Show(score, _roundsPlayed, totalRounds * 10);
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
        if (silhouette)  silhouette.ShowPose(poseNames[poseIdx]);
    }

    bool ValidateAndPaint()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return false;

        PosePart[] parts = GetParts(currentPose);
        if (parts == null || parts.Length == 0) return false;

        bool allOk = true;

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
            Color c = ok ? UITheme.Success : UITheme.Failure;

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
                float tolY = sw * 0.4f * tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {11,13,15}, bones = new[] {1,2},
                        validator = () => (lm(11).y - lm(15).y) > tolY
                    },
                    new PosePart {
                        joints = new[] {12,14,16}, bones = new[] {3,4},
                        validator = () => (lm(12).y - lm(16).y) > tolY
                    }
                };
            }
            case Pose.TPose:
            {
                float tolY  = sw * 0.35f * tolMult;
                float extTh = sw * 0.8f / tolMult;
                // Abs en X para ser robusto a si la camara espeja o no: solo
                // exige que la muneca este lejos del hombro horizontalmente.
                return new[] {
                    new PosePart {
                        joints = new[] {11,13,15}, bones = new[] {1,2},
                        validator = () => Mathf.Abs(lm(15).y - lm(11).y) < tolY && Mathf.Abs(lm(11).x - lm(15).x) > extTh
                    },
                    new PosePart {
                        joints = new[] {12,14,16}, bones = new[] {3,4},
                        validator = () => Mathf.Abs(lm(16).y - lm(12).y) < tolY && Mathf.Abs(lm(16).x - lm(12).x) > extTh
                    }
                };
            }
            case Pose.TouchFace:
            {
                // Distancia 2D (sin Z, que en MediaPipe tiene escala distinta y
                // explota cuando la mano va al frente de la cara). Tolerancia mas
                // amplia: cualquier punto de la cara, no solo la punta de la nariz.
                float tolDist = sw * 0.75f * tolMult;
                Vector2 xy(int k) { var v = lm(k); return new Vector2(v.x, v.y); }
                return new[] {
                    new PosePart {
                        joints = new[] {0,15,16}, bones = new[] {2,4},
                        validator = () =>
                            Vector2.Distance(xy(15), xy(0)) < tolDist ||
                            Vector2.Distance(xy(16), xy(0)) < tolDist
                    }
                };
            }
            case Pose.ArmsWide:
            {
                float threshold = sw * 1.5f / tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {11,12,15,16}, bones = new[] {1,2,3,4},
                        validator = () => Mathf.Abs(lm(16).x - lm(15).x) > threshold
                    }
                };
            }
            case Pose.HandsDown:
            {
                // Referencia: hombro en vez de cadera. Asi solo pedimos que las
                // munecas esten "abajo del torso", no necesariamente al nivel del
                // muslo. Pose mas natural de "brazos relajados a los lados".
                float tolY = sw * 0.6f * tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {11,15}, bones = new[] {1,2},
                        validator = () => (lm(15).y - lm(11).y) > tolY
                    },
                    new PosePart {
                        joints = new[] {12,16}, bones = new[] {3,4},
                        validator = () => (lm(16).y - lm(12).y) > tolY
                    }
                };
            }
            case Pose.Squat:
            {
                float squatTh = sw * 0.5f * tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {23,24,25,26},
                        bones  = new[] {8,9,10,11},
                        validator = () =>
                            (lm(25).y - lm(23).y) < squatTh &&
                            (lm(26).y - lm(24).y) < squatTh
                    }
                };
            }
            case Pose.OneArmUp:
            {
                float tolY     = sw * 0.4f * tolMult;
                float downTol  = tolY * 0.3f;
                return new[] {
                    new PosePart {
                        joints = new[] {11,12,13,14,15,16},
                        bones  = new[] {1,2,3,4},
                        validator = () =>
                        {
                            bool leftUp    = (lm(11).y - lm(15).y) > tolY;
                            bool rightUp   = (lm(12).y - lm(16).y) > tolY;
                            bool leftDown  = (lm(11).y - lm(15).y) < downTol;
                            bool rightDown = (lm(12).y - lm(16).y) < downTol;
                            return (leftUp && rightDown) || (rightUp && leftDown);
                        }
                    }
                };
            }
            case Pose.HandsOnHips:
            {
                // Tolerancias separadas: en Y un poco mas estrechas (la mano debe
                // estar cerca de la altura de la cadera), en X mas anchas (el codo
                // queda flexionado y la muneca puede caer ligeramente afuera de la
                // cadera). Subimos tolerancia general para no exigir precision quirurgica.
                float tolY = sw * 0.55f * tolMult;
                float tolX = sw * 0.85f * tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {11,13,15,23},
                        bones  = new[] {1,2,5},
                        validator = () =>
                            Mathf.Abs(lm(15).y - lm(23).y) < tolY &&
                            Mathf.Abs(lm(15).x - lm(23).x) < tolX
                    },
                    new PosePart {
                        joints = new[] {12,14,16,24},
                        bones  = new[] {3,4,6},
                        validator = () =>
                            Mathf.Abs(lm(16).y - lm(24).y) < tolY &&
                            Mathf.Abs(lm(16).x - lm(24).x) < tolX
                    }
                };
            }
            case Pose.StrongMan:
            {
                // Bicep flex: codo a la altura del hombro, antebrazo vertical
                // (muneca arriba del codo y aprox sobre el).
                float tolY = sw * 0.35f * tolMult;
                float tolX = sw * 0.55f * tolMult;
                return new[] {
                    new PosePart {
                        joints = new[] {11,13,15}, bones = new[] {1,2},
                        validator = () =>
                            Mathf.Abs(lm(13).y - lm(11).y) < tolY &&
                            (lm(13).y - lm(15).y)         > tolY * 0.5f &&
                            Mathf.Abs(lm(15).x - lm(13).x) < tolX
                    },
                    new PosePart {
                        joints = new[] {12,14,16}, bones = new[] {3,4},
                        validator = () =>
                            Mathf.Abs(lm(14).y - lm(12).y) < tolY &&
                            (lm(14).y - lm(16).y)         > tolY * 0.5f &&
                            Mathf.Abs(lm(16).x - lm(14).x) < tolX
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
        ShowFeedback("Perfect!", UITheme.Success);
        PlayClip(correctClip);

        if (stickFigure)
        {
            stickFigure.SetAllJointsColor(UITheme.Success);
            stickFigure.SetAllBonesColor(UITheme.Success);
            stickFigure.RegisterCorrect();
        }

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

    public void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}