using UnityEngine;

/// <summary>
/// SOLO desplazamiento lateral + altura fija (groundY).
/// NO toca huesos — Animation Rigging (Two Bone IK) los controla.
/// </summary>
public class AvatarControllerUDP : MonoBehaviour
{
    [Header("Config")]
    public float groundY = 0f;
    public bool mirrorMode = true;

    [Header("Desplazamiento lateral")]
    public float displacementScale = 8f;
    public float displacementSpeed = 5f;

    void LateUpdate()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
            return;
        }

        Vector3 leftHip = PoseReceiverUDP.Instance.GetLandmark(23);
        Vector3 rightHip = PoseReceiverUDP.Instance.GetLandmark(24);
        float hipCenterX = (leftHip.x + rightHip.x) * 0.5f;

        float offsetX = (hipCenterX - 0.5f) * displacementScale;
        if (mirrorMode) offsetX = -offsetX;

        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(newPos.x, offsetX, Time.deltaTime * displacementSpeed);
        newPos.y = groundY;
        transform.position = newPos;
    }
}
