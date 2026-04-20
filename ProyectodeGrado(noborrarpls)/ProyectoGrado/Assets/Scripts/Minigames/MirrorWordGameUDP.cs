using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minijuego 3: Mirror the Word
/// Muestra una palabra de pose en ingles (HANDS UP, T POSE, etc). El niño imita
/// la pose; se valida por geometria sobre landmarks MediaPipe. Mantener la
/// pose 1.5 s → acierto (+10 pts).
///
/// Feedback visual en vivo:
///  - Partes del stickman implicadas en la pose actual se pintan ROJO si
///    no cumplen, VERDE si cumplen.
///  - Partes no implicadas quedan del color neon base.
///  - Barra de progreso del hold (fillBar) se llena mientras la pose es valida.
///  - El wordText pulsa verde durante el hold.
/// </summary>
public class MirrorWordGameUDP : MonoBehaviour
{
    enum Pose { HandsUp, TPose, TouchHead, ArmsWide, HandsDown }

    [Header("UI")]
    public TextMeshProUGUI wordText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI countdownText;
    /// <summary>Imagen con Image.fillAmount (tipo Filled). Se llena durante el hold.</summary>
    public Image holdFillBar;

    [Header("Referencias")]
    /// <summary>Referencia al stickman para pintar partes en vivo.</summary>
    public StickFigureUDP stickFigure;

    [Header("Config")]
    public float roundTime    = 6f;
    public float holdTime     = 1.5f;
    public float feedbackTime = 1.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;

    [Header("Colores feedback")]
    public Color colorOk   = new Color(0.2f, 1f, 0.3f, 1f);   // verde neon
    public Color colorBad  = new Color(1f, 0.2f, 0.25f, 1f);  // rojo neon

    // --- Nombres legibles
    private readonly string[] poseNames = {
        "HANDS UP", "T POSE", "TOUCH HEAD", "ARMS WIDE", "HANDS DOWN"
    };

    // --- Estado
    private int   score       = 0;
    private Pose  currentPose;
    private int   lastPoseIdx = -1;
    private bool  roundActive = false;
    private float holdTimer   = 0f;

    // Partes que se estan pintando esta ronda (para restaurar al terminar)
    private readonly HashSet<int> paintedJoints = new HashSet<int>();
    private readonly HashSet<int> paintedBones  = new HashSet<int>();

    /// <summary>Una subcondicion de una pose: joints y bones que cambian color segun pase o no.</summary>
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
            holdTimer = 0f;

            while (timer > 0f && roundActive)
            {
                if (countdownText) countdownText.text = Mathf.CeilToInt(timer).ToString();

                // Validar la pose (pinta joints/bones segun cada parte)
                bool allOk = ValidateAndPaint();

                if (allOk)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdTime)
                    {
                        EvaluateCorrect();
                        roundActive = false;
                    }
                }
                else
                {
                    holdTimer = 0f;
                }

                // UI del hold
                if (holdFillBar) holdFillBar.fillAmount = holdTimer / holdTime;
                if (wordText)
                {
                    float t = holdTimer / holdTime;
                    wordText.color = Color.Lerp(Color.white, colorOk, t);
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            if (roundActive)
            {
                ShowFeedback("Try again!", Color.white);
                roundActive = false;
            }

            // Reset visual entre rondas
            if (stickFigure) stickFigure.ResetColors();
            paintedJoints.Clear();
            paintedBones.Clear();
            if (holdFillBar) holdFillBar.fillAmount = 0f;

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

        if (wordText)
        {
            wordText.text  = poseNames[poseIdx];
            wordText.color = Color.white;
        }

        holdTimer = 0f;
        if (stickFigure) stickFigure.ResetColors();
    }

    /// <summary>
    /// Valida la pose actual parte por parte. Pinta los joints/bones de cada parte en
    /// verde (ok) o rojo (mal). Devuelve true si todas las partes pasan.
    /// </summary>
    bool ValidateAndPaint()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return false;

        PosePart[] parts = GetParts(currentPose);
        if (parts == null || parts.Length == 0) return false;

        bool allOk = true;

        // Primero restauro a color base las partes que pintamos el frame anterior
        // pero que ya no son relevantes (por si cambia la pose).
        if (stickFigure)
        {
            foreach (int j in paintedJoints) stickFigure.SetJointColor(j, stickFigure.jointColor);
            foreach (int b in paintedBones)  stickFigure.SetBoneColor(b,  stickFigure.boneColor);
        }
        paintedJoints.Clear();
        paintedBones.Clear();

        // Validar cada parte y pintar
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

    /// <summary>Devuelve la lista de subcondiciones (partes) de la pose indicada.</summary>
    PosePart[] GetParts(Pose pose)
    {
        float sw = ShoulderDist();
        if (sw < 0.05f) return null;

        // Aliases cortos para legibilidad
        var I = PoseReceiverUDP.Instance;
        Vector3 lm(int k) => I.GetLandmark(k);

        switch (pose)
        {
            case Pose.HandsUp:
            {
                float tolY = sw * 0.4f;
                return new []
                {
                    new PosePart {
                        joints = new[] {11, 13, 15},
                        bones  = new[] {1, 2},
                        validator = () => (lm(11).y - lm(15).y) > tolY
                    },
                    new PosePart {
                        joints = new[] {12, 14, 16},
                        bones  = new[] {3, 4},
                        validator = () => (lm(12).y - lm(16).y) > tolY
                    }
                };
            }

            case Pose.TPose:
            {
                float tolY  = sw * 0.35f;
                float extTh = sw * 0.8f;
                return new []
                {
                    // brazo izq: muñeca a altura de hombro + extendida
                    new PosePart {
                        joints = new[] {11, 13, 15},
                        bones  = new[] {1, 2},
                        validator = () =>
                            Mathf.Abs(lm(15).y - lm(11).y) < tolY &&
                            (lm(11).x - lm(15).x) > extTh
                    },
                    // brazo der: muñeca a altura de hombro + extendida
                    new PosePart {
                        joints = new[] {12, 14, 16},
                        bones  = new[] {3, 4},
                        validator = () =>
                            Mathf.Abs(lm(16).y - lm(12).y) < tolY &&
                            (lm(16).x - lm(12).x) > extTh
                    }
                };
            }

            case Pose.TouchHead:
            {
                float tolDist = sw * 0.35f;
                return new []
                {
                    new PosePart {
                        joints = new[] {0, 15, 16},
                        bones  = new[] {2, 4},
                        validator = () =>
                            Vector3.Distance(lm(15), lm(0)) < tolDist ||
                            Vector3.Distance(lm(16), lm(0)) < tolDist
                    }
                };
            }

            case Pose.ArmsWide:
            {
                float threshold = sw * 1.5f;
                return new []
                {
                    new PosePart {
                        joints = new[] {11, 12, 15, 16},
                        bones  = new[] {1, 2, 3, 4},
                        validator = () => Mathf.Abs(lm(16).x - lm(15).x) > threshold
                    }
                };
            }

            case Pose.HandsDown:
            {
                float tolY = sw * 0.3f;
                float hipY = (lm(23).y + lm(24).y) * 0.5f;
                return new []
                {
                    new PosePart {
                        joints = new[] {15, 23},
                        bones  = new[] {1, 2},
                        validator = () => (lm(15).y - hipY) > tolY
                    },
                    new PosePart {
                        joints = new[] {16, 24},
                        bones  = new[] {3, 4},
                        validator = () => (lm(16).y - hipY) > tolY
                    }
                };
            }
        }
        return null;
    }

    /// <summary>Distancia entre hombros como unidad de normalizacion.</summary>
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
        ShowFeedback("Great job!", colorOk);
        PlayClip(correctClip);

        // Flash de todo el stickman en verde
        if (stickFigure)
        {
            stickFigure.SetAllJointsColor(colorOk);
            stickFigure.SetAllBonesColor(colorOk);
        }
    }

    void ShowFeedback(string msg, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text  = msg;
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

    /// <summary>Vuelve al menu principal. Enlazar desde Button OnClick en el Inspector.</summary>
    public void BackToMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }
}

