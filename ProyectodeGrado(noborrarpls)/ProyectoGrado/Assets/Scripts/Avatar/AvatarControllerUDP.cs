using UnityEngine;

/// <summary>
/// Controla el personaje Amy (Mixamo Humanoid) usando los landmarks de MediaPipe.
/// Mueve brazos, piernas y mantiene a Amy en el piso.
/// El torso NO se toca aqui — lo maneja TorsoController via Animation Rigging.
/// </summary>
public class AvatarControllerUDP : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Config")]
    public float groundY = 2.11f;
    public float rotationSpeed = 10f;
    public bool mirrorMode = true;

    [Header("Debug")]
    public bool mostrarLogs = true;

    [Header("Desplazamiento lateral")]
    public float displacementScale = 8f;
    public float displacementSpeed = 5f;

    private Transform leftShoulder, rightShoulder;
    private Transform leftElbow, rightElbow;
    private Transform leftHip, rightHip;
    private Transform leftKnee, rightKnee;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("[AvatarControllerUDP] Se requiere un Animator con rig Humanoid");
            enabled = false;
            return;
        }

        leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftKnee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightKnee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        Debug.Log($"[Avatar] Start - Huesos encontrados: LShoulder={leftShoulder != null} LHip={leftHip != null}");
    }

    void LateUpdate()
    {
        bool hayInstance = PoseReceiverUDP.Instance != null;
        bool hayPose = hayInstance && PoseReceiverUDP.Instance.poseDetected;

        

        if (!hayPose) return;

        RotateBone(leftShoulder, 11, 13);
        RotateBone(rightShoulder, 12, 14);
        RotateBone(leftElbow, 13, 15);
        RotateBone(rightElbow, 14, 16);
        RotateBone(leftHip, 23, 25);
        RotateBone(rightHip, 24, 26);
        RotateBone(leftKnee, 25, 27);
        RotateBone(rightKnee, 26, 28);

        KeepOnGround();
    }

    void RotateBone(Transform bone, int fromLandmark, int toLandmark)
    {
        if (bone == null) return;
        Vector3 from = GetWorldLandmark(fromLandmark);
        Vector3 to = GetWorldLandmark(toLandmark);
        Vector3 direction = (to - from).normalized;
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.FromToRotation(Vector3.down, direction);
        bone.rotation = Quaternion.Slerp(bone.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    void KeepOnGround()
{
    if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
    {
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
        return;
    }

    // Calcular centro de caderas del niño
    Vector3 leftHip = PoseReceiverUDP.Instance.GetLandmark(23);
    Vector3 rightHip = PoseReceiverUDP.Instance.GetLandmark(24);
    float hipCenterX = (leftHip.x + rightHip.x) * 0.5f;

    // Convertir de espacio MediaPipe (0-1) a espacio mundo
    // 0.5 = centro, 0 = izquierda, 1 = derecha
    float offsetX = (hipCenterX - 0.5f) * displacementScale;
    if (mirrorMode) offsetX = -offsetX;

    Vector3 newPos = transform.position;
    newPos.x = Mathf.Lerp(newPos.x, offsetX, Time.deltaTime * displacementSpeed);
    newPos.y = groundY;
    transform.position = newPos;
}

Vector3 GetWorldLandmark(int index)
{
    Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(index);
    float x = mirrorMode ? -(lm.x - 0.5f) : (lm.x - 0.5f);
    float y = lm.y - 0.5f;  // invertido: antes era 0.5f - lm.y
    return new Vector3(x, y, lm.z);
}
}