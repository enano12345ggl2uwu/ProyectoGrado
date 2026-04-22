using System.Collections;
using UnityEngine;

/// <summary>
/// Utilidad global de screen shake. Llama a ScreenShake.Instance.Shake() desde cualquier sitio.
/// Setup: crea un GameObject vacio "ScreenShake" en MainMenu (o en cada escena) con este script.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private Vector3   _originalLocalPos;
    private Transform _target;
    private Coroutine _current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Shake(float duration = 0.25f, float magnitude = 0.15f)
    {
        if (Camera.main == null) return;
        _target = Camera.main.transform;
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        _originalLocalPos = _target.localPosition;
        float t = 0f;
        while (t < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            _target.localPosition = _originalLocalPos + new Vector3(x, y, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        _target.localPosition = _originalLocalPos;
        _current = null;
    }
}