/*
═══════════════════════════════════════════════════════════════════════════════
INSTRUCCIONES: Armar la escena "Island3" en Unity
═══════════════════════════════════════════════════════════════════════════════

1. Crear la escena
   → File > New Scene > Basic (Built-in)
   → Save As "Island3" en Assets/Scenes/

2. GameObject StickFigure (esqueleto en vivo)
   → Hierarchy > Create Empty → renombrar "StickFigure"
   → Position (0, 0, 0)
   → Add Component: StickFigureUDP
   → Inspector: CenterOnHips = ✓, Scale = 4

3. GameObject MirrorGame
   → Hierarchy > Create Empty → renombrar "MirrorGame"
   → Add Component: MirrorWordGameUDP
   → Add Component: AudioSource (Spatial Blend = 0)

4. Canvas de UI
   → Hierarchy > UI > Canvas (Render Mode: Screen Space - Overlay)
   → Dentro del Canvas crear:
     a) TMP Text "WordText"           - anchor top-center, fontSize 120
     b) TMP Text "ScoreText"          - anchor top-left,   fontSize 40
     c) TMP Text "FeedbackText"       - anchor center,     fontSize 60
     d) TMP Text "CountdownText"      - anchor top-right,  fontSize 80
     e) UI Image "HoldFillBar"
        - anchor bottom-center, size (600, 30)
        - Image Type: Filled
        - Fill Method: Horizontal, Fill Origin: Left
        - Color: verde neon
     f) UI Button "BackButton"
        - anchor bottom-left, size (150, 60)

5. Enlazar referencias en MirrorGame
   Inspector de MirrorGame > MirrorWordGameUDP:
     - wordText        → WordText
     - scoreText       → ScoreText
     - feedbackText    → FeedbackText
     - countdownText   → CountdownText
     - holdFillBar     → HoldFillBar
     - stickFigure     → StickFigure (arrastrar el GameObject)
     - audioSource     → el AudioSource del mismo GameObject
     - correctClip / wrongClip → SFX

6. Enlazar boton BackToMenu
   BackButton > Button (Script) > OnClick > "+"
   Arrastrar "MirrorGame" → dropdown: MirrorWordGameUDP > BackToMenu()

7. Camara
   → Main Camera: Position (0, 0, -6), Rotation (0, 0, 0)
   → Clear Flags: Solid Color, Background: negro u oscuro
     (los colores neon destacan mas sobre fondo oscuro)

8. PoseReceiverUDP
   → Verificar que existe un GameObject con PoseReceiverUDP (Port 5052).
   → Si no: Create Empty "PoseReceiver" → Add Component PoseReceiverUDP.

9. Play Mode
   → Lanzar Python UDP en puerto 5052
   → La palabra aparece, el stickman se pinta por partes:
     * rojo en las partes que no cumplen la pose
     * verde en las que si cumplen
     * al completar 1.5 s sostenidos → +10 pts y todo el stickman verde

═══════════════════════════════════════════════════════════════════════════════
*/
