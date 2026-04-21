using UnityEngine;

/// <summary>
/// Visualiza los landmarks de MediaPipe como esferas (joints) y cilindros 3D (bones).
/// El stickman sigue el cuerpo en pantalla. Cabeza anclada al centro craneal (orejas 7-8).
/// emissionIntensity multiplica los colores para control de Bloom.
/// </summary>
public class StickFigureUDP : MonoBehaviour
{
    [Header("Visual")]
    public float sphereSize = 0.20f;                              // Nodos visibles como puntos oscuros
    public float boneWidth  = 0.06f;
    public Color jointColor = new Color(0.2f, 0.2f, 0.2f, 1f);  // Gris oscuro
    public Color boneColor  = new Color(0.5f, 0.9f, 0.9f, 1f);  // Cyan claro

    [Header("Neon / Glow")]
    [Range(0.5f, 4f)] public float emissionIntensity = 1.8f;
    public bool enableGlow = true;

    [Header("Transform")]
    public float   scale  = 5f;
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    [Header("Smoothing")]
    [Range(1f, 40f)] public float boneSmoothing = 18f;

    [Header("Head Settings")]
    public float headSize  = 0.55f;
    public Color headColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public float eyeSize   = 0.13f;
    public Color eyeColor  = new Color(0.5f, 0.9f, 0.9f, 1f);

    // --- internos ---
    private GameObject[]  joints = new GameObject[33];
    private Transform[]   boneTransforms;
    private Material[]    _jointMats;
    private Material[]    _boneMats;

    private Vector3[]     _jointPositions  = new Vector3[33];
    private Vector3[]     _bonePosSmooth;
    private Quaternion[]  _boneRotSmooth;
    private Vector3[]     _boneScaleSmooth;
    private bool          _boneInitialized = false;

    // Cabeza, ojos, cuello
    private Material  _headMat;
    private Transform _headTransform;
    private Material  _eyeMat;
    private Transform _leftEye;
    private Transform _rightEye;
    private Transform _neckBone;
    private Material  _neckMat;

    // Landmarks de cara — visualmente ocultos, usados para cabeza/ojos
    private readonly int[] FACE_LANDMARKS = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    public int BoneCount => connections.GetLength(0);

    private readonly int[,] connections = new int[,]
    {
        {11,12},{11,13},{13,15},{12,14},{14,16},
        {11,23},{12,24},{23,24},
        {23,25},{25,27},{24,26},{26,28},
        {27,29},{29,31},{27,31},
        {28,30},{30,32},{28,32}
    };

    // Aplica glow multiplicando el color base
    private Color Glow(Color c) => enableGlow ? c * emissionIntensity : c;

