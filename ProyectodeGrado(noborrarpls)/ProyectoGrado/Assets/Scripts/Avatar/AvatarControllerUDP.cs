using UnityEngine;

/// <summary>
/// Controla un avatar Humanoid (Amy de Mixamo) usando landmarks de MediaPipe.
/// Version 8: giro por diferencial de hombros + extremidades + desplazamiento.
/// Animator Controller debe estar en None.
/// </summary>
public class AvatarControllerUDP : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Config")]
    public float smoothing = 10f;
    public float torsoRotationSpeed = 5f;
    public bool mirrorMode = true;

    [Header("Movimiento corporal")]
    public float positionScale = 5f;
    public float positionSmoothing = 8f;
    public Vector3 positionOffset = new Vector3(0, 0, 0);

    private Transform hips, spine;
    private Transform leftUpperArm, rightUpperArm;
    private Transform leftLowerArm, rightLowerArm;
    private Transform leftHand, rightHand;
    private Transform leftUpperLeg, rightUpperLeg;
    private Transform leftLowerLeg, rightLowerLeg;
    private Transform leftFoot, rightFoot;

    private Quaternion initHips, initSpine;
    private Quaternion initLUpperArm, initRUpperArm;
    private Quaternion initLLowerArm, initRLowerArm;
    private Quaternion initLUpperLeg, initRUpperLeg;
    private Quaternion initLLowerLeg, initRLowerLeg;

    private Vector3 axisLUpperArm, axisRUpperArm;
    private Vector3 axisLLowerArm, axisRLowerArm;
    private Vector3 axisLUpperLeg, axisRUpperLeg;
    private Vector3 axisLLowerLeg, axisRLowerLeg;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("[AvatarControllerUDP] Se requiere Animator Humanoid");
            enabled = false;
            return;
        }

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        spine = animator.GetBoneTransform(HumanBodyBones.Chest);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        initHips = hips.localRotation;
        initSpine = spine ? spine.localRotation : Quaternion.identity;
        initLUpperArm = leftUpperArm.localRotation;
        initRUpperArm = rightUpperArm.localRotation;
        initLLowerArm = leftLowerArm.localRotation;
        initRLowerArm = rightLowerArm.localRotation;
        initLUpperLeg = leftUpperLeg.localRotation;
        initRUpperLeg = rightUpperLeg.localRotation;
        initLLowerLeg = leftLowerLeg.localRotation;
        initRLowerLeg = rightLowerLeg.localRotation;

        axisLUpperArm = GetBoneAxis(leftUpperArm, leftLowerArm);
        axisRUpperArm = GetBoneAxis(rightUpperArm, rightLowerArm);
        axisLLowerArm = GetBoneAxis(leftLowerArm, leftHand);
        axisRLowerArm = GetBoneAxis(rightLowerArm, rightHand);
        axisLUpperLeg = GetBoneAxis(leftUpperLeg, leftLowerLeg);
        axisRUpperLeg = GetBoneAxis(rightUpperLeg, rightLowerLeg);
        axisLLowerLeg = GetBoneAxis(leftLowerLeg, leftFoot);
        axisRLowerLeg = GetBoneAxis(rightLowerLeg, rightFoot);
    }

    Vector3 GetBoneAxis(Transform bone, Transform child)
    {
        if (bone == null || child == null) return Vector3.down;
        Vector3 worldDir = (child.position - bone.position).normalized;
        return bone.InverseTransformDirection(worldDir);
    }

    void LateUpdate()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        // PASO 1: restaurar T-pose
        hips.localRotation = initHips;
        if (spine) spine.localRotation = initSpine;
        leftUpperArm.localRotation = initLUpperArm;
        rightUpperArm.localRotation = initRUpperArm;
        leftLowerArm.localRotation = initLLowerArm;
        rightLowerArm.localRotation = initRLowerArm;
        leftUpperLeg.localRotation = initLUpperLeg;
        rightUpperLeg.localRotation = initRUpperLeg;
        leftLowerLeg.localRotation = initLLowerLeg;
        rightLowerLeg.localRotation = initRLowerLeg;

        // PASO 2: obtener landmarks
        Vector3 lShoulder = L(11), rShoulder = L(12);
        Vector3 lElbow = L(13), rElbow = L(14);
        Vector3 lWrist = L(15), rWrist = L(16);
        Vector3 lHip = L(23), rHip = L(24);
        Vector3 lKnee = L(25), rKnee = L(26);
        Vector3 lAnkle = L(27), rAnkle = L(28);

        // PASO 3: rotar torso PRIMERO
        RotateTorso();

        // PASO 4: rotar extremidades
        Transform luArm = mirrorMode ? rightUpperArm : leftUpperArm;
        Transform ruArm = mirrorMode ? leftUpperArm : rightUpperArm;
        Transform llArm = mirrorMode ? rightLowerArm : leftLowerArm;
        Transform rlArm = mirrorMode ? leftLowerArm : rightLowerArm;
        Transform luLeg = mirrorMode ? rightUpperLeg : leftUpperLeg;
        Transform ruLeg = mirrorMode ? leftUpperLeg : rightUpperLeg;
        Transform llLeg = mirrorMode ? rightLowerLeg : leftLowerLeg;
        Transform rlLeg = mirrorMode ? leftLowerLeg : rightLowerLeg;

        Vector3 axLU = mirrorMode ? axisRUpperArm : axisLUpperArm;
        Vector3 axRU = mirrorMode ? axisLUpperArm : axisRUpperArm;
        Vector3 axLL = mirrorMode ? axisRLowerArm : axisLLowerArm;
        Vector3 axRL = mirrorMode ? axisLLowerArm : axisRLowerArm;
        Vector3 axLUL = mirrorMode ? axisRUpperLeg : axisLUpperLeg;
        Vector3 axRUL = mirrorMode ? axisLUpperLeg : axisRUpperLeg;
        Vector3 axLLL = mirrorMode ? axisRLowerLeg : axisLLowerLeg;
        Vector3 axRLL = mirrorMode ? axisLLowerLeg : axisRLowerLeg;

        AimBone(luArm, lShoulder, lElbow, axLU);
        AimBone(ruArm, rShoulder, rElbow, axRU);
        AimBone(llArm, lElbow, lWrist, axLL);
        AimBone(rlArm, rElbow, rWrist, axRL);
        AimBone(luLeg, lHip, lKnee, axLUL);
        AimBone(ruLeg, rHip, rKnee, axRUL);
        AimBone(llLeg, lKnee, lAnkle, axLLL);
        AimBone(rlLeg, rKnee, rAnkle, axRLL);

        // PASO 5: mover en el espacio
        MoveBody(lHip, rHip, lShoulder, rShoulder);
    }

    void AimBone(Transform bone, Vector3 from, Vector3 to, Vector3 localAxis)
    {
        if (bone == null) return;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Vector3 currentWorldDir = bone.TransformDirection(localAxis);
        Quaternion delta = Quaternion.FromToRotation(currentWorldDir, dir);
        bone.rotation = delta * bone.rotation;
    }

    void RotateTorso()
    {
        if (spine == null || hips == null) return;

        Vector3 lS = L(11);
        Vector3 rS = L(12);
        Vector3 lH = L(23);
        Vector3 rH = L(24);

        Vector3 hipCenter = (lH + rH) * 0.5f;
        Vector3 shoulderCenter = (lS + rS) * 0.5f;
        Vector3 spineUp = (shoulderCenter - hipCenter).normalized;
        Vector3 hipRight = (rH - lH).normalized;
        Vector3 forward = Vector3.Cross(hipRight, spineUp).normalized;

        if (forward.sqrMagnitude < 0.01f) return;

        hips.localRotation = initHips;
        spine.localRotation = initSpine;

        Quaternion targetRot = Quaternion.LookRotation(forward, spineUp);

        hips.rotation = Quaternion.Slerp(hips.rotation, targetRot, Time.deltaTime * torsoRotationSpeed);
        spine.rotation = Quaternion.Slerp(spine.rotation, targetRot, Time.deltaTime * (torsoRotationSpeed * 1.2f));

        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head != null)
        {
            head.rotation = Quaternion.Slerp(head.rotation, targetRot, Time.deltaTime * torsoRotationSpeed);
        }
    }
    
    void MoveBody(Vector3 lHip, Vector3 rHip, Vector3 lShoulder, Vector3 rShoulder)
    {
        Vector3 bodyCenter = (lHip + rHip + lShoulder + rShoulder) * 0.25f;
        Vector3 targetPos = new Vector3(
            bodyCenter.x * positionScale,
            bodyCenter.y * positionScale,
            bodyCenter.z * positionScale
        ) + positionOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmoothing);
    }

    Vector3 L(int index)
    {
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(index);
        // World landmarks: en metros desde el centro de las caderas
        // X: positivo a la derecha del usuario
        // Y: positivo hacia arriba
        // Z: positivo hacia atras (lejos de la camara)
        float x = mirrorMode ? -lm.x : lm.x;
        float y = -lm.y; // invertir porque MediaPipe Y va hacia abajo
        float z = -lm.z; // invertir para que adelante sea +Z
        return new Vector3(x, y, z);
    }
}