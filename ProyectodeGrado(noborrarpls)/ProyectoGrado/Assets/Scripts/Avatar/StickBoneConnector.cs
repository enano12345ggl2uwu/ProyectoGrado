using UnityEngine;

/// <summary>
/// Une visualmente dos Transforms con un LineRenderer (ej: hombro - codo).
/// SETUP:
///  1. Crea un GameObject con LineRenderer (Component > Effects > Line Renderer).
///  2. Pon este script y arrastra los dos Transforms (point1, point2) y el LineRenderer.
///  3. El LineRenderer debe tener un Material con shader tipo "Sprites/Default".
/// </summary>
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