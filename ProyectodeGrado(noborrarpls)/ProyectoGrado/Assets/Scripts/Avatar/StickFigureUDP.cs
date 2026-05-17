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

    [Header("Swim Bob")]
    public bool  swimEnabled        = true;
    [Range(0.5f, 5f)]  public float swimFrequency      = 2.5f;
    [Range(0f,   0.5f)] public float swimAmplitude     = 0.18f;
    [Range(0f,   0.05f)] public float swimSpeedThreshold = 0.008f;
    [Tooltip("AudioSource del stickfigure (o de la escena).")]
    public AudioSource swimAudioSource;
    [Tooltip("Clip que suena mientras se desplaza (loop).")]
    public AudioClip   swimClip;

    [Header("Correct Shake")]
    [Range(0f, 0.4f)] public float shakeIntensity = 0.12f;
    [Range(0f, 1f)]   public float shakeDuration  = 0.35f;

    [Header("Head Settings")]
    public float headSize  = 2.4f;
    public Color headColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public float eyeSize   = 0.13f;
    public Color eyeColor  = new Color(0.5f, 0.9f, 0.9f, 1f);

    [Header("Expression")]
    [Tooltip("Segundos que dura la expresion antes de volver a neutra.")]
    public float expressionDuration = 1.0f;
    [Tooltip("Tamano base de la boca (esfera escalada).")]
    public float mouthSize = 0.18f;

    [Header("Combo + Aura")]
    [Tooltip("Segundos sin acertar antes de perder el combo.")]
    public float comboFalloffSeconds = 4f;
    public bool  enableCombo = true;
    [Tooltip("Tamano max del aura (combo >= 10).")]
    public float auraMaxRadius = 4.5f;
    public Color auraColorLow  = new Color(0.3f, 0.85f, 0.4f, 0.25f);  // verde
    public Color auraColorMid  = new Color(1f,   0.8f,  0.2f, 0.30f);  // amarillo
    public Color auraColorHigh = new Color(1f,   0.45f, 0.2f, 0.35f);  // naranja
    public Color auraColorMax  = new Color(0.7f, 0.3f,  1f,   0.40f);  // morado

    [Header("Hand Trails")]
    public bool  enableHandTrails = true;
    [Range(0.05f, 1.5f)] public float trailTime  = 0.4f;
    [Range(0.01f, 0.5f)] public float trailWidth = 0.18f;

    [Header("Skins")]
    [Tooltip("Skins disponibles. La primera es la default. Tecla K cicla.")]
    public Skin[] skins = new Skin[]
    {
        new Skin{ name="Default",
                  jointColor=new Color(0.2f,0.2f,0.2f), boneColor=new Color(0.5f,0.9f,0.9f),
                  eyeColor=new Color(0.5f,0.9f,0.9f),   headColor=new Color(0.15f,0.15f,0.15f),
                  hat=HatType.None,
                  hatColor=new Color(1f,1f,1f),
                  shirtEnabled=false, shirtColor=new Color(0.5f,0.9f,0.9f),
                  pantsEnabled=false, pantsColor=new Color(0.3f,0.3f,0.5f) },
        new Skin{ name="Fire",
                  jointColor=new Color(0.3f,0.1f,0.0f), boneColor=new Color(1.0f,0.5f,0.1f),
                  eyeColor=new Color(1.0f,0.85f,0.2f),  headColor=new Color(0.25f,0.05f,0.0f),
                  hat=HatType.Cone,
                  hatColor=new Color(1.0f,0.3f,0.1f),
                  shirtEnabled=true, shirtColor=new Color(1.0f,0.5f,0.1f),
                  pantsEnabled=true, pantsColor=new Color(0.7f,0.15f,0.05f) },
        new Skin{ name="Ice",
                  jointColor=new Color(0.1f,0.2f,0.3f), boneColor=new Color(0.5f,0.85f,1.0f),
                  eyeColor=new Color(0.8f,0.95f,1.0f),  headColor=new Color(0.15f,0.2f,0.3f),
                  hat=HatType.Crown,
                  hatColor=new Color(0.7f,0.9f,1.0f),
                  shirtEnabled=true, shirtColor=new Color(0.9f,0.95f,1.0f),
                  pantsEnabled=true, pantsColor=new Color(0.3f,0.5f,0.8f) },
        new Skin{ name="Magic",
                  jointColor=new Color(0.2f,0.05f,0.3f), boneColor=new Color(0.85f,0.4f,1.0f),
                  eyeColor=new Color(1.0f,0.7f,1.0f),   headColor=new Color(0.2f,0.05f,0.25f),
                  hat=HatType.Cone,
                  hatColor=new Color(0.55f,0.2f,0.8f),
                  shirtEnabled=true, shirtColor=new Color(0.7f,0.35f,0.95f),
                  pantsEnabled=true, pantsColor=new Color(0.45f,0.15f,0.6f) },
        new Skin{ name="Vanilla",
                  jointColor=new Color(0.2f,0.2f,0.2f), boneColor=new Color(0.5f,0.9f,0.9f),
                  eyeColor=new Color(0.5f,0.9f,0.9f),   headColor=new Color(0.15f,0.15f,0.15f),
                  hat=HatType.None,
                  hatColor=new Color(1f,1f,1f),
                  shirtEnabled=false, shirtColor=new Color(0.5f,0.9f,0.9f),
                  pantsEnabled=false, pantsColor=new Color(0.3f,0.3f,0.5f) },
    };
    public bool persistSkin = true;

    public enum HatType { None, Cone, Cylinder, Crown, Cap }

    [System.Serializable]
    public class Skin
    {
        public string name;
        public Color  jointColor;
        public Color  boneColor;
        public Color  eyeColor;
        public Color  headColor;

        [Header("Ropa")]
        public HatType hat          = HatType.None;
        public Color   hatColor     = Color.white;
        public bool    shirtEnabled = false;
        public Color   shirtColor   = Color.gray;
        public bool    pantsEnabled = false;
        public Color   pantsColor   = Color.gray;
    }

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

    // Swim + Shake
    private float _prevHipX    = 0f;
    private float _swimPhase   = 0f;
    private float _swimBlend   = 0f;
    private float _shakeTimer  = 0f;
    private float _shakeY      = 0f;

    // Cabeza, ojos, cuello
    private Material  _headMat;
    private Transform _headTransform;
    private Material  _eyeMat;
    private Transform _leftEye;
    private Transform _rightEye;
    private Transform _neckBone;
    private Material  _neckMat;

    // Expresion (cara) + combo + aura + trails + skin
    public enum ExpressionState { Neutral, Happy, Sad }
    private ExpressionState _expression      = ExpressionState.Neutral;
    private float           _expressionTimer = 0f;
    private Transform       _mouthTransform;
    private Material        _mouthMat;
    private int             _combo           = 0;
    private float           _comboTimer      = 0f;
    private Transform       _auraTransform;
    private Material        _auraMat;
    private TrailRenderer   _trailL;
    private TrailRenderer   _trailR;
    private int             _currentSkin     = 0;

    // Ropa
    private Transform _hatTransform;
    private Material  _hatMat;
    private HatType   _currentHatType  = HatType.None;
    private Vector3   _hatTargetScale  = Vector3.zero;
    private Transform _shirtTransform;
    private Material  _shirtMat;
    private Transform _pantsLTransform;
    private Transform _pantsRTransform;
    private Material  _pantsMat;
    private bool      _firstPoseFrame  = true;

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

    // Anti-shader-stripping: prioriza un material asset en Resources/ (su shader
    // nunca se strippea porque está referenciado por un asset). Cae a Shader.Find
    // si el asset no existe.
    private static Material _resourceTemplate;
    private static bool _resourceLookupDone;

    private Material MakeUnlitMat(Color c)
    {
        if (!_resourceLookupDone)
        {
            _resourceTemplate = Resources.Load<Material>("StickFigureMat");
            _resourceLookupDone = true;
            if (_resourceTemplate == null)
                Debug.LogWarning("[StickFigureUDP] Resources/StickFigureMat.mat no encontrado — usando Shader.Find fallback (riesgo de stripping en build).");
        }

        if (_resourceTemplate != null)
            return new Material(_resourceTemplate) { color = c };

        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        return new Material(shader) { color = c };
    }

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

            var mat = MakeUnlitMat(Glow(jointColor));
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

            var mat = MakeUnlitMat(Glow(boneColor));
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
        _headMat = MakeUnlitMat(headColor);
        head.GetComponent<Renderer>().sharedMaterial = _headMat;
        _headTransform = head.transform;

        // ─── Ojos ───
        _eyeMat   = MakeUnlitMat(Glow(eyeColor));
        _leftEye  = CreateSphere("Eye_L", eyeSize, _eyeMat);
        _rightEye = CreateSphere("Eye_R", eyeSize, _eyeMat);

        // ─── Boca ───
        _mouthMat       = MakeUnlitMat(Glow(eyeColor));
        _mouthTransform = CreateSphere("Mouth", mouthSize, _mouthMat);

        // ─── Aura (combo) ───
        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aura.name = "ComboAura";
        aura.transform.parent = transform;
        Destroy(aura.GetComponent<Collider>());
        _auraMat = MakeUnlitMat(auraColorLow);
        // Usamos un shader transparente si esta disponible para alpha real.
        Shader transparentShader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
        if (transparentShader != null) _auraMat.shader = transparentShader;
        aura.GetComponent<Renderer>().sharedMaterial = _auraMat;
        aura.transform.localScale = Vector3.zero;
        _auraTransform = aura.transform;

        // ─── Trails de manos ───
        if (enableHandTrails)
        {
            _trailL = AddTrail(joints[15].transform, "TrailL");
            _trailR = AddTrail(joints[16].transform, "TrailR");
        }

        // ─── Camisa (cilindro entre hombros y caderas) ───
        // Escala 0 hasta que UpdateClothing la posicione con landmarks reales,
        // si no aparece como cilindro gigante en el origen antes de la primera pose.
        GameObject shirt      = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shirt.name            = "Shirt";
        shirt.transform.parent = transform;
        shirt.transform.localScale = Vector3.zero;
        Destroy(shirt.GetComponent<Collider>());
        _shirtMat = MakeUnlitMat(Glow(boneColor));
        shirt.GetComponent<Renderer>().sharedMaterial = _shirtMat;
        shirt.SetActive(false);
        _shirtTransform = shirt.transform;

        // ─── Pantalones (un cilindro por pierna) ───
        _pantsMat        = MakeUnlitMat(Glow(boneColor));
        _pantsLTransform = CreateCylinder("PantsL", _pantsMat);
        _pantsRTransform = CreateCylinder("PantsR", _pantsMat);
        _pantsLTransform.localScale = Vector3.zero;
        _pantsRTransform.localScale = Vector3.zero;
        _pantsLTransform.gameObject.SetActive(false);
        _pantsRTransform.gameObject.SetActive(false);

        // ─── Aplicar skin guardado (configura ropa y sombrero) ───
        ApplySavedSkin();

        // ─── Cuello ───
        GameObject neck      = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neck.name            = "Neck";
        neck.transform.parent = transform;
        Destroy(neck.GetComponent<Collider>());
        _neckMat = MakeUnlitMat(Glow(boneColor));
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

    private Transform CreateCylinder(string name, Material mat)
    {
        GameObject go        = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name              = name;
        go.transform.parent  = transform;
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
        Update_DebugInput();

        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
            return;

        // ─── Joints mapeados primero (los necesitamos validos antes de tocar trails y ropa) ───
        // Primer frame de pose: limpia los trails para que no dibujen una linea
        // desde el origen hasta la primera posicion de las muñecas.
        if (_firstPoseFrame)
        {
            // Aplica posiciones inmediatas a joints antes de habilitar trails
            for (int i = 0; i < 33; i++)
            {
                Vector3 lm0 = PoseReceiverUDP.Instance.GetLandmark(i);
                Vector3 p0  = new Vector3((lm0.x - 0.5f) * scale, (0.5f - lm0.y) * scale, lm0.z * scale) + offset;
                joints[i].transform.position = p0;
                _jointPositions[i]           = p0;
            }
            if (_trailL != null) _trailL.Clear();
            if (_trailR != null) _trailR.Clear();
            _prevHipX = (PoseReceiverUDP.Instance.GetLandmark(23).x +
                         PoseReceiverUDP.Instance.GetLandmark(24).x) * 0.5f;
            _firstPoseFrame = false;
        }

        // ─── Swim bob ───
        float hipX     = (PoseReceiverUDP.Instance.GetLandmark(23).x +
                          PoseReceiverUDP.Instance.GetLandmark(24).x) * 0.5f;
        float hipSpeed = Mathf.Abs(hipX - _prevHipX) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
        _prevHipX      = hipX;
        float swimTarget = (swimEnabled && hipSpeed > swimSpeedThreshold) ? 1f : 0f;
        _swimBlend = Mathf.Lerp(_swimBlend, swimTarget, Time.unscaledDeltaTime * 4f);
        if (_swimBlend > 0.01f)
            _swimPhase += Time.unscaledDeltaTime * swimFrequency * Mathf.PI * 2f;
        float swimY = Mathf.Sin(_swimPhase) * swimAmplitude * _swimBlend;

        if (swimAudioSource && swimClip)
        {
            if (_swimBlend > 0.2f && !swimAudioSource.isPlaying)
            {
                swimAudioSource.clip = swimClip;
                swimAudioSource.loop = true;
                swimAudioSource.Play();
            }
            else if (_swimBlend <= 0.2f && swimAudioSource.isPlaying)
            {
                swimAudioSource.Stop();
            }
        }

        // ─── Correct shake ───
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.unscaledDeltaTime;
            _shakeY = Random.Range(-shakeIntensity, shakeIntensity);
        }
        else { _shakeY = 0f; }

        float extraY = swimY + _shakeY;

        // ─── Joints mapeados directo desde coordenadas de cámara [0..1] ───
        for (int i = 0; i < 33; i++)
        {
            Vector3 lm  = PoseReceiverUDP.Instance.GetLandmark(i);
            Vector3 pos = new Vector3(
                (lm.x - 0.5f) * scale,
                (0.5f - lm.y) * scale + extraY,
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
        float t = Mathf.Clamp01(Time.unscaledDeltaTime * boneSmoothing);
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

        UpdateExpressionAndAura();
        UpdateClothing();
    }

    void UpdateClothing()
    {
        // Camisa: cilindro entre midpoint hombros y midpoint caderas
        if (_shirtTransform != null && _shirtTransform.gameObject.activeSelf)
        {
            Vector3 shoulderMid = (_jointPositions[11] + _jointPositions[12]) * 0.5f;
            Vector3 hipMid      = (_jointPositions[23] + _jointPositions[24]) * 0.5f;
            Vector3 dir = hipMid - shoulderMid;
            float   len = dir.magnitude;
            if (len > 1e-4f)
            {
                _shirtTransform.position = (shoulderMid + hipMid) * 0.5f;
                _shirtTransform.rotation = Quaternion.FromToRotation(Vector3.up, dir / len);
                float shoulderWidth = Vector3.Distance(_jointPositions[11], _jointPositions[12]);
                float thickness     = Mathf.Max(0.1f, shoulderWidth * 0.7f);
                _shirtTransform.localScale = new Vector3(thickness, len * 0.5f, thickness * 0.6f);
            }
        }

        // Pantalones: cadera -> rodilla por pierna
        if (_pantsLTransform != null && _pantsLTransform.gameObject.activeSelf)
        {
            float shoulderWidth = Vector3.Distance(_jointPositions[11], _jointPositions[12]);
            float thickness     = Mathf.Max(0.08f, shoulderWidth * 0.32f);
            UpdateLeg(_pantsLTransform, _jointPositions[23], _jointPositions[25], thickness);
            UpdateLeg(_pantsRTransform, _jointPositions[24], _jointPositions[26], thickness);
        }

        // Sombrero: en lo alto de la cabeza
        if (_hatTransform != null && _hatTransform.gameObject.activeSelf && _headTransform != null)
        {
            float lift = headSize * (_currentHatType == HatType.Crown ? 0.5f : 0.65f);
            _hatTransform.position = _headTransform.position + Vector3.up * lift;
            _hatTransform.rotation = Quaternion.identity;
            if (_hatTransform.localScale.sqrMagnitude < 0.001f)
                _hatTransform.localScale = _hatTargetScale;
        }
    }

    void UpdateLeg(Transform leg, Vector3 hip, Vector3 knee, float thickness)
    {
        Vector3 dir = knee - hip;
        float   len = dir.magnitude;
        if (len < 1e-4f) return;
        leg.position   = (hip + knee) * 0.5f;
        leg.rotation   = Quaternion.FromToRotation(Vector3.up, dir / len);
        leg.localScale = new Vector3(thickness, len * 0.5f, thickness);
    }

    public void TriggerShake() => _shakeTimer = shakeDuration;

    // ─────────────────────────────────────────────────────────────────────
    // API publica: feedback de minijuegos
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Llamar en acierto: incrementa combo, sonrisa, shake.</summary>
    public void RegisterCorrect()
    {
        if (enableCombo)
        {
            _combo++;
            _comboTimer = comboFalloffSeconds;
        }
        TriggerExpression(ExpressionState.Happy);
        TriggerShake();
    }

    /// <summary>Llamar en fallo: corta combo y cara triste.</summary>
    public void RegisterWrong()
    {
        _combo      = 0;
        _comboTimer = 0f;
        TriggerExpression(ExpressionState.Sad);
    }

    public void TriggerExpression(ExpressionState state)
    {
        _expression      = state;
        _expressionTimer = expressionDuration;
    }

    public int CurrentCombo => _combo;

    // ─────────────────────────────────────────────────────────────────────
    // Skins
    // ─────────────────────────────────────────────────────────────────────

    public void SetSkin(int index)
    {
        if (skins == null || skins.Length == 0) return;
        _currentSkin = ((index % skins.Length) + skins.Length) % skins.Length;
        var s = skins[_currentSkin];

        jointColor = s.jointColor;
        boneColor  = s.boneColor;
        eyeColor   = s.eyeColor;
        headColor  = s.headColor;

        // Aplica a materiales ya existentes (si Start corrio)
        if (_jointMats != null) ResetColors();
        if (_mouthMat  != null) _mouthMat.color = Glow(eyeColor);
        if (_trailL != null) _trailL.startColor = _trailL.endColor = Glow(boneColor);
        if (_trailR != null) _trailR.startColor = _trailR.endColor = Glow(boneColor);

        // Ropa: camisa y pantalones
        if (_shirtTransform != null)
        {
            _shirtTransform.gameObject.SetActive(s.shirtEnabled);
            if (_shirtMat != null) _shirtMat.color = Glow(s.shirtColor);
        }
        if (_pantsLTransform != null && _pantsRTransform != null)
        {
            _pantsLTransform.gameObject.SetActive(s.pantsEnabled);
            _pantsRTransform.gameObject.SetActive(s.pantsEnabled);
            if (_pantsMat != null) _pantsMat.color = Glow(s.pantsColor);
        }

        // Sombrero: si cambio el tipo, recrear la malla
        if (_currentHatType != s.hat)
        {
            if (_hatTransform != null) Destroy(_hatTransform.gameObject);
            _hatTransform   = null;
            _hatMat         = null;
            _currentHatType = s.hat;
            if (s.hat != HatType.None)
            {
                _hatMat       = MakeUnlitMat(Glow(s.hatColor));
                _hatTransform = CreateHatMesh(s.hat, _hatMat);
            }
        }
        else if (_hatMat != null)
        {
            _hatMat.color = Glow(s.hatColor);
        }
        if (_hatTransform != null)
            _hatTransform.gameObject.SetActive(s.hat != HatType.None);

        if (persistSkin)
        {
            PlayerPrefs.SetInt("stickfigure_skin", _currentSkin);
            PlayerPrefs.Save();
        }
    }

    Transform CreateHatMesh(HatType type, Material mat)
    {
        GameObject go;
        Vector3 localScale;
        switch (type)
        {
            case HatType.Cone:
                // No hay primitivo cono — capsula vertical como gorro de mago
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                localScale = new Vector3(headSize * 0.6f, headSize * 0.7f, headSize * 0.6f);
                break;
            case HatType.Cylinder:
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                localScale = new Vector3(headSize * 0.85f, headSize * 0.4f, headSize * 0.85f);
                break;
            case HatType.Crown:
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                localScale = new Vector3(headSize * 1.0f, headSize * 0.22f, headSize * 1.0f);
                break;
            case HatType.Cap:
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                localScale = new Vector3(headSize * 0.95f, headSize * 0.45f, headSize * 0.95f);
                break;
            default:
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                localScale = Vector3.one * 0.01f;
                break;
        }
        go.name             = $"Hat_{type}";
        go.transform.parent = transform;
        _hatTargetScale     = localScale;
        // Empieza invisible — UpdateClothing lo escala cuando hay pose valida,
        // si no aparece como cilindro gigante en el origen.
        go.transform.localScale = Vector3.zero;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    public void CycleSkin() => SetSkin(_currentSkin + 1);

    void ApplySavedSkin()
    {
        if (skins == null || skins.Length == 0) return;
        int saved = persistSkin ? PlayerPrefs.GetInt("stickfigure_skin", 0) : 0;
        SetSkin(saved);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers internos
    // ─────────────────────────────────────────────────────────────────────

    private TrailRenderer AddTrail(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var tr = go.AddComponent<TrailRenderer>();
        tr.time         = trailTime;
        tr.startWidth   = trailWidth;
        tr.endWidth     = 0f;
        tr.minVertexDistance = 0.02f;
        tr.numCapVertices    = 4;
        tr.numCornerVertices = 4;
        var trailShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        tr.material = new Material(trailShader) { color = Glow(boneColor) };
        tr.startColor = Glow(boneColor);
        tr.endColor   = new Color(Glow(boneColor).r, Glow(boneColor).g, Glow(boneColor).b, 0f);
        return tr;
    }

    void UpdateExpressionAndAura()
    {
        // Expression timer
        if (_expressionTimer > 0f)
        {
            _expressionTimer -= Time.unscaledDeltaTime;
            if (_expressionTimer <= 0f) _expression = ExpressionState.Neutral;
        }

        // Eyes + mouth scale segun expresion
        Vector3 eyeScaleBase   = Vector3.one * eyeSize;
        Vector3 mouthScaleBase = Vector3.one * mouthSize;
        Vector3 eyeScale, mouthScale;

        switch (_expression)
        {
            case ExpressionState.Happy:
                eyeScale   = new Vector3(eyeScaleBase.x, eyeScaleBase.y * 0.35f, eyeScaleBase.z); // squint
                mouthScale = new Vector3(mouthScaleBase.x * 2.2f, mouthScaleBase.y * 0.6f, mouthScaleBase.z);
                break;
            case ExpressionState.Sad:
                eyeScale   = new Vector3(eyeScaleBase.x * 1.2f, eyeScaleBase.y * 1.3f, eyeScaleBase.z);
                mouthScale = new Vector3(mouthScaleBase.x * 1.4f, mouthScaleBase.y * 0.5f, mouthScaleBase.z);
                break;
            default:
                eyeScale   = eyeScaleBase;
                mouthScale = new Vector3(mouthScaleBase.x * 1.3f, mouthScaleBase.y * 0.5f, mouthScaleBase.z);
                break;
        }
        if (_leftEye)  _leftEye.localScale  = eyeScale;
        if (_rightEye) _rightEye.localScale = eyeScale;
        if (_mouthTransform) _mouthTransform.localScale = mouthScale;

        // Boca posicionada bajo los ojos (midpoint X, debajo en Y)
        if (_mouthTransform && _leftEye && _rightEye && _headTransform)
        {
            Vector3 mid = (_leftEye.position + _rightEye.position) * 0.5f;
            mid.y -= headSize * 0.30f;
            _mouthTransform.position = mid;
        }

        // Combo decay
        if (_comboTimer > 0f)
        {
            _comboTimer -= Time.unscaledDeltaTime;
            if (_comboTimer <= 0f) _combo = 0;
        }

        // Aura: escala y color segun combo
        if (_auraTransform && _auraMat)
        {
            float t = Mathf.Clamp01(_combo / 10f);
            float targetScale = Mathf.Lerp(0f, auraMaxRadius, t);
            // Pulso suave cuando hay combo
            if (_combo > 0)
                targetScale *= 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.05f;

            Vector3 cur = _auraTransform.localScale;
            _auraTransform.localScale = Vector3.Lerp(cur, Vector3.one * targetScale, Time.unscaledDeltaTime * 6f);

            // Posicionada en hipline
            if (_jointPositions != null && joints != null && joints[23] != null && joints[24] != null)
            {
                _auraTransform.position = (_jointPositions[23] + _jointPositions[24]) * 0.5f;
            }

            Color target;
            if      (t < 0.33f) target = Color.Lerp(auraColorLow,  auraColorMid,  t / 0.33f);
            else if (t < 0.66f) target = Color.Lerp(auraColorMid,  auraColorHigh, (t - 0.33f) / 0.33f);
            else                target = Color.Lerp(auraColorHigh, auraColorMax,  (t - 0.66f) / 0.34f);
            _auraMat.color = Color.Lerp(_auraMat.color, target, Time.unscaledDeltaTime * 4f);
        }
    }

    void Update_DebugInput()
    {
        if (Input.GetKeyDown(KeyCode.K)) CycleSkin();
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
