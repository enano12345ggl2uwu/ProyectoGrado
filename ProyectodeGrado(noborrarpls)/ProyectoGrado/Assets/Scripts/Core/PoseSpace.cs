using UnityEngine;

/// <summary>
/// Singleton que centraliza la transformación de coordenadas MediaPipe → mundo Unity.
/// Todos los minijuegos usan esto en lugar de sus propios transforms dispersos.
///
/// Uso:
///   Vector3 worldPos = PoseSpace.LandmarkToWorld(rawLandmark);
///
/// Para override por minijuego:
///   PoseSpaceConfig.Instance.WithOverride(scale: 5.5f, offset: new Vector3(0, 2.5f, 0));
/// </summary>
public class PoseSpace
{
    public static Vector3 LandmarkToWorld(Vector3 rawLandmark)
    {
        PoseSpaceConfig config = PoseSpaceConfig.Instance;
        if (config == null) return rawLandmark;

        Vector3 scaled = new Vector3(
            rawLandmark.x * config.Scale,
            rawLandmark.y * config.Scale,
            rawLandmark.z * config.Scale
        );

        return scaled + config.Offset;
    }
}

/// <summary>
/// Configuración de transform. Singleton.
/// Por defecto: scale=5f, offset=(0, 2, 0) — valores actuales del proyecto.
/// </summary>
public class PoseSpaceConfig : MonoBehaviour
{
    public static PoseSpaceConfig Instance { get; private set; }

    [Header("Transform")]
    public float scale = 5f;
    public Vector3 offset = new Vector3(0f, 2f, 0f);

    // Override temporal (para un minijuego específico)
    private float overrideScale = -1f;
    private Vector3 overrideOffset = Vector3.zero;
    private bool hasOverride = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float Scale => hasOverride ? overrideScale : scale;
    public Vector3 Offset => hasOverride ? overrideOffset : offset;

    public void WithOverride(float overrideScale, Vector3 overrideOffset)
    {
        this.overrideScale = overrideScale;
        this.overrideOffset = overrideOffset;
        hasOverride = true;
    }

    public void ClearOverride()
    {
        hasOverride = false;
    }
}
