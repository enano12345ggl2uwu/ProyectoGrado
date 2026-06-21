using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay de tutorial v2. Se autogestiona en Start: si el jugador nunca lo vio
/// en esta instalación (PlayerPrefs por minigameKey), aparece, pausa el juego y
/// espera click en "Listo" (mouse o dwell de PoseCursor). Una vez confirmado se
/// marca como visto y no vuelve a aparecer hasta que se llame ResetAll().
///
/// Setup en escena (por minijuego):
///   1. Crea un GameObject "TutorialOverlay" como ÚLTIMO hijo del Canvas
///      (debe quedar encima de todo, incluso del PoseCursor).
///   2. Estructura sugerida:
///        TutorialOverlay  (este script + CanvasGroup + Image fondo semi-transparente)
///          └─ Card  (Image — la tarjeta blanca)
///               ├─ TitleText      (TMP grande)
///               ├─ BodyText       (TMP mediano)
///               ├─ PlaceholderImg (Image — donde irá el video, hoy un sprite)
///               └─ DoneButton     (Button con texto "Listo")
///   3. En el Inspector arrastra: canvasGroup, titleText, bodyText, placeholderImage, doneButton.
///   4. Llena: title, body, placeholderSprite, minigameKey ("color"|"balloon"|"size"|"mirror").
///   5. El DoneButton funciona con click de mouse Y con dwell del PoseCursor automáticamente
///      (PoseCursor invoca Button.onClick.Invoke()).
///
/// NO requiere editar el manager del minijuego.
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    [Header("Contenido")]
    [TextArea(1, 2)] public string title = "Tutorial";
    [TextArea(3, 6)] public string body  = "Descripción del minijuego.";
    public Sprite placeholderSprite;

    [Header("Identidad")]
    [Tooltip("Clave única por minijuego: color | balloon | size | mirror.")]
    public string minigameKey = "color";

    [Header("Comportamiento")]
    [Tooltip("Si está ON, se evalúa en Start si debe mostrarse (según PlayerPrefs).")]
    public bool showOnStart = true;
    [Tooltip("Pausa el juego (Time.timeScale = 0) mientras el tutorial está visible.")]
    public bool pauseGame = true;
    [Tooltip("Permite cerrar con click de mouse además del botón Listo.")]
    public bool clickToCloseEnabled = true;
    [Tooltip("Duración del fade in/out en segundos (usa unscaled time).")]
    public float fadeDuration = 0.3f;

    [Header("Refs UI")]
    public CanvasGroup     canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Image           placeholderImage;
    public Button          doneButton;

    private static readonly System.Collections.Generic.HashSet<string> _seenThisSession
        = new System.Collections.Generic.HashSet<string>();

    private float _prevTimeScale = 1f;
    private bool  _isVisible;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        EnsureRaycastBlocker();

        // Conectar el boton aqui de forma permanente.
        if (doneButton)
        {
            doneButton.onClick.RemoveListener(OnDonePressed);
            doneButton.onClick.AddListener(OnDonePressed);
        }
    }

    // CanvasGroup.blocksRaycasts solo bloquea raycasts que ya golpearon un grafico
    // dentro de este subtree. Sin un Image fullscreen con raycastTarget, los clicks
    // pasan al DifficultyPanel detras. Y si el RectTransform no esta a fullstretch,
    // en aspect ratios anchos (4K, Free Aspect) la escena se ve a los lados.
    // Forzamos ambas cosas aqui.
    void EnsureRaycastBlocker()
    {
        StretchToParent(GetComponent<RectTransform>());

        var ownImage = GetComponent<Image>();
        if (ownImage != null)
        {
            ownImage.raycastTarget = true;
            return;
        }

        var existing = transform.Find("__RaycastBlocker");
        if (existing != null) return;

        var go = new GameObject("__RaycastBlocker", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.SetAsFirstSibling();
        StretchToParent(rt);

        var img = go.GetComponent<Image>();
        img.color         = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
    }

    static void StretchToParent(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale       = Vector3.one;
    }

    void Start()
    {
        if (!showOnStart) return;
        if (_seenThisSession.Contains(minigameKey)) return;
        Show();
    }

    public void Show()
    {
        if (_isVisible) return;
        _isVisible = true;

        // Overlay encima de DifficultyPanel; PoseCursor encima del overlay
        // (asi el cursor sigue visible para hacer dwell sobre el boton Listo).
        transform.SetAsLastSibling();
        var cursor = FindObjectOfType<PoseCursor>();
        if (cursor != null && cursor.transform.parent == transform.parent)
            cursor.transform.SetAsLastSibling();

        if (titleText)        titleText.text   = title;
        if (bodyText)         bodyText.text    = body;
        if (placeholderImage) placeholderImage.sprite = placeholderSprite;

        if (pauseGame)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, true));
    }

    void Update()
    {
        if (!_isVisible) return;
        if (clickToCloseEnabled && Input.GetMouseButtonDown(0))
        {
            // Si el click no cayó sobre el botón Listo, igual cerramos —
            // facilita testing. Si quieres forzar click solo en el botón,
            // pon clickToCloseEnabled = false.
            OnDonePressed();
        }
    }

    public void OnDonePressed()
    {
        Debug.Log($"[TutorialOverlay] OnDonePressed (visible={_isVisible}, key={minigameKey})");
        if (!_isVisible) return;

        _seenThisSession.Add(minigameKey);

        StopAllCoroutines();
        StartCoroutine(FadeOutAndClose());
    }

    IEnumerator FadeOutAndClose()
    {
        yield return FadeTo(0f, false);

        if (pauseGame) Time.timeScale = _prevTimeScale;
        _isVisible = false;
    }

    IEnumerator FadeTo(float target, bool interactableWhenDone)
    {
        if (canvasGroup == null) yield break;

        float start    = canvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float t        = 0f;

        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = true;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha          = target;
        canvasGroup.interactable   = interactableWhenDone;
        canvasGroup.blocksRaycasts = interactableWhenDone;
    }

    // -------------------------------------------------------------------------
    // Debug helpers
    // -------------------------------------------------------------------------

    /// <summary>Resetea todos los flags de esta sesión (el tutorial vuelve a aparecer).</summary>
    public static void ResetAll()
    {
        _seenThisSession.Clear();
        Debug.Log("[TutorialOverlay] Reset all tutorial flags.");
    }

    /// <summary>Resetea el flag de una clave específica.</summary>
    public static void ResetKey(string key)
    {
        _seenThisSession.Remove(key);
        Debug.Log($"[TutorialOverlay] Reset tutorial flag for '{key}'.");
    }
}
