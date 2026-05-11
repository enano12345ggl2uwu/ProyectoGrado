using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barra de progreso global basada en estrellas acumuladas en todos los minijuegos.
/// Total: 12 estrellas (3 por nivel x 4 niveles).
///
/// SETUP en Unity:
///  1. Crea un Panel en el Canvas del IslandSelector (o donde quieras).
///  2. Dentro pon una Image con Image Type = Filled, Fill Method = Horizontal.
///     Asignala al campo "fillImage".
///  3. Opcional: un TextMeshProUGUI para mostrar "8 / 12 estrellas".
///  4. Pega este script en cualquier GameObject de la escena y arrastra las refs.
///  5. Llama Refresh() al entrar a la escena (o se llama solo en Start).
/// </summary>
public class GlobalProgressBar : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Image con Type=Filled. Su fillAmount va de 0 a 1.")]
    public Image fillImage;
    [Tooltip("Texto opcional: '8 / 12'. Deja vacio para no mostrarlo.")]
    public TextMeshProUGUI starsLabel;

    [Header("Config")]
    [Tooltip("Claves de minigame en el mismo orden que ResultsScreen.minigameKey.")]
    public string[] minigameKeys = { "color", "balloon", "size", "mirror" };
    [Tooltip("Estrellas maximas por nivel (normalmente 3).")]
    public int starsPerLevel = 3;

    [Header("Colores de la barra (opcional)")]
    public bool  useGradient    = true;
    public Color colorLow       = new Color(0.9f, 0.3f, 0.3f);   // rojo
    public Color colorMid       = new Color(1.0f, 0.8f, 0.2f);   // amarillo
    public Color colorHigh      = new Color(0.3f, 0.85f, 0.4f);  // verde

    [Header("Animacion")]
    [Tooltip("Si true, la barra se llena suavemente al entrar a la escena.")]
    public bool  animateOnStart = true;
    [Range(0.2f, 3f)] public float animDuration = 0.8f;

    private float _targetFill = 0f;
    private float _currentFill = 0f;
    private bool  _animating  = false;

    void Start() => Refresh();

    /// <summary>Recalcula el progreso y actualiza la barra. Llamar al volver al menú.</summary>
    public void Refresh()
    {
        int total    = minigameKeys.Length * starsPerLevel;
        int earned   = 0;
        foreach (var key in minigameKeys)
            earned += Mathf.Clamp(PlayerPrefs.GetInt($"stars_{key}", 0), 0, starsPerLevel);

        _targetFill = total > 0 ? (float)earned / total : 0f;

        if (starsLabel)
            starsLabel.text = $"{earned} / {total}";

        if (animateOnStart)
        {
            _currentFill = 0f;
            _animating   = true;
        }
        else
        {
            ApplyFill(_targetFill);
        }
    }

    void Update()
    {
        if (!_animating) return;
        _currentFill = Mathf.MoveTowards(_currentFill, _targetFill, Time.deltaTime / animDuration);
        ApplyFill(_currentFill);
        if (Mathf.Approximately(_currentFill, _targetFill)) _animating = false;
    }

    void ApplyFill(float t)
    {
        if (fillImage) fillImage.fillAmount = t;

        if (useGradient && fillImage)
        {
            Color c = t < 0.5f
                ? Color.Lerp(colorLow, colorMid,  t * 2f)
                : Color.Lerp(colorMid, colorHigh, (t - 0.5f) * 2f);
            fillImage.color = c;
        }
    }

    /// <summary>Devuelve las estrellas guardadas de un minijuego concreto.</summary>
    public static int GetStars(string key) => PlayerPrefs.GetInt($"stars_{key}", 0);

    /// <summary>Resetea todas las estrellas (para testing).</summary>
    public void ResetProgress()
    {
        foreach (var key in minigameKeys)
            PlayerPrefs.DeleteKey($"stars_{key}");
        PlayerPrefs.Save();
        Refresh();
    }
}
