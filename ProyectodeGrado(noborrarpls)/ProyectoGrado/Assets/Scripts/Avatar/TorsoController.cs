using UnityEngine;

/// <summary>
/// Detecta cuanto gira el torso del jugador usando el ratio de hombros
/// vs altura del torso. Mueve un TorsoTarget para que Animation Rigging
/// rote el torso de Amy automaticamente.
/// Capa 1: calculo de currentTurnAngle (-90 a +90 grados)
/// Capa 2: feedback visual y sonoro al girar
/// Capa 3: mueve el TorsoTarget para rotar el torso via rig
/// </summary>
public class TorsoController : MonoBehaviour
{
    [Header("Capa 1 - Deteccion de giro")]
    [Range(0.01f, 1f)]
    public float smoothing = 0.15f;
    public float minValidRatio = 0.05f;

    [Header("Capa 2 - Feedback")]
    public float triggerAngle = 30f;
    public ParticleSystem turnParticles;
    public AudioSource audioSource;
    public AudioClip swooshClip;

    [Header("Capa 3 - Torso Target (Animation Rigging)")]
    [Tooltip("Arrastra aqui el TorsoTarget que hizo rotar a Amy")]
    public Transform torsoTarget;

    [Tooltip("Posicion base del target (cuando estas de frente)")]
    public Vector3 targetBasePosition = new Vector3(0, 1.4f, 1f);

    [Tooltip("Cuanto se mueve el target horizontalmente segun el giro")]
    public float targetSwingDistance = 2f;

    [Tooltip("Invertir direccion del giro")]
    public bool invertDirection = false;

    [Tooltip("Angulo minimo antes de mover el target (evita jitter en reposo)")]
    public float deadzone = 5f;

    [Header("Debug (solo lectura)")]
    public float currentTurnAngle = 0f;
    public float maxRatioSeen = 0.01f;
    public bool feedbackActive = false;

    // Landmarks MediaPipe
    private const int L_SHOULDER = 11;
    private const int R_SHOULDER = 12;
    private const int L_HIP = 23;
    private const int R_HIP = 24;

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return;

        CalcularAngulo();
        EvaluarFeedback();
        MoverTorsoTarget();
    }

    void CalcularAngulo()
    {
        Vector3 lShoulder = PoseReceiverUDP.Instance.GetLandmark(L_SHOULDER);
        Vector3 rShoulder = PoseReceiverUDP.Instance.GetLandmark(R_SHOULDER);
        Vector3 lHip = PoseReceiverUDP.Instance.GetLandmark(L_HIP);
        Vector3 rHip = PoseReceiverUDP.Instance.GetLandmark(R_HIP);

        float shoulderWidth = Mathf.Abs(rShoulder.x - lShoulder.x);

        Vector3 shoulderMid = (lShoulder + rShoulder) * 0.5f;
        Vector3 hipMid = (lHip + rHip) * 0.5f;
        float torsoHeight = Vector3.Distance(shoulderMid, hipMid);

        if (torsoHeight < minValidRatio) return;

        float ratio = shoulderWidth / torsoHeight;

        if (ratio > maxRatioSeen) maxRatioSeen = ratio;

        float reduccion = 1f - (ratio / maxRatioSeen);
        reduccion = Mathf.Clamp01(reduccion);

        float anguloMagnitud = reduccion * 90f;

        float signo = Mathf.Sign(lShoulder.z - rShoulder.z);
        if (invertDirection) signo = -signo;

        float anguloObjetivo = anguloMagnitud * signo;

        currentTurnAngle = Mathf.Lerp(currentTurnAngle, anguloObjetivo, smoothing);
    }

    void EvaluarFeedback()
    {
        float anguloAbs = Mathf.Abs(currentTurnAngle);

        if (!feedbackActive && anguloAbs > triggerAngle)
        {
            feedbackActive = true;
            DispararFeedback();
        }
        else if (feedbackActive && anguloAbs < triggerAngle * 0.5f)
        {
            feedbackActive = false;
        }
    }

    void DispararFeedback()
    {
        if (turnParticles != null) turnParticles.Play();
        if (audioSource != null && swooshClip != null) audioSource.PlayOneShot(swooshClip);
        Debug.Log($"[TorsoController] Giro detectado: {currentTurnAngle:F1} grados");
    }

    void MoverTorsoTarget()
    {
        if (torsoTarget == null) return;

        // Aplicar deadzone para evitar jitter en reposo
        float angle = (Mathf.Abs(currentTurnAngle) > deadzone) ? currentTurnAngle : 0f;

        // Convertir angulo (-90 a +90) a desplazamiento X (-swing a +swing)
        float offsetX = (angle / 90f) * targetSwingDistance;

        Vector3 nuevaPos = targetBasePosition;
        nuevaPos.x += offsetX;

        torsoTarget.localPosition = nuevaPos;
    }

    public void ResetCalibracion()
    {
        maxRatioSeen = 0.01f;
        currentTurnAngle = 0f;
    }
}