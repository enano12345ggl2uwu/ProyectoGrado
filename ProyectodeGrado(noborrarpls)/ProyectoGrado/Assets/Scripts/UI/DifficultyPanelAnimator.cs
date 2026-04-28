using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anima la entrada del DifficultyPanel cuando se activa (SetActive(true)).
/// Soporta 3 estilos de entrada (FadeScale / SlideDown / BounceIn) y una
/// cascada opcional para los botones internos.
///
/// SETUP en Unity:
///  1. Selecciona el GameObject "DifficultyPanel"
///  2. Add Component > DifficultyPanelAnimator
///  3. Asigna en Inspector:
///       entryStyle      -> BounceIn (recomendado para juego infantil)
///       cascadeButtons  -> true
///       buttons         -> arrastra Easy, Medium, Hard, Start (en ese orden)
///  4. NO necesitas tocar nada mas — al activarse el panel dispara la animacion.
///
/// IMPORTANTE: si tu DifficultyPanel arranca activo en la escena (SetActive(true)
/// en el Inspector), la animacion corre en el primer frame. Si quieres que arranque
/// oculto, desactivalo en Inspector y haz que otro script lo active cuando toque.
/// </summary>
[DisallowMultipleComponent]
public class DifficultyPanelAnimator : MonoBehaviour
{
    public enum EntryStyle { None, FadeScale, SlideDown, BounceIn }

    [Header("Estilo de entrada del panel")]
    public EntryStyle entryStyle = EntryStyle.BounceIn;

    [Tooltip("Duracion total de la animacion del panel.")]
    [Range(0.1f, 1.5f)] public float duration = 0.5f;

    [Header("SlideDown")]
    [Tooltip("Pixeles que cae desde arriba (solo SlideDown).")]
    public float slideDistance = 200f;

    [Header("Cascada de botones")]
    [Tooltip("Si esta activo, los botones aparecen uno tras otro despues del panel.")]
    public bool cascadeButtons = true;
    [Tooltip("Botones a animar en cascada, en el orden deseado (Easy, Medium, Hard, Start).")]
    public List<RectTransform> buttons = new List<RectTransform>();
    [Range(0.02f, 0.3f)] public float cascadeStagger = 0.08f;
    [Range(0.1f, 0.6f)]  public float cascadeDuration = 0.3f;

    private CanvasGroup _cg;
    private Vector3     _panelOriginalScale;
    private Vector2     _panelOriginalAnchoredPos;
    private RectTransform _rt;

    // Cache por boton: escala original
    private readonly Dictionary<RectTransform, Vector3> _btnOriginalScale = new Dictionary<RectTransform, Vector3>();

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        _panelOriginalScale       = transform.localScale;
        _panelOriginalAnchoredPos = _rt != null ? _rt.anchoredPosition : Vector2.zero;

        // Cacheamos escala original de cada boton ANTES de cualquier animacion
        foreach (var b in buttons)
            if (b != null && !_btnOriginalScale.ContainsKey(b))
                _btnOriginalScale[b] = b.localScale;
    }

    void OnEnable()
    {
        // Al activarse el panel, disparamos la animacion seleccionada.
        StopAllCoroutines();
        StartCoroutine(PlayEntry());
    }

    IEnumerator PlayEntry()
    {
        // Si hay cascada, arrancamos los botones invisibles para que no parpadeen
        if (cascadeButtons)
        {
            foreach (var b in buttons)
            {
                if (b == null) continue;
                b.localScale = Vector3.zero;
            }
        }

        // Fase 1: animacion del panel
        switch (entryStyle)
        {
            case EntryStyle.None:
                _cg.alpha = 1f;
                transform.localScale = _panelOriginalScale;
                break;

            case EntryStyle.FadeScale:
                yield return AnimFadeScale();
                break;

            case EntryStyle.SlideDown:
                yield return AnimSlideDown();
                break;

            case EntryStyle.BounceIn:
                yield return AnimBounceIn();
                break;
        }

        // Fase 2: cascada de botones (si esta activa)
        if (cascadeButtons)
            yield return AnimCascade();
    }

    // -------------------------------------------------------------------------
    // Estilos de entrada del panel
    // -------------------------------------------------------------------------

    IEnumerator AnimFadeScale()
    {
        _cg.alpha = 0f;
        Vector3 from = _panelOriginalScale * 0.85f;
        Vector3 to   = _panelOriginalScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);
            _cg.alpha = eased;
            yield return null;
        }
        transform.localScale = to;
        _cg.alpha = 1f;
    }

    IEnumerator AnimSlideDown()
    {
        if (_rt == null) yield break;

        _cg.alpha = 0f;
        Vector2 from = _panelOriginalAnchoredPos + new Vector2(0f, slideDistance);
        Vector2 to   = _panelOriginalAnchoredPos;
        _rt.anchoredPosition = from;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
            _rt.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            _cg.alpha = eased;
            yield return null;
        }
        _rt.anchoredPosition = to;
        _cg.alpha = 1f;
    }

    /// <summary>
    /// Bounce: 0 -> 1.10 -> 0.95 -> 1.00. Curva tipo "back-out" hecha a mano
    /// para no depender de AnimationCurve serializado.
    /// </summary>
    IEnumerator AnimBounceIn()
    {
        _cg.alpha = 1f;
        Vector3 baseScale = _panelOriginalScale;

        // Tramo 1: 0 -> 1.10  (60% del tiempo, ease-out)
        float seg1 = duration * 0.6f;
        float t = 0f;
        while (t < seg1)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seg1);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, baseScale * 1.10f, eased);
            yield return null;
        }

        // Tramo 2: 1.10 -> 0.95  (20% del tiempo, lineal)
        float seg2 = duration * 0.2f;
        t = 0f;
        while (t < seg2)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seg2);
            transform.localScale = Vector3.LerpUnclamped(baseScale * 1.10f, baseScale * 0.95f, k);
            yield return null;
        }

        // Tramo 3: 0.95 -> 1.00  (20% del tiempo, ease-out)
        float seg3 = duration * 0.2f;
        t = 0f;
        while (t < seg3)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seg3);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            transform.localScale = Vector3.LerpUnclamped(baseScale * 0.95f, baseScale, eased);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    // -------------------------------------------------------------------------
    // Cascada de botones
    // -------------------------------------------------------------------------

    IEnumerator AnimCascade()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var b = buttons[i];
            if (b == null) continue;
            StartCoroutine(AnimButtonPop(b, i * cascadeStagger));
        }
        // Esperamos a que termine el ultimo (para que el panel quede "tranquilo" despues)
        yield return new WaitForSeconds(buttons.Count * cascadeStagger + cascadeDuration);
    }

    IEnumerator AnimButtonPop(RectTransform btn, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 target = _btnOriginalScale.ContainsKey(btn) ? _btnOriginalScale[btn] : Vector3.one;
        Vector3 from   = Vector3.zero;
        Vector3 over   = target * 1.10f;

        // 0 -> 1.10 (70% ease-out)
        float seg1 = cascadeDuration * 0.7f;
        float t = 0f;
        while (t < seg1)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seg1);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            btn.localScale = Vector3.LerpUnclamped(from, over, eased);
            yield return null;
        }

        // 1.10 -> 1.00 (30% lineal)
        float seg2 = cascadeDuration * 0.3f;
        t = 0f;
        while (t < seg2)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seg2);
            btn.localScale = Vector3.LerpUnclamped(over, target, k);
            yield return null;
        }

        btn.localScale = target;
    }
}
