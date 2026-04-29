using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Estilo visual para botones UGUI:
///   - Sprite de rectangulo redondeado generado en runtime
///   - Brillo + escala al pasar el mouse (hover glow)
///   - Pulse de escala al hacer click
///   - Pulse continuo cuando esta seleccionado (SetSelected)
///   - SetBaseColor() para que DifficultySelector pueda cambiar el color sin conflictos
///
/// SETUP: Add Component → UIButtonStyle en cualquier GameObject con Button + Image.
///
/// IMPORTANTE: La inicializacion se hace en Awake() para que _originalScale y _img
/// esten listos ANTES de que cualquier otro Start() llame a SetSelected().
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public class UIButtonStyle : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Sprite")]
    public int texWidth     = 128;
    public int texHeight    = 64;
    public int cornerRadius = 24;

    [Header("Color base del boton")]
    public Color baseColor = new Color(1f, 0.72f, 0.10f, 1f); // naranja-amarillo

    [Header("Hover / Click")]
    [Range(0f, 0.5f)]    public float brightnessBoost = 0.28f;
    [Range(1f, 1.3f)]    public float hoverScale      = 1.08f;
    [Range(0.05f, 0.3f)] public float animSpeed       = 0.10f;

    private Image    _img;
    private Color    _currentBase;
    private Vector3  _originalScale;
    private bool     _hovered;
    private bool     _selected;

    private Outline  _outline; // borde blanco visible cuando esta seleccionado
    private Shadow   _shadow;  // sombra sutil siempre visible

    private Coroutine _selectPulseCoroutine;
    private Coroutine _animScaleCoroutine;

    // -------------------------------------------------------------------------
    // Awake se ejecuta ANTES que cualquier Start() en la escena.
    // Aqui capturamos _originalScale e _img para que esten listos si otro
    // script llama a SetSelected() desde su propio Start().
    // -------------------------------------------------------------------------
    void Awake()
    {
        _img           = GetComponent<Image>();
        _originalScale = transform.localScale;
        _currentBase   = baseColor;

        // Sombra suave siempre presente (mejora la profundidad visual)
        _shadow = GetComponent<Shadow>();
        if (_shadow == null) _shadow = gameObject.AddComponent<Shadow>();
        _shadow.effectColor    = new Color(0f, 0f, 0f, 0.30f);
        _shadow.effectDistance = new Vector2(2f, -3f);

        // Borde blanco solo visible cuando el boton esta seleccionado
        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();
        _outline.effectColor    = new Color(1f, 1f, 1f, 0.70f);
        _outline.effectDistance = new Vector2(2.5f, -2.5f);
        _outline.enabled        = false;
    }

    void Start()
    {
        // Generamos el sprite y configuramos la imagen.
        // Esto va en Start (no Awake) porque RoundedRectGenerator crea una Texture2D
        // y es mas seguro hacerlo despues de que Unity termino de inicializar la camara.
        _img.sprite = RoundedRectGenerator.Generate(
            texWidth     > 0 ? texWidth     : 128,
            texHeight    > 0 ? texHeight    : 64,
            cornerRadius > 0 ? cornerRadius : 24);
        _img.type  = Image.Type.Sliced;
        _img.color = _currentBase;

        GetComponent<Button>().transition = Selectable.Transition.None;
    }

    // -------------------------------------------------------------------------
    // API publica
    // -------------------------------------------------------------------------

    /// <summary>
    /// Marca este boton como seleccionado (pulso continuo) o deseleccionado.
    /// Seguro de llamar desde el Start() de otro script gracias a Awake().
    /// </summary>
    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (_outline) _outline.enabled = selected; // borde blanco visible solo cuando esta seleccionado

        // Detenemos cualquier pulso de seleccion previo
        if (_selectPulseCoroutine != null)
        {
            StopCoroutine(_selectPulseCoroutine);
            _selectPulseCoroutine = null;
        }

        if (selected)
        {
            // Solo arrancamos coroutine si el GameObject esta activo
            if (isActiveAndEnabled)
                _selectPulseCoroutine = StartCoroutine(SelectPulse());
        }
        else
        {
            // Volvemos a escala original solo si no hay animacion de escala activa
            if (_animScaleCoroutine == null && !_hovered)
                transform.localScale = _originalScale;
        }
    }

    /// <summary>
    /// Cambia el color base del boton (por ejemplo, para indicar seleccion de dificultad).
    /// Seguro de llamar en cualquier momento; usa _img que se inicializa en Awake().
    /// </summary>
    public void SetBaseColor(Color c)
    {
        _currentBase = c;
        // _img puede ser null si se llama antes de Awake (muy poco probable, pero lo protegemos)
        if (_img == null) _img = GetComponent<Image>();
        _img.color = _hovered ? Brighten(c, brightnessBoost) : c;
    }

    // -------------------------------------------------------------------------
    // Eventos de puntero
    // -------------------------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _hovered = true;

        // Detenemos animacion de escala previa (hover saliente o click), pero
        // NO detenemos SelectPulse para que siga marcado como seleccionado.
        PararAnimScale();

        _animScaleCoroutine = StartCoroutine(AnimScale(_originalScale * hoverScale, animSpeed));
        if (_img) _img.color = Brighten(_currentBase, brightnessBoost);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _hovered = false;

        // Detenemos animacion de escala de hover/click
        PararAnimScale();

        _animScaleCoroutine = StartCoroutine(AnimScale(_originalScale, animSpeed));
        if (_img) _img.color = _currentBase;

        // Si el boton esta seleccionado y el pulso fue interrumpido por el hover,
        // lo reiniciamos ahora que el mouse salio.
        if (_selected && _selectPulseCoroutine == null)
            _selectPulseCoroutine = StartCoroutine(SelectPulse());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        // Detenemos animacion de escala activa (no el pulso de seleccion)
        PararAnimScale();
        _animScaleCoroutine = StartCoroutine(ClickPulse());
    }

    void OnDisable()
    {
        // Al desactivarse, todas las coroutines se detienen automaticamente.
        // Limpiamos las referencias para que no queden colgando.
        _animScaleCoroutine   = null;
        _selectPulseCoroutine = null;
        _hovered              = false;
    }

    // -------------------------------------------------------------------------
    // Corutinas privadas
    // -------------------------------------------------------------------------

    IEnumerator ClickPulse()
    {
        float targetScale = _hovered ? hoverScale : 1f;
        yield return AnimScale(_originalScale * 0.90f, 0.06f);
        yield return AnimScale(_originalScale * targetScale, 0.10f);
        _animScaleCoroutine = null;
        // Si esta seleccionado y el pulso no esta corriendo, lo reanudamos
        if (_selected && _selectPulseCoroutine == null)
            _selectPulseCoroutine = StartCoroutine(SelectPulse());
    }

    IEnumerator AnimScale(Vector3 target, float duration)
    {
        Vector3 start   = transform.localScale;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Easing ease-out cubico — arranca rapido, frena al final (mas natural)
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }
        transform.localScale = target;
    }

    /// <summary>
    /// Pulso suave e infinito que indica que el boton esta seleccionado.
    /// Solo modifica escala cuando el mouse NO esta encima (para no pelear con hover).
    /// </summary>
    IEnumerator SelectPulse()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 3.5f;
            float s = 1f + Mathf.Sin(t) * 0.05f;
            // Solo aplicamos escala si no hay hover (hover maneja su propia escala)
            if (!_hovered)
                transform.localScale = _originalScale * s;
            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    /// <summary>Detiene la animacion de escala activa (hover/click) sin tocar SelectPulse.</summary>
    private void PararAnimScale()
    {
        if (_animScaleCoroutine != null)
        {
            StopCoroutine(_animScaleCoroutine);
            _animScaleCoroutine = null;
        }
    }

    static Color Brighten(Color c, float amount) =>
        new Color(Mathf.Clamp01(c.r + amount),
                  Mathf.Clamp01(c.g + amount),
                  Mathf.Clamp01(c.b + amount),
                  c.a);
}
