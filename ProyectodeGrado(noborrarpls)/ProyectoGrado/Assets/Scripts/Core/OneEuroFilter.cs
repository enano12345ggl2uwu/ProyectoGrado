using UnityEngine;

/// <summary>
/// One-Euro Filter: suaviza datos ruidosos (landmarks de MediaPipe) sin lag.
/// Se aplica por eje (X, Y, Z) de cada landmark.
///
/// Parámetros ajustables:
/// - minCutoff: frecuencia de corte mínima (Hz). Más bajo = más suave.
/// - beta: velocidad de cambio de cutoff. Más alto = más responsivo a cambios rápidos.
/// - dCutoff: damping cutoff. Típicamente 1.0.
/// </summary>
public class OneEuroFilter
{
    public float minCutoff = 1.0f;
    public float beta = 0.007f;
    public float dCutoff = 1.0f;
    public float freq = 30f; // Hz del stream de datos (ajusta si tu MediaPipe es diferente)

    private float x_prev;
    private float dx_prev;
    private float lastTime;

    public OneEuroFilter()
    {
        x_prev = 0f;
        dx_prev = 0f;
        lastTime = Time.realtimeSinceStartup;
    }

    public float Filter(float x)
    {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastTime;
        lastTime = now;

        if (dt <= 0f) return x_prev;

        // Estimar velocidad
        float dx = (x - x_prev) / dt;

        // Low-pass filter en la velocidad
        float cutoff = minCutoff + beta * Mathf.Abs(dx);
        float alpha = CutoffToAlpha(cutoff, dt);
        float dx_filtered = alpha * dx + (1f - alpha) * dx_prev;

        // Low-pass filter en la posición
        alpha = CutoffToAlpha(cutoff, dt);
        float x_filtered = alpha * x + (1f - alpha) * x_prev;

        x_prev = x_filtered;
        dx_prev = dx_filtered;

        return x_filtered;
    }

    private float CutoffToAlpha(float cutoff, float dt)
    {
        float tau = 1f / (2f * Mathf.PI * cutoff);
        return 1f / (1f + tau / dt);
    }
}
