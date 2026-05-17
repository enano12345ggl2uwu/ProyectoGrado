using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Auto-construye el panel "MY PROGRESS" en el lado izquierdo de la pantalla.
/// Solo agrega este script a cualquier GameObject en la escena IslandSelector.
/// No requiere setup en el Inspector.
/// </summary>
public class ProgressPanelUI : MonoBehaviour
{
    [Header("Posicion en pantalla")]
    [Tooltip("Mueve el panel. X: negativo=izquierda, positivo=derecha. Y: negativo=abajo, positivo=arriba.")]
    public Vector2 posicion = new Vector2(0f, 0f);
    [Tooltip("Tamaño del panel en pixeles.")]
    public Vector2 tamanio  = new Vector2(250f, 190f);

    private static readonly string[] Keys      = { "color", "balloon", "size", "mirror" };
    private static readonly string[] LevelNames = { "Color Jump", "Balloon Pop", "Size Sort", "Mirror Word" };
    private const int MaxStars = 12;

    private Image            _fillImage;
    private TextMeshProUGUI  _starsLabel;
    private TextMeshProUGUI[] _levelLabels;

    void Start()
    {
        BuildUI();
        Refresh();
    }

    void Update()
    {
        // Debug: Shift + R resetea todas las estrellas y refresca la barra.
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            && Input.GetKeyDown(KeyCode.R))
        {
            ResetAllStars();
        }
    }

    public void ResetAllStars()
    {
        foreach (var k in Keys) PlayerPrefs.DeleteKey($"stars_{k}");
        PlayerPrefs.Save();
        Debug.Log("[ProgressPanelUI] Estrellas reseteadas.");
        Refresh();
    }

    void BuildUI()
    {
        // Buscar SOLO canvases de la escena activa, ignorando los persistentes
        // como el de SceneTransition (DontDestroyOnLoad). Si no filtramos, el
        // panel se parenta al canvas global y viaja entre escenas.
        Canvas canvas = null;
        var myScene = gameObject.scene;
        foreach (var c in FindObjectsOfType<Canvas>())
        {
            if (c.gameObject.scene == myScene) { canvas = c; break; }
        }
        if (canvas == null) { Debug.LogError("[ProgressPanelUI] No hay Canvas en la escena."); return; }

        // ── Panel raiz ─────────────────────────────────────────────────────
        GameObject panel = MakeRect("ProgressPanel", canvas.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            posicion, tamanio);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.07f, 0.07f, 0.18f, 0.90f);

        // ── Titulo ─────────────────────────────────────────────────────────
        MakeText(panel.transform, "Title", "MY PROGRESS",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(230f, 30f),
            17, FontStyles.Bold, new Color(1f, 0.85f, 0.2f));

        // Linea decorativa bajo el titulo
        var line = MakeRect("Line", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(210f, 2f));
        line.AddComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.5f);

        // ── Barra de fondo ─────────────────────────────────────────────────
        var barBg = MakeRect("BarBg", panel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(210f, 26f));
        barBg.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.28f, 1f);

        // ── Barra de relleno (fill) ─────────────────────────────────────────
        var fillGO = MakeRect("BarFill", barBg.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(-4f, -4f));
        _fillImage            = fillGO.AddComponent<Image>();
        _fillImage.type       = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillAmount = 0f;
        _fillImage.color      = new Color(0.3f, 0.85f, 0.4f, 1f);

        // ── Etiqueta de estrellas ───────────────────────────────────────────
        _starsLabel = MakeText(panel.transform, "StarsLabel", "0 / 12 ★",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(220f, 26f),
            15, FontStyles.Normal, new Color(1f, 1f, 1f, 0.9f));

        // ── Mini etiquetas de niveles ───────────────────────────────────────
        _levelLabels = new TextMeshProUGUI[4];
        float spacing = 52f;
        float startX  = -(spacing * 1.5f);
        for (int i = 0; i < 4; i++)
        {
            _levelLabels[i] = MakeText(panel.transform, $"Level{i}", LevelNames[i],
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(startX + i * spacing, 30f), new Vector2(spacing - 4f, 34f),
                9, FontStyles.Normal, new Color(0.75f, 0.75f, 1f, 0.85f));
            _levelLabels[i].enableWordWrapping = true;
            _levelLabels[i].overflowMode = TextOverflowModes.Truncate;
        }
    }

    public void Refresh()
    {
        if (_fillImage == null || _starsLabel == null) return;

        int earned = 0;
        for (int i = 0; i < Keys.Length; i++)
        {
            int s = Mathf.Clamp(PlayerPrefs.GetInt($"stars_{Keys[i]}", 0), 0, 3);
            earned += s;

            // Colorea la etiqueta del nivel segun sus estrellas
            if (_levelLabels != null && i < _levelLabels.Length && _levelLabels[i] != null)
            {
                _levelLabels[i].color = s == 3 ? new Color(1f, 0.85f, 0.2f)
                                      : s > 0  ? new Color(0.75f, 0.75f, 1f, 0.85f)
                                      :          new Color(0.5f, 0.5f, 0.6f, 0.6f);
            }
        }

        float target = (float)earned / MaxStars;
        Color barColor = target < 0.5f
            ? Color.Lerp(new Color(0.9f, 0.3f, 0.3f), new Color(1f, 0.8f, 0.2f), target * 2f)
            : Color.Lerp(new Color(1f, 0.8f, 0.2f),   new Color(0.3f, 0.85f, 0.4f), (target - 0.5f) * 2f);
        _fillImage.color = barColor;

        _starsLabel.text = $"{earned} / {MaxStars} ★";

        StopAllCoroutines();
        StartCoroutine(AnimateFill(target));
    }

    IEnumerator AnimateFill(float target)
    {
        float from = _fillImage.fillAmount;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.8f;
            _fillImage.fillAmount = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        _fillImage.fillAmount = target;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static GameObject MakeRect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.pivot        = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta    = sizeDelta;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        float fontSize, FontStyles style, Color color)
    {
        var go = MakeRect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        var tmp         = go.AddComponent<TextMeshProUGUI>();
        tmp.text        = text;
        tmp.fontSize    = fontSize;
        tmp.fontStyle   = style;
        tmp.color       = color;
        tmp.alignment   = TextAlignmentOptions.Center;
        return tmp;
    }
}
