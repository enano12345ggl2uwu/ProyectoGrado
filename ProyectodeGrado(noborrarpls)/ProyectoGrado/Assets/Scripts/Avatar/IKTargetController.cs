using UnityEngine;

/// <summary>
/// Posiciona IK targets relativo al hueso REAL de caderas de Amy (no transform.position).
/// Usa la misma conversion L() del AvatarControllerUDP V4 que funcionaba.
/// Mirror = intercambio de landmarks (persona izq → Amy der), igual que V4 hacia con huesos.
/// </summary>
public class IKTargetController : MonoBehaviour
{
    [Header("IK Targets")]
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;

    [Header("IK Hints (opcional)")]
    public Transform leftHandHint;
    public Transform rightHandHint;
    public Transform leftFootHint;
    public Transform rightFootHint;

    [Header("Referencias")]
    public Animator animator;

    [Header("Config")]
    public bool mirrorMode = true;
    [Tooltip("Escala de extremidades. Amy escala 3 → probar 5-8")]
    public float limbScale = 6f;
    public float smoothSpeed = 10f;

    private Transform hipsBone;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("[IKTargetController] Requiere Animator Humanoid");
            enabled = false;
            return;
        }
        hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
        Debug.Log($"[IKTargetController] Hips bone en {hipsBone.position}");
    }

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;
        if (hipsBone == null) return;

        // Centro del cuerpo en espacio L()
        Vector3 bodyCenter = (L(23) + L(24) + L(11) + L(12)) * 0.25f;

        if (mirrorMode)
        {
            // INTERCAMBIO: persona izq → Amy der (espejo visual)
            MoveTarget(rightHandTarget, 15, bodyCenter);
            MoveTarget(leftHandTarget, 16, bodyCenter);
            MoveTarget(rightFootTarget, 27, bodyCenter);
            MoveTarget(leftFootTarget, 28, bodyCenter);

            MoveTarget(rightHandHint, 13, bodyCenter);
            MoveTarget(leftHandHint, 14, bodyCenter);
            MoveTarget(rightFootHint, 25, bodyCenter);
            MoveTarget(leftFootHint, 26, bodyCenter);
        }
        else
        {
            MoveTarget(leftHandTarget, 15, bodyCenter);
            MoveTarget(rightHandTarget, 16, bodyCenter);
            MoveTarget(leftFootTarget, 27, bodyCenter);
            MoveTarget(rightFootTarget, 28, bodyCenter);

            MoveTarget(leftHandHint, 13, bodyCenter);
            MoveTarget(rightHandHint, 14, bodyCenter);
            MoveTarget(leftFootHint, 25, bodyCenter);
            MoveTarget(rightFootHint, 26, bodyCenter);
        }
    }

    void MoveTarget(Transform target, int landmarkIdx, Vector3 bodyCenter)
    {
        if (target == null) return;

        Vector3 rel = L(landmarkIdx) - bodyCenter;

        // Referencia = hueso real de caderas (a la altura correcta de Amy escala 3)
        // No transform.position (que esta en el suelo)
        Vector3 worldPos = hipsBone.position + rel * limbScale;

        target.position = Vector3.Lerp(target.position, worldPos, Time.deltaTime * smoothSpeed);
    }

    /// <summary>
    /// EXACTA misma conversion del AvatarControllerUDP V4 que funcionaba.
    /// </summary>
    Vector3 L(int index)
    {
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(index);
        float x = mirrorMode ? -(lm.x - 0.5f) : (lm.x - 0.5f);
        float y = -(lm.y - 0.5f);
        float z = -lm.z;
        return new Vector3(x, y, z);
    }
}
