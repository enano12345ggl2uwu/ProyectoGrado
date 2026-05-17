using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente helper: al Awake reemplaza el sprite del Image al que esta pegado
/// por un anillo hueco generado por codigo (RingSpriteGenerator).
///
/// Pegalo en DwellRing y RoundProgressRing del PoseCursor.
/// El Image debe tener Type = Filled, Fill Method = Radial 360 para que se vea como anillo de progreso.
/// </summary>
[RequireComponent(typeof(Image))]
public class RingSpriteApplier : MonoBehaviour
{
    [Header("Geometria del anillo")]
    [Tooltip("Lado del sprite en pixeles. 256 es buen balance calidad/memoria.")]
    public int spriteSize = 256;

    [Tooltip("Grosor del anillo como fraccion del radio. 0.15 = delgado, 0.3 = grueso.")]
    [Range(0.05f, 0.5f)] public float thicknessRatio = 0.15f;

    void Awake()
    {
        var img = GetComponent<Image>();
        img.sprite = RingSpriteGenerator.GetRingSprite(spriteSize, thicknessRatio);
        // Si el usuario olvido configurar el Image, esto al menos lo deja visible
        if (img.type == Image.Type.Simple) img.preserveAspect = true;
    }
}
