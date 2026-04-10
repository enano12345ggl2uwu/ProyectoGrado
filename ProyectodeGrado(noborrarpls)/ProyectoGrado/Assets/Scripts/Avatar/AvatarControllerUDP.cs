using UnityEngine;

/// <summary>
/// Controla el personaje Amy (Mixamo Humanoid) usando los landmarks de MediaPipe.
/// Requiere un Animator con rig Humanoid asignado.
/// </summary>
public class AvatarControllerUDP : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;

    [Header("Config")]
    public float groundY = 0f;
    public float torsoRotationSpeed = 5f;
    public bool mirrorMode = true;

    private Transform leftShoulder, rightShoulder;
    private Transform leftElbow, rightElbow;
    private Transform leftWrist, rightWrist;
    private Transform leftHip, rightHip;
    private Transform leftKnee, rightKnee;
    private Transform spine;

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
        leftWrist = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightWrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
        leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftKnee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightKnee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
    }

    void LateUpdate()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        RotateBone(leftShoulder, 11, 13);
        RotateBone(rightShoulder, 12, 14);
        RotateBone(leftElbow, 13, 15);
        RotateBone(rightElbow, 14, 16);
        RotateBone(leftHip, 23, 25);
        RotateBone(rightHip, 24, 26);
        RotateBone(leftKnee, 25, 27);
        RotateBone(rightKnee, 26, 28);

        RotateTorso();
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
        bone.rotation = Quaternion.Slerp(bone.rotation, targetRot, Time.deltaTime * 10f);
    }

    void RotateTorso()
    {
        if (spine == null) return;
        Vector3 lHip = GetWorldLandmark(23);
        Vector3 rHip = GetWorldLandmark(24);
        Vector3 hipDir = (rHip - lHip).normalized;
        if (mirrorMode) hipDir.x = -hipDir.x;
        Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(hipDir, Vector3.forward));
        spine.rotation = Quaternion.Slerp(spine.rotation, targetRot, Time.deltaTime * torsoRotationSpeed);
    }

    void KeepOnGround()
    {
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
    }

    Vector3 GetWorldLandmark(int index)
    {
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(index);
        float x = mirrorMode ? -(lm.x - 0.5f) : (lm.x - 0.5f);
        return new Vector3(x, 0.5f - lm.y, lm.z);
    }
}
