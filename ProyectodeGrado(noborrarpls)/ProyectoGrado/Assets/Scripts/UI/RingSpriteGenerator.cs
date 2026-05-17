using UnityEngine;

/// <summary>
/// Genera un Sprite de anillo hueco (donut) por codigo, antialiased.
/// Cachea por (size, thicknessRatio) para reutilizar entre rings.
///
/// Uso:
///   image.sprite = RingSpriteGenerator.GetRingSprite(256, 0.15f);
///
/// El sprite resultante es blanco con alpha. El color final lo controla
/// el componente Image.color (asi se puede tintar).
/// </summary>
public static class RingSpriteGenerator
{
    private static readonly System.Collections.Generic.Dictionary<long, Sprite> _cache
        = new System.Collections.Generic.Dictionary<long, Sprite>();

    /// <summary>
    /// Devuelve un sprite de anillo hueco.
    /// </summary>
    /// <param name="size">Lado del cuadrado en pixeles (recomendado 256).</param>
    /// <param name="thicknessRatio">Grosor del anillo como fraccion del radio. 0.15 = anillo delgado.</param>
    public static Sprite GetRingSprite(int size = 256, float thicknessRatio = 0.15f)
    {
        long key = ((long)size << 32) | (long)(thicknessRatio * 10000f);
        if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.5f - 1f;
        float innerR = outerR * (1f - thicknessRatio);
        const float aa = 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) - center.x;
                float dy = (y + 0.5f) - center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha;
                if (d > outerR + aa || d < innerR - aa)
                    alpha = 0f;
                else if (d > outerR)
                    alpha = 1f - (d - outerR) / aa;
                else if (d < innerR)
                    alpha = 1f - (innerR - d) / aa;
                else
                    alpha = 1f;

                alpha = Mathf.Clamp01(alpha);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );
        sprite.name = $"GenRing_{size}_{thicknessRatio:F2}";

        _cache[key] = sprite;
        return sprite;
    }
}
