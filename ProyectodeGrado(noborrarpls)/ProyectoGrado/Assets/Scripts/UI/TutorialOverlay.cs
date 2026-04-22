using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Overlay de tutorial para el primer round de cualquier minijuego.
/// Setup:
///  1. En el Canvas del juego, crea Panel "TutorialOverlay" oculto al inicio.
///  2. Dentro: titleText (TMP grande) y bodyText (TMP mediano).
///  3. Pega este script en el Panel. Arrastra titleText, bodyText.
///  4. Desde el minijuego (MirrorWord/ColorJump/SizeSort): arrastra este TutorialOverlay
///     y llama tutorial.ShowForSeconds("titulo", "cuerpo", 3f) antes del primer round.
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    public void Show(string title, string body)
    {
        if (panel) panel.SetActive(true);
        if (titleText) titleText.text = title;
        if (bodyText)  bodyText.text  = body;
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }

    public IEnumerator ShowForSeconds(string title, string body, float seconds)
    {
        Show(title, body);
        yield return new WaitForSeconds(seconds);
        Hide();
    }
}
