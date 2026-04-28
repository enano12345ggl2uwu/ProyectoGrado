using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de progreso visual para el gesto HOLD IT!
/// Muestra un gradiente de color mientras el jugador mantiene la pose,
/// y pulsa cuando llega al 100%.
///
/// SETUP en Unity:
///   1. Crea un Panel "HoldBarRoot" en el Canvas.
///   2. Dentro: Image "Background" (color gris oscuro semitransparente, tipo Filled no).
///   3. Dentro de Background: Image "Fill" (tipo Filled, Method = Horizontal, Fill Origin = Left).
///   4. Agrega HoldFillBar.cs al HoldBarRoot y arrastra Fill en fillImage, Background en backgroundImage.
///   5. En el script del minijuego, reemplaza "public Image holdFillBar" por "public HoldFillBar holdBar".
/// </summary>
public class HoldFillBar : MonoBehaviour
{
    [Header("Imagenes")]
    public Image fillImage;
    public Image backgroundImage;

    private Coroutine _pulse;

    // Los colores siempre vienen de UITheme para consistencia
    private Color ColorStart => UITheme.Warning; // amarillo al comenzar
    private Color ColorEnd   => UITheme.Success; // verde al completar

    void Awake()
    {
        if (backgroundImage)
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.55f);

        ResetBar();
    }

    /// <summary>
    /// Llama esto cada frame con t entre 0 y 1.
    /// t=0 = vacio, t=1 = lleno y pulsa.
    /// </summary>
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);

        if (fillImage)
        {
            fillImage.fillAmount = t;
            fillImage.color      = Color.Lerp(ColorStart, ColorEnd, t);
        }

        if (t >= 1f && _pulse == null)
            _pulse = StartCoroutine(PulseLoop());
    }

    /// <summary>Resetea la barra a 0 y detiene la animacion de pulso.</summary>
    public void ResetBar()
    {
        if (_pulse != null) { StopCoroutine(_pulse); _pulse = null; }
        transform.localScale = Vector3.one;

        if (fillImage)
        {
            fillImage.fillAmount = 0f;
            fillImage.color      = ColorStart;
        }
    }

    IEnumerator PulseLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 8f;
            float s = 1f + Mathf.Sin(t) * 0.05f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
    }
}
