using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de seleccion de dificultad que aparece antes de iniciar cualquier minijuego.
/// Muestra difficultyPanel al inicio, oculta gamePanel.
/// Al pulsar "START", oculta el panel y arranca el juego con la dificultad elegida.
///
/// SETUP en Unity:
///  1. Canvas > crear Panel "DifficultyPanel" (fondo oscuro semitransparente)
///  2. Dentro: titulo TMP, 3 botones (EasyBtn, MediumBtn, HardBtn), boton StartBtn
///  3. El resto de la UI del juego va en otro Panel "GamePanel" (o el Canvas raiz)
///  4. Asignar en Inspector:
///       difficultyPanel -> DifficultyPanel
///       gamePanel       -> GamePanel  (o el Canvas del juego)
///       colorJumpGame   -> ColorJumpManager  (si es ColorJump, si no dejar vacio)
///       mirrorWordGame  -> MirrorGame        (si es MirrorWord, si no dejar vacio)
///       easyBtn/mediumBtn/hardBtn -> los 3 botones
///  5. Conectar OnClick de cada boton:
///       EasyBtn   -> DifficultySelector.SelectEasy()
///       MediumBtn -> DifficultySelector.SelectMedium()
///       HardBtn   -> DifficultySelector.SelectHard()
///       StartBtn  -> DifficultySelector.StartGame()
/// </summary>
public class DifficultySelector : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject difficultyPanel;
    public GameObject gamePanel;

    [Header("Juego (asignar solo el que este en la escena)")]
    public ColorJumpGameUDP colorJumpGame;
    public MirrorWordGameUDP mirrorWordGame;

    [Header("Botones de dificultad")]
    public Image easyBtnImage;
    public Image mediumBtnImage;
    public Image hardBtnImage;

    [Header("Textos de descripcion (opcional)")]
    public TextMeshProUGUI descriptionText;

    [Header("Colores")]
    public Color selectedColor   = new Color(0.2f, 1f, 0.3f, 1f);
    public Color unselectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    private int selectedLevel = 0; // 0=Easy, 1=Medium, 2=Hard

    private readonly string[] descriptions = {
        "EASY\nMore time, forgiving poses",
        "MEDIUM\nBalanced challenge",
        "HARD\nFast rounds, strict poses"
    };

    void Start()
    {
        if (difficultyPanel) difficultyPanel.SetActive(true);
        if (gamePanel)       gamePanel.SetActive(false);
        SelectEasy();
    }

    public void SelectEasy()
    {
        selectedLevel = 0;
        UpdateUI();
    }

    public void SelectMedium()
    {
        selectedLevel = 1;
        UpdateUI();
    }

    public void SelectHard()
    {
        selectedLevel = 2;
        UpdateUI();
    }

    public void StartGame()
    {
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (gamePanel)       gamePanel.SetActive(true);

        if (colorJumpGame  != null) colorJumpGame.StartGame(selectedLevel);
        if (mirrorWordGame != null) mirrorWordGame.StartGame(selectedLevel);
    }

    void UpdateUI()
    {
        if (easyBtnImage)   easyBtnImage.color   = selectedLevel == 0 ? selectedColor : unselectedColor;
        if (mediumBtnImage) mediumBtnImage.color = selectedLevel == 1 ? selectedColor : unselectedColor;
        if (hardBtnImage)   hardBtnImage.color   = selectedLevel == 2 ? selectedColor : unselectedColor;

        if (descriptionText && selectedLevel < descriptions.Length)
            descriptionText.text = descriptions[selectedLevel];
    }
}