    void Start()
    {
        // ─── Joints ───
        _jointMats = new Material[33];
        for (int i = 0; i < 33; i++)
        {
            GameObject s       = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name             = $"Joint_{i}";
            s.transform.parent = transform;
            s.transform.localScale = Vector3.one * sphereSize;
            Destroy(s.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Unlit/Color")) { color = Glow(jointColor) };
            s.GetComponent<Renderer>().sharedMaterial = mat;
            _jointMats[i] = mat;
            joints[i]     = s;
        }

        // Ocultar esferas de cara (posición sigue usándose internamente)
        foreach (int idx in FACE_LANDMARKS)
            joints[idx].transform.localScale = Vector3.zero;

        // ─── Bones ───
        int boneCount    = connections.GetLength(0);
        boneTransforms   = new Transform[boneCount];
        _boneMats        = new Material[boneCount];
        _bonePosSmooth   = new Vector3[boneCount];
        _boneRotSmooth   = new Quaternion[boneCount];
        _boneScaleSmooth = new Vector3[boneCount];

        for (int i = 0; i < boneCount; i++)
        {
            GameObject cyl       = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name             = $"Bone_{i}";
            cyl.transform.parent = transform;
            Destroy(cyl.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Unlit/Color")) { color = Glow(boneColor) };
            cyl.GetComponent<Renderer>().sharedMaterial = mat;
            _boneMats[i]        = mat;
            boneTransforms[i]   = cyl.transform;
            _boneRotSmooth[i]   = Quaternion.identity;
            _boneScaleSmooth[i] = new Vector3(boneWidth, 0f, boneWidth);
        }

        // ─── Cabeza ───
        GameObject head      = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name            = "Head";
        head.transform.parent = transform;
        head.transform.localScale = Vector3.one * headSize;
        Destroy(head.GetComponent<Collider>());
        _headMat = new Material(Shader.Find("Unlit/Color")) { color = headColor };
        head.GetComponent<Renderer>().sharedMaterial = _headMat;
        _headTransform = head.transform;

        // ─── Ojos ───
        _eyeMat   = new Material(Shader.Find("Unlit/Color")) { color = Glow(eyeColor) };
        _leftEye  = CreateSphere("Eye_L", eyeSize, _eyeMat);
        _rightEye = CreateSphere("Eye_R", eyeSize, _eyeMat);

        // ─── Cuello ───
        GameObject neck      = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neck.name            = "Neck";
        neck.transform.parent = transform;
        Destroy(neck.GetComponent<Collider>());
        _neckMat = new Material(Shader.Find("Unlit/Color")) { color = Glow(boneColor) };
        neck.GetComponent<Renderer>().sharedMaterial = _neckMat;
        _neckBone = neck.transform;
    }

    private Transform CreateSphere(string name, float size, Material mat)
    {
        GameObject go        = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name              = name;
        go.transform.parent  = transform;
        go.transform.localScale = Vector3.one * size;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    // ─── Color API para MirrorWordGameUDP ───

    public void SetAllJointsColor(Color c)
    {
        if (_jointMats == null) return;
        for (int i = 11; i < _jointMats.Length; i++)
            if (_jointMats[i] != null) _jointMats[i].color = Glow(c);
        if (_headMat != null) _headMat.color = c; // cabeza no brilla con feedback
        if (_neckMat != null) _neckMat.color = Glow(c);
    }

    public void SetAllBonesColor(Color c)
    {
        if (_boneMats == null) return;
        for (int i = 0; i < _boneMats.Length; i++)
            if (_boneMats[i] != null) _boneMats[i].color = Glow(c);
        if (_neckMat != null) _neckMat.color = Glow(c);
    }

    public void SetJointColor(int landmarkIndex, Color c)
    {
        if (_jointMats == null || landmarkIndex < 0 || landmarkIndex >= _jointMats.Length) return;
        if (_jointMats[landmarkIndex] != null) _jointMats[landmarkIndex].color = Glow(c);
    }

    public void SetBoneColor(int boneIndex, Color c)
    {
        if (_boneMats == null || boneIndex < 0 || boneIndex >= _boneMats.Length) return;
        if (_boneMats[boneIndex] != null) _boneMats[boneIndex].color = Glow(c);
    }

    public void ResetColors()
    {
        if (_jointMats == null) return;
        for (int i = 11; i < _jointMats.Length; i++)
            if (_jointMats[i] != null) _jointMats[i].color = Glow(jointColor);
        if (_headMat != null) _headMat.color = headColor;
        if (_eyeMat  != null) _eyeMat.color  = Glow(eyeColor);
        if (_neckMat != null) _neckMat.color  = Glow(boneColor);
        if (_boneMats != null)
            for (int i = 0; i < _boneMats.Length; i++)
                if (_boneMats[i] != null) _boneMats[i].color = Glow(boneColor);
    }

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return;

        // ─── Joints mapeados directo desde coordenadas de cámara [0..1] ───
        for (int i = 0; i < 33; i++)
        {
            Vector3 lm  = PoseReceiverUDP.Instance.GetLandmark(i);
            Vector3 pos = new Vector3(
                (lm.x - 0.5f) * scale,
                (0.5f - lm.y) * scale,
                lm.z * scale
            ) + offset;

            joints[i].transform.position = pos;
            _jointPositions[i]           = pos;
        }

        // ─── Cabeza: centro craneal = midpoint orejas (landmarks 7 y 8) ───
        _headTransform.position = (_jointPositions[7] + _jointPositions[8]) * 0.5f;

        // ─── Ojos: landmarks 2 (ojo izq inner) y 5 (ojo der inner) ───
        _leftEye.position  = _jointPositions[2];
        _rightEye.position = _jointPositions[5];

        // ─── Cuello: midpoint hombros → cabeza ───
        Vector3 shoulderMid = (_jointPositions[11] + _jointPositions[12]) * 0.5f;
        UpdateCylinder(_neckBone, shoulderMid, _headTransform.position);

        // ─── Bones con suavizado ───
        float t = Mathf.Clamp01(Time.deltaTime * boneSmoothing);
        for (int i = 0; i < boneTransforms.Length; i++)
        {
            Vector3 a   = _jointPositions[connections[i, 0]];
            Vector3 b   = _jointPositions[connections[i, 1]];
            Vector3 dir = b - a;
            float   len = dir.magnitude;
            if (len < 1e-4f) continue;

            Vector3    targetPos   = (a + b) * 0.5f;
            Quaternion targetRot   = Quaternion.FromToRotation(Vector3.up, dir / len);
            Vector3    targetScale = new Vector3(boneWidth, len * 0.5f, boneWidth);

            if (!_boneInitialized)
            {
                _bonePosSmooth[i]   = targetPos;
                _boneRotSmooth[i]   = targetRot;
                _boneScaleSmooth[i] = targetScale;
            }
            else
            {
                _bonePosSmooth[i]   = Vector3.Lerp(_bonePosSmooth[i], targetPos, t);
                _boneRotSmooth[i]   = Quaternion.Slerp(_boneRotSmooth[i], targetRot, t);
                _boneScaleSmooth[i] = Vector3.Lerp(_boneScaleSmooth[i], targetScale, t);
            }

            boneTransforms[i].SetPositionAndRotation(_bonePosSmooth[i], _boneRotSmooth[i]);
            boneTransforms[i].localScale = _boneScaleSmooth[i];
        }
        _boneInitialized = true;
    }

    private void UpdateCylinder(Transform cyl, Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float   len = dir.magnitude;
        if (len < 1e-4f) return;
        cyl.position   = (a + b) * 0.5f;
        cyl.rotation   = Quaternion.FromToRotation(Vector3.up, dir / len);
        cyl.localScale = new Vector3(boneWidth, len * 0.5f, boneWidth);
    }
}
