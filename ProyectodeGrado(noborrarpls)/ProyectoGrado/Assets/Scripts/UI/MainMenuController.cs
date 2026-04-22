using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu principal unificado. Reemplaza MainMenu.cs + Menumanager.cs.
/// Setup:
///   1. Pega en Canvas/MainMenu. Borra MainMenu.cs y Menumanager.cs (evita duplicados).
///   2. En el Canvas crea botones y arrastra estos metodos en OnClick:
///       - PlayColorJump   -> boton "Color Jump"
///       - PlayMirrorWorld -> boton "Mirror World"
///       - PlaySizeSort    -> boton "Size Sort"
///       - OpenCalibration -> boton "Calibrate Camera"
///       - OpenInstructions / CloseInstructions -> botones "How to Play" / "Back"
///       - ExitGame        -> boton "Exit"
///   3. Arrastra bestScoreText (TMP) al Inspector para mostrar mejores puntuaciones.
///   4. Arrastra mainPanel (contenedor de botones principales) e instructionsPanel.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainPanel;
    public GameObject instructionsPanel;

    [Header("UI opcional")]
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI titleText;

    [Header("Nombres de escenas (deben existir en Build Settings)")]
    public string colorJumpScene   = "ColorJump";
    public string mirrorWorldScene = "Island3";
    public string sizeSortScene    = "SizeSort";
    public string calibrationScene = "Calibration";

    void Start()
    {
        if (instructionsPanel) instructionsPanel.SetActive(false);
        if (mainPanel)         mainPanel.SetActive(true);
        UpdateBestScores();
    }

    void UpdateBestScores()
    {
        if (bestScoreText == null) return;
        int color  = PlayerPrefs.GetInt("best_color",  0);
        int mirror = PlayerPrefs.GetInt("best_mirror", 0);
        int size   = PlayerPrefs.GetInt("best_size",   0);
        bestScoreText.text =
            "BEST SCORES\n" +
            $"Color Jump:  {color}\n" +
            $"Mirror World: {mirror}\n" +
            $"Size Sort:   {size}";
    }

    public void PlayColorJump()   { SceneManager.LoadScene(colorJumpScene); }
    public void PlayMirrorWorld() { SceneManager.LoadScene(mirrorWorldScene); }
    public void PlaySizeSort()    { SceneManager.LoadScene(sizeSortScene); }
    public void OpenCalibration() { SceneManager.LoadScene(calibrationScene); }

    public void OpenInstructions()
    {
        if (mainPanel)         mainPanel.SetActive(false);
        if (instructionsPanel) instructionsPanel.SetActive(true);
    }

    public void CloseInstructions()
    {
        if (instructionsPanel) instructionsPanel.SetActive(false);
        if (mainPanel)         mainPanel.SetActive(true);
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
