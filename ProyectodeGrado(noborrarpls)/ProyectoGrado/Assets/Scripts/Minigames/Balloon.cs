using UnityEngine;
using TMPro;

/// <summary>
/// Componente individual de cada globo. Lo spawnean BalloonPopGameUDP (modo color)
/// o NumberBalloonGameUDP (modo numero).
///
/// SETUP (prefab) — opcion sprite:
///  1. Crear un GameObject vacio "BalloonPrefab".
///  2. Hijo: GameObject "Visual" con SpriteRenderer usando el sprite del globo.
///     El codigo asigna SpriteRenderer.color en runtime (no hace falta material especial).
///  3. Hijo: TextMeshPro (3D, NO UGUI) centrado, font size 4, color blanco.
///     Solo necesario para modo numero. El script lo orienta hacia la camara.
///  4. Collider en el raiz: CircleCollider2D (si escena 2D) o SphereCollider (si 3D).
///
/// SETUP (prefab) — opcion 3D esfera:
///  - Sphere con SphereCollider radio 0.5. Material Standard o Unlit/Color.
///    El codigo crea una instancia del material para asignar el color.
/// </summary>
public class Balloon : MonoBehaviour
{
    public int  ColorIndex  { get; private set; }
    public int  NumberIndex { get; private set; } = -1;
    public bool OffScreen   { get; private set; }

    private float          _speed;
    private float          _despawnY;
    private SpriteRenderer _sprite;
    private Renderer       _rend;
    private TextMeshPro    _label;

    /// <summary>Modo color: asigna color al sprite o al material 3D.</summary>
    public void Init(int colorIdx, Color color, float speed, float despawnY)
    {
        ColorIndex = colorIdx;
        _speed     = speed;
        _despawnY  = despawnY;

        // Prioridad: SpriteRenderer (2D) > Renderer generico (3D mesh).
        _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_sprite != null)
        {
            _sprite.color = color;
        }
        else
        {
            _rend = GetComponentInChildren<Renderer>();
            if (_rend != null)
                _rend.material = new Material(_rend.sharedMaterial) { color = color };
        }
    }

    /// <summary>Modo numero: asigna color + texto del numeral (numberIdx 0-9 -> "1"-"10").</summary>
    public void InitWithNumber(int numberIdx, Color color, float speed, float despawnY)
    {
        Init(numberIdx, color, speed, despawnY);
        NumberIndex = numberIdx;
        _label = GetComponentInChildren<TextMeshPro>();
        if (_label != null) _label.text = (numberIdx + 1).ToString();
    }

    // --- Modo estático: flota en su anclaje sin irse por pantalla ---
    private bool    _static;
    private Vector3 _anchor;
    private float   _bobFreq;
    private float   _bobAmp;
    private float   _bobPhase;

    /// <summary>Globo fijo que sólo se balancea suavemente sobre su posición inicial.</summary>
    public void InitStatic(int numberIdx, Color color, float bobFreq = 1.5f, float bobAmp = 0.18f)
    {
        Init(numberIdx, color, 0f, float.MaxValue);
        NumberIndex = numberIdx;
        _label = GetComponentInChildren<TextMeshPro>();
        if (_label != null) _label.text = (numberIdx + 1).ToString();
        _static   = true;
        _anchor   = transform.position;
        _bobFreq  = bobFreq;
        _bobAmp   = bobAmp;
        _bobPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (_static)
        {
            float y = _anchor.y + Mathf.Sin((Time.time + _bobPhase) * _bobFreq) * _bobAmp;
            transform.position = new Vector3(_anchor.x, y, _anchor.z);
        }
        else
        {
            transform.position += Vector3.up * _speed * Time.deltaTime;
            float sway = Mathf.Sin(Time.time * 2f + transform.position.x) * 0.3f * Time.deltaTime;
            transform.position += Vector3.right * sway;
            if (transform.position.y > _despawnY) OffScreen = true;
        }

        if (_label != null && Camera.main != null)
            _label.transform.rotation = Camera.main.transform.rotation;
    }
}
