using UnityEngine;

/// <summary>
/// Genera sprites de rectangulo redondeado en runtime.
/// Usado por UIButtonStyle para dar bordes redondeados a los botones sin necesitar PNGs externos.
/// </summary>
public static class RoundedRectGenerator
{
    /// <summary>
    /// Genera un Sprite blanco de rectangulo redondeado, listo para 9-slice.
    /// </summary>
    /// <param name="width">Ancho de la textura en pixeles</param>
    /// <param name="height">Alto de la textura en pixeles</param>
    /// <param name="radius">Radio de las esquinas en pixeles</param>
    public static Sprite Generate(int width = 128, int height = 64, int radius = 24)
    {
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var pixels = new Color32[width * height];

        // 2x supersample: cada pixel evalua 4 sub-pixeles para anti-aliasing en esquinas
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;  x++)
        {
            int hits = 0;
            if (IsInsideRoundedRectSub(x, y, 0.25f, 0.25f, width, height, radius)) hits++;
            if (IsInsideRoundedRectSub(x, y, 0.75f, 0.25f, width, height, radius)) hits++;
            if (IsInsideRoundedRectSub(x, y, 0.25f, 0.75f, width, height, radius)) hits++;
            if (IsInsideRoundedRectSub(x, y, 0.75f, 0.75f, width, height, radius)) hits++;

            byte alpha = (byte)(255 * hits / 4);
            pixels[y * width + x] = new Color32(255, 255, 255, alpha);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        // Border = radio de esquina, para que el 9-slice no deforme las esquinas al redimensionar
        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    static bool IsInsideRoundedRectSub(int px, int py, float ox, float oy, int w, int h, int r)
    {
        float fx = px + ox;
        float fy = py + oy;

        bool inH = fx >= r && fx <= w - r;
        bool inV = fy >= r && fy <= h - r;
        if (inH || inV) return true;

        float cx = fx < r ? r : w - r;
        float cy = fy < r ? r : h - r;
        float dx = fx - cx;
        float dy = fy - cy;
        return dx * dx + dy * dy <= (float)r * r;
    }
}
