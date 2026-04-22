using UnityEngine;
using TMPro;

/// <summary>
/// Panel simple de instrucciones. Usalo en MainMenu o dentro de un minijuego.
/// Setup:
///  1. En el Canvas, crea Panel "InstructionsPanel" oculto al inicio.
///  2. Dentro: titleText (TMP) y bodyText (TMP) con el texto principal.
///  3. Pega este script en el Panel. Arrastra los textos en el Inspector.
///  4. Boton "Back" -> OnClick -> InstructionsScreen.Hide()
///     (o si lo usas desde MainMenuController, conecta a CloseInstructions).
/// </summary>
public class InstructionsScreen : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Texto default")]
    [TextArea(3, 8)]
    public string defaultBody =
        "1. Start the Python pose sender (pose_sender_udp.py)\n" +
        "2. Stand 2 meters from your webcam\n" +
        "3. Copy the pose or color shown on screen\n" +
        "4. Hold the pose to score points!";

    public string defaultTitle = "HOW TO PLAY";

    void Start()
    {
        if (titleText && string.IsNullOrEmpty(titleText.text)) titleText.text = defaultTitle;
        if (bodyText  && string.IsNullOrEmpty(bodyText.text))  bodyText.text  = defaultBody;
    }

    public void Show() { if (panel) panel.SetActive(true); }
    public void Hide() { if (panel) panel.SetActive(false); }
}
