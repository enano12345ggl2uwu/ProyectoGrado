using UnityEngine;

/// <summary>
/// Visualiza los 33 landmarks de MediaPipe como esferas y huesos (LineRenderer).
/// El esqueleto se ancla a <c>transform.position</c> y, opcionalmente, se centra
/// en el punto medio de las caderas (landmarks 23 y 24) para evitar derive
/// respecto al origen de la camara.
/// Colores neon por defecto para alta visibilidad (niños con TDAH).
/// Expone API publica para que otros scripts pinten joints/bones dinamicamente.
/// </summary>
public class StickFigureUDP : MonoBehaviour
{
    [Header("Visual")]
    public float sphereSize = 0.18f;
    public float boneWidth  = 0.08f;
    /// <summary>Color por defecto de las esferas (joints). Neon cyan.</summary>
    public Color jointColor = new Color(0.2f, 1f, 1f, 1f);
    /// <summary>Color por defecto de las lineas (bones). Neon magenta.</summary>
    public Color boneColor  = new Color(1f, 0.25f, 0.9f, 1f);

    [Header("Transform")]
    public float   scale  = 4f;
    public Vector3 offset = Vector3.zero;

    [Header("Centering")]
    /// <summary>
    /// Cuando esta activo, resta el punto medio de caderas (lm 23/24) a todos los
    /// joints, de modo que las caderas siempre queden en <c>transform.position + offset</c>.
    /// </summary>
    public bool centerOnHips = true;

    // --- internos ---
    private GameObject[]   joints = new GameObject[33];
    private LineRenderer[] bones;

    /// <summary>Cantidad de huesos (pares de conexion) expuesta para scripts externos.</summary>
    public int BoneCount => connections.GetLength(0);

    // 18 conexiones de huesos (indices de landmarks MediaPipe)
    // Orden importa: otros scripts referencian estos indices.
    //  0: {11,12} hombros
    //  1: {11,13} brazo izq superior
    //  2: {13,15} brazo izq inferior
    //  3: {12,14} brazo der superior
    //  4: {14,16} brazo der inferior
    //  5: {11,23} torso izq
    //  6: {12,24} torso der
    //  7: {23,24} caderas
    //  8: {23,25} pierna izq superior
    //  9: {25,27} pierna izq inferior
    // 10: {24,26} pierna der superior
    // 11: {26,28} pierna der inferior
    // 12-14: pie izq
    // 15-17: pie der
    private readonly int[,] connections = new int[,]
    {
        {11,12},{11,13},{13,15},{12,14},{14,16},
        {11,23},{12,24},{23,24},
        {23,25},{25,27},{24,26},{26,28},
        {27,29},{29,31},{27,31},
        {28,30},{30,32},{28,32}
    };

    void Start()
    {
        // Crear esferas (una vez)
        for (int i = 0; i < 33; i++)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.parent     = transform;
            s.transform.localScale = Vector3.one * sphereSize;
            s.GetComponent<Renderer>().material.color = jointColor;
            Destroy(s.GetComponent<Collider>());
            joints[i] = s;
        }

        // Crear LineRenderers (una vez, sin allocar material por frame)
        int boneCount = connections.GetLength(0);
        bones = new LineRenderer[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            GameObject lineObj = new GameObject($"Bone_{i}");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth    = boneWidth;
            lr.endWidth      = boneWidth;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.startColor    = boneColor;
            lr.endColor      = boneColor;
            lr.positionCount = 2;
            bones[i] = lr;
        }
    }

    /// <summary>Pinta todos los joints con el color indicado.</summary>
    public void SetAllJointsColor(Color c)
    {
        if (joints == null) return;
        for (int i = 0; i < joints.Length; i++)
            if (joints[i] != null)
                joints[i].GetComponent<Renderer>().material.color = c;
    }

    /// <summary>Pinta todas las lineas (bones) con el color indicado.</summary>
    public void SetAllBonesColor(Color c)
    {
        if (bones == null) return;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null)
            {
                bones[i].startColor = c;
                bones[i].endColor   = c;
            }
    }

    /// <summary>Pinta un joint (indice MediaPipe 0-32) con el color indicado.</summary>
    public void SetJointColor(int landmarkIndex, Color c)
    {
        if (joints == null || landmarkIndex < 0 || landmarkIndex >= joints.Length) return;
        if (joints[landmarkIndex] != null)
            joints[landmarkIndex].GetComponent<Renderer>().material.color = c;
    }

    /// <summary>Pinta un bone (indice 0 a BoneCount-1) con el color indicado.</summary>
    public void SetBoneColor(int boneIndex, Color c)
    {
        if (bones == null || boneIndex < 0 || boneIndex >= bones.Length) return;
        bones[boneIndex].startColor = c;
        bones[boneIndex].endColor   = c;
    }

    /// <summary>Restaura todos los joints y bones a los colores base (jointColor/boneColor).</summary>
    public void ResetColors()
    {
        SetAllJointsColor(jointColor);
        SetAllBonesColor(boneColor);
    }

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return;

        // Punto medio de caderas (solo X e Y; Z se ignora para que el stickman
        // siempre quede en el plano del ancla, delante del fondo de la escena)
        Vector3 hipCenter = Vector3.zero;
        if (centerOnHips)
        {
            Vector3 h23 = PoseReceiverUDP.Instance.GetLandmark(23);
            Vector3 h24 = PoseReceiverUDP.Instance.GetLandmark(24);
            hipCenter = new Vector3(
                ((h23.x + h24.x) * 0.5f - 0.5f) * scale,
                (0.5f - (h23.y + h24.y) * 0.5f) * scale,
                0f
            );
        }

        // Ancla: posicion del GameObject + offset del Inspector
        Vector3 anchor = transform.position + offset;

        // Posicionar esferas (Z fijo en ancla; solo X e Y siguen al cuerpo)
        for (int i = 0; i < 33; i++)
        {
            Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(i);
            Vector3 pos = new Vector3(
                (lm.x - 0.5f) * scale,
                (0.5f - lm.y) * scale,
                0f
            ) - hipCenter + anchor;
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
