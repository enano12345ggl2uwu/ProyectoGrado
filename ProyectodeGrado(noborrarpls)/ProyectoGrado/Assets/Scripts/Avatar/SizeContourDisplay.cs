using UnityEngine;

/// <summary>
/// Dibuja dos rectángulos: uno fijo (target) y uno vivo (player).
/// Cuando coinciden en width Y height, ambos se vuelven verdes.
///
/// Setup:
///   1. GameObject vacío "SizeContour" — posición = centro visual deseado (ej. 0, 1.5, 0).
///   2. Pega este script. No requiere referencias — genera los LineRenderer en runtime.
///   3. En SizeSortGameUDP arrastra este componente al campo "contour".
/// </summary>
public class SizeContourDisplay : MonoBehaviour
{
    [Header("Visual")]
    public float lineWidth = 0.15f;

    [Header("Colores")]
    public Color targetColor = new Color(1f,  1f,  1f,  1f);
    public Color liveColor   = new Color(0.4f, 0.9f, 1f, 1f);
    public Color matchColor  = new Color(0.2f, 1f,  0.3f, 1f);

    private LineRenderer _target;
    private LineRenderer _live;
    private Material     _targetMat;
    private Material     _liveMat;

    void Start()
    {
        Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        _target = MakeRect("TargetContour", unlit, targetColor);
        _live   = MakeRect("LiveContour",   unlit, liveColor);
        _targetMat = _target.material;
        _liveMat   = _live.material;

        // Oculto hasta que llegue la primera pose real
        _live.enabled = false;
    }

    LineRenderer MakeRect(string name, Shader shader, Color c)
    {
        var go = new GameObject(name);
        go.transform.parent        = transform;
        go.transform.localPosition = Vector3.zero;

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;   // posiciones en world space — sin doble offset
        lr.loop          = true;
        lr.startWidth    = lr.endWidth = lineWidth;
        lr.positionCount = 4;
        lr.sortingOrder  = 100;    // siempre encima de props 3D
        lr.material      = new Material(shader) { color = c };
        lr.startColor    = lr.endColor = c;
        return lr;
    }

    public void SetTargetSize(float width, float height)
    {
        DrawRect(_target, width, height);
    }

    public void SetLiveSize(float width, float height, bool matching)
    {
        if (!_live.enabled) _live.enabled = true;
        DrawRect(_live, width, height);

        Color liveNow   = matching ? matchColor : liveColor;
        Color targetNow = matching ? matchColor : targetColor;

        if (_liveMat)
        {
            _liveMat.color       = liveNow;
            _live.startColor     = _live.endColor = liveNow;
        }
        if (_targetMat)
        {
            _targetMat.color     = targetNow;
            _target.startColor   = _target.endColor = targetNow;
        }
    }

    void DrawRect(LineRenderer lr, float width, float height)
    {
        Vector3 c = transform.position;
        float hw = Mathf.Max(0.05f, width)  * 0.5f;
        float hh = Mathf.Max(0.05f, height) * 0.5f;
        lr.SetPosition(0, c + new Vector3(-hw, -hh, 0f));
        lr.SetPosition(1, c + new Vector3( hw, -hh, 0f));
        lr.SetPosition(2, c + new Vector3( hw,  hh, 0f));
        lr.SetPosition(3, c + new Vector3(-hw,  hh, 0f));
    }
}
