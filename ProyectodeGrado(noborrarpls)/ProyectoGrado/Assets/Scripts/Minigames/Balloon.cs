using UnityEngine;

/// <summary>
/// Componente individual de cada globo. Lo spawnea BalloonPopGameUDP.
/// Se mueve hacia arriba a una velocidad fija; se autodestruye al salir de pantalla.
///
/// SETUP (prefab):
///  - GameObject con Mesh Renderer (Sphere) o Sprite circular.
///  - Material con color aplicado por codigo (Init asigna el color).
///  - SphereCollider radio ~0.5 (no trigger).
///  - Este script se agrega automaticamente en runtime si no existe.
/// </summary>
public class Balloon : MonoBehaviour
{
    public int   ColorIndex { get; private set; }
    public bool  OffScreen  { get; private set; }

    private float _speed;
    private float _despawnY;
    private Renderer _rend;

    public void Init(int colorIdx, Color color, float speed, float despawnY)
    {
        ColorIndex = colorIdx;
        _speed     = speed;
        _despawnY  = despawnY;
        _rend      = GetComponentInChildren<Renderer>();
        if (_rend != null)
        {
            // Material per-instance para no afectar al prefab
            _rend.material = new Material(_rend.sharedMaterial) { color = color };
        }
    }

    void Update()
    {
        transform.position += Vector3.up * _speed * Time.deltaTime;
        // leve vaiven horizontal para que no sea robotico
        float sway = Mathf.Sin(Time.time * 2f + transform.position.x) * 0.3f * Time.deltaTime;
        transform.position += Vector3.right * sway;

        if (transform.position.y > _despawnY) OffScreen = true;
    }
}
