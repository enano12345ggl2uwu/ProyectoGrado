using UnityEngine;

/// <summary>
/// Controla un avatar Humanoid (Amy de Mixamo) usando landmarks de MediaPipe.
/// Version 4: restaura T-pose cada frame, detecta eje automatico y mueve el cuerpo en el espacio.
/// </summary>
public class AvatarControllerUDP : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Config")]
    public float smoothing = 10f;
    public bool mirrorMode = true;

    [Header("Movimiento corporal")]
    public float positionScale = 4f;
    public float positionSmoothing = 8f;
    public Vector3 positionOffset = new Vector3(0, 0, 0);

    // Huesos
    private Transform hips, spine;
    private Transform leftUpperArm, rightUpperArm;
    private Transform leftLowerArm, rightLowerArm;
    private Transform leftHand, rightHand;
    private Transform leftUpperLeg, rightUpperLeg;
    private Transform leftLowerLeg, rightLowerLeg;
    private Transform leftFoot, rightFoot;

    // Rotaciones iniciales (T-pose) en espacio local
    private Quaternion initHips, initSpine;
    private Quaternion initLUpperArm, initRUpperArm;
    private Quaternion initLLowerArm, initRLowerArm;
    private Quaternion initLUpperLeg, initRUpperLeg;
    private Quaternion initLLowerLeg, initRLowerLeg;

    // Ejes locales de cada hueso en T-pose (calculados automaticamente)
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
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
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

        // Guardar rotaciones iniciales LOCALES
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

        // Calcular el eje local de cada hueso mirando a su hijo
        axisLUpperArm = GetBoneAxis(leftUpperArm, leftLowerArm);
        axisRUpperArm = GetBoneAxis(rightUpperArm, rightLowerArm);
        axisLLowerArm = GetBoneAxis(leftLowerArm, leftHand);
        axisRLowerArm = GetBoneAxis(rightLowerArm, rightHand);
        axisLUpperLeg = GetBoneAxis(leftUpperLeg, leftLowerLeg);
        axisRUpperLeg = GetBoneAxis(rightUpperLeg, rightLowerLeg);
        axisLLowerLeg = GetBoneAxis(leftLowerLeg, leftFoot);
        axisRLowerLeg = GetBoneAxis(rightLowerLeg, rightFoot);
    }

    /// <summary>
    /// Calcula el eje local hacia el que apunta un hueso en T-pose, usando la posicion de su hijo.
    /// </summary>
    Vector3 GetBoneAxis(Transform bone, Transform child)
    {
        if (bone == null || child == null) return Vector3.down;
        Vector3 worldDir = (child.position - bone.position).normalized;
        return bone.InverseTransformDirection(worldDir);
    }

    void LateUpdate()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        // PASO 1: restaurar T-pose (evita acumulacion)
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

        // PASO 2: obtener landmarks en espacio Unity
        Vector3 lShoulder = L(11), rShoulder = L(12);
        Vector3 lElbow = L(13), rElbow = L(14);
        Vector3 lWrist = L(15), rWrist = L(16);
        Vector3 lHip = L(23), rHip = L(24);
        Vector3 lKnee = L(25), rKnee = L(26);
        Vector3 lAnkle = L(27), rAnkle = L(28);

        // PASO 3: rotar cada hueso
        // Con mirror activo, izquierda y derecha se intercambian
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

        // PASO 4: mover el cuerpo en el espacio segun el centro de las caderas
        MoveBody(lHip, rHip, lShoulder, rShoulder);
    }

    /// <summary>
    /// Rota un hueso para que su eje local apunte de "from" hacia "to" en espacio mundo.
    /// </summary>
    void AimBone(Transform bone, Vector3 from, Vector3 to, Vector3 localAxis)
    {
        if (bone == null) return;
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // Direccion actual del hueso en mundo (segun T-pose restaurada)
        Vector3 currentWorldDir = bone.TransformDirection(localAxis);

        // Rotacion adicional para alinear la direccion actual con la deseada
        Quaternion delta = Quaternion.FromToRotation(currentWorldDir, dir);
        bone.rotation = delta * bone.rotation;
    }

    /// <summary>
    /// Mueve todo el cuerpo de Amy en el espacio segun el centro del torso detectado.
    /// </summary>
    void MoveBody(Vector3 lHip, Vector3 rHip, Vector3 lShoulder, Vector3 rShoulder)
    {
        // Centro del cuerpo (promedio caderas + hombros)
        Vector3 bodyCenter = (lHip + rHip + lShoulder + rShoulder) * 0.25f;

        // Escalar y aplicar offset
        Vector3 targetPos = bodyCenter * positionScale + positionOffset;

        // Mover suave
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmoothing);
    }

    /// <summary>
    /// Convierte landmark MediaPipe a espacio Unity.
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