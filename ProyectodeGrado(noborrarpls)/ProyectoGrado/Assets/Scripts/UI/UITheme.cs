using UnityEngine;

/// <summary>
/// Paleta de colores compartida por todos los scripts de UI del proyecto.
/// Cambia los valores aqui para que el cambio afecte todos los minijuegos y pantallas.
/// </summary>
public static class UITheme
{
    // Feedback
    public static readonly Color Success = new Color(0.2f,  1f,    0.3f,  1f); // verde lima
    public static readonly Color Failure = new Color(1f,    0.2f,  0.25f, 1f); // rojo brillante
    public static readonly Color Warning = new Color(1f,    0.85f, 0.2f,  1f); // amarillo dorado
    public static readonly Color Neutral = new Color(0.25f, 0.25f, 0.25f, 1f); // gris oscuro

    // Botones
    public static readonly Color ButtonBase     = new Color(1f,    0.72f, 0.10f, 1f); // naranja-amarillo (normal)
    public static readonly Color ButtonSelected = new Color(0.20f, 0.55f, 1.00f, 1f); // azul (seleccionado)

    // Colores de minijuego (plataformas, globos, palabras)
    public static readonly Color[] GameColors =
    {
        new Color(0.94f, 0.33f, 0.31f), // RED
        new Color(0.31f, 0.76f, 0.97f), // BLUE
        new Color(0.40f, 0.73f, 0.42f), // GREEN
        new Color(1.00f, 0.84f, 0.31f), // YELLOW
        new Color(1.00f, 0.60f, 0.20f), // ORANGE
        new Color(0.67f, 0.28f, 0.74f), // PURPLE
    };
}
