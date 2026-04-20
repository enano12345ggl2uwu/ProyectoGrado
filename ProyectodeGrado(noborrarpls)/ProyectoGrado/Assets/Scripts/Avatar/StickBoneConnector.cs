using UnityEngine;

public class StickBoneConnector : MonoBehaviour {
    public LineRenderer lineRenderer;
    public Transform point1; // Ej: Hombro (11)
    public Transform point2; // Ej: Codo (13)

    void Update() {
        if (point1 != null && point2 != null) {
            lineRenderer.SetPosition(0, point1.position);
            lineRenderer.SetPosition(1, point2.position);
        }
    }
}