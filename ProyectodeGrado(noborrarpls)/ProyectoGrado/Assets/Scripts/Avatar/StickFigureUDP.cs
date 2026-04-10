using UnityEngine;

/// <summary>
/// Dibuja los 33 landmarks de MediaPipe como esferas + líneas.
/// Útil para debug visual durante el desarrollo.
/// </summary>
public class StickFigureUDP : MonoBehaviour
{
    [Header("Visual")]
    public float sphereSize = 0.08f;
    public Color jointColor = Color.cyan;
    public Color boneColor = Color.white;
    public float scale = 5f;
    public Vector3 offset = new Vector3(0, 2, 0);

    private GameObject[] joints = new GameObject[33];
    private LineRenderer[] bones;

    // Conexiones entre landmarks (pares) para dibujar huesos
    private readonly int[,] connections = new int[,] {
        {11,12},{11,13},{13,15},{12,14},{14,16}, // brazos
        {11,23},{12,24},{23,24},                  // torso
        {23,25},{25,27},{24,26},{26,28},          // piernas
        {27,29},{29,31},{27,31},                  // pie izq
        {28,30},{30,32},{28,32}                   // pie der
    };

    void Start()
    {
        // Crear esferas
        for (int i = 0; i < 33; i++)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.parent = transform;
            s.transform.localScale = Vector3.one * sphereSize;
            var rend = s.GetComponent<Renderer>();
            rend.material.color = jointColor;
            Destroy(s.GetComponent<Collider>());
            joints[i] = s;
        }

        // Crear líneas
        int boneCount = connections.GetLength(0);
        bones = new LineRenderer[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            GameObject lineObj = new GameObject($"Bone_{i}");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = boneColor;
            lr.endColor = boneColor;
            lr.positionCount = 2;
            bones[i] = lr;
        }
    }

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected) return;

        // Posicionar esferas
        for (int i = 0; i < 33; i++)
        {
            Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(i);
            Vector3 pos = new Vector3(
                (lm.x - 0.5f) * scale,
                (0.5f - lm.y) * scale,
                lm.z * scale
            ) + offset;
            joints[i].transform.position = pos;
        }

        // Dibujar huesos
        for (int i = 0; i < bones.Length; i++)
        {
            int a = connections[i, 0];
            int b = connections[i, 1];
            bones[i].SetPosition(0, joints[a].transform.position);
            bones[i].SetPosition(1, joints[b].transform.position);
        }
    }
}
