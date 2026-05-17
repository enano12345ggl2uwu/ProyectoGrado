using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anillo radial que muestra el tiempo restante de la round (1 = lleno, 0 = vacio).
/// Cambia color verde -> amarillo -> rojo segun el progreso y pulsa cuando queda <20%.
/// Pensado para colgar como hijo del PoseCursor (anillo concentrico exterior).
///
/// API:
///   bar.Show();
///   bar.SetProgress(remainingTime / totalTime); // cada frame
///   bar.Hide();
///
/// SETUP:
///   1. GameObject hijo de PoseCursor con Image (Type=Filled, Method=Radial 360, Source=Knob o RingSpriteApplier).
///   2. Pegar este script. El campo ringImage se autollena con el Image del mismo GO si esta vacio.
///   3. Desde el script del minijuego, asignar la referencia y llamar Show/SetProgress/Hide.
/// </summary>
[RequireComponent(typeof(Image))]
public class RoundProgressBar : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Image del anillo. Auto-asignado al Awake si esta vacio.")]
    public Image ringImage;

    [Header("Colores")]
    public Color colorHigh = new Color(0.30f, 0.85f, 0.40f); // verde
    public Color colorMid  = new Color(0.95f, 0.80f, 0.20f); // amarillo
    public Color colorLow  = new Color(0.92f, 0.25f, 0.25f); // rojo

    [Header("Umbrales")]
    [Range(0.2f, 0.8f)] public float midThreshold = 0.5f;
    [Range(0.05f, 0.4f)] public float lowThreshold = 0.2f;

    [Header("Pulse cuando <lowThreshold")]
    public float pulseSpeed     = 8f;
    public float pulseAmplitude = 0.06f;

    private Coroutine _pulse;
    private bool _visible = false;

    void Awake()
    {
        if (!ringImage) ringImage = GetComponent<Image>();
        // Estado inicial: oculto pero el GO sigue activo (el script puede recibir llamadas).
        // Ocultamos via alpha 0 del Image para no perder referencias.
        SetImmediateHidden();
    }

    void SetImmediateHidden()
    {
        if (ringImage)
        {
            var c = ringImage.color; c.a = 0f; ringImage.color = c;
            ringImage.fillAmount = 0f;
        }
        StopPulse();
        _visible = false;
    }

    public void Show()
    {
        _visible = true;
        if (ringImage)
        {
            ringImage.fillAmount = 1f;
            var col = colorHigh; col.a = 1f; ringImage.color = col;
        }
    }

    public void Hide()
    {
        SetImmediateHidden();
    }

    /// <summary>Llamar cada frame con t en [0,1]. 1=lleno (round empieza), 0=vacio (se acabo).</summary>
    public void SetProgress(float t)
    {
        if (!_visible) Show();
        t = Mathf.Clamp01(t);
        if (ringImage)
        {
            ringImage.fillAmount = t;
            var c = ColorFor(t); c.a = 1f; ringImage.color = c;
        }

        if (t < lowThreshold) StartPulse();
        else StopPulse();
    }

    Color ColorFor(float t)
    {
        if (t >= midThreshold)
        {
            float k = Mathf.InverseLerp(midThreshold, 1f, t);
            return Color.Lerp(colorMid, colorHigh, k);
        }
        if (t >= lowThreshold)
        {
            float k = Mathf.InverseLerp(lowThreshold, midThreshold, t);
            return Color.Lerp(colorLow, colorMid, k);
        }
        return colorLow;
    }

    void StartPulse()
    {
        if (_pulse != null) return;
        _pulse = StartCoroutine(PulseLoop());
    }

    void StopPulse()
    {
        if (_pulse != null) StopCoroutine(_pulse);
        _pulse = null;
        transform.localScale = Vector3.one;
    }

    IEnumerator PulseLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * pulseSpeed;
            float s = 1f + Mathf.Sin(t) * pulseAmplitude;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
    }
}
