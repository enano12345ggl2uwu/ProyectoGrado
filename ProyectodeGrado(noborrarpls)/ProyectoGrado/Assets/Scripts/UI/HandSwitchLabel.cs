using UnityEngine;
using TMPro;

/// <summary>
/// Actualiza el texto del boton "Cambiar mano" segun la mano activa del PoseCursor.
/// Pegalo en el mismo Button que llama PoseCursor.SwitchHand().
///
/// Setup:
///  1. Boton SwitchHandBtn con TMP hijo (texto).
///  2. Add Component -> HandSwitchLabel.
///  3. Arrastra el TMP al campo "label".
///  4. Arrastra el GameObject PoseCursor al campo "cursor".
/// </summary>
public class HandSwitchLabel : MonoBehaviour
{
    public TextMeshProUGUI label;
    public PoseCursor      cursor;

    [Header("Textos")]
    public string rightText = "Mano: Derecha";
    public string leftText  = "Mano: Izquierda";

    void Update()
    {
        if (label == null || cursor == null) return;
        label.text = cursor.handLandmark == 16 ? rightText : leftText;
    }
}
