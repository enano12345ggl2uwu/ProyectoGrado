using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla final de resultados al acabar una sesion. Se usa dentro de la escena del minijuego.
/// Guarda best score en PlayerPrefs bajo la key "best_{minigameKey}".
///
/// Setup:
///  1. En el Canvas del minijuego, crea un Panel "ResultsPanel" oculto al inicio.
///  2. Dentro: titleText (TMP), scoreText (TMP), bestText (TMP), 2 botones (Replay, Menu).
///  3. Pega este script en el Panel o un hijo. Arrastra panel = ResultsPanel, textos, y configura minigameKey.
///  4. En el boton Replay conecta OnClick -> ResultsScreen.ReplayScene()
///  5. En el boton Menu conecta OnClick -> ResultsScreen.BackToMenu()
///  6. Desde el script del minijuego, al acabar llama results.Show(score, rounds).
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestText;

    [Header("Config")]
    public string minigameKey = "mirror"; // "color" | "mirror" | "size"
    public string mainMenuScene = "MainMenu";

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    public void Show(int finalScore, int rounds)
    {
        if (panel) panel.SetActive(true);
        if (titleText) titleText.text = "GREAT JOB!";
        if (scoreText) scoreText.text = $"Score: {finalScore}\nRounds: {rounds}";

        string key = $"best_{minigameKey}";
        int best   = PlayerPrefs.GetInt(key, 0);
        if (finalScore > best)
        {
            best = finalScore;
            PlayerPrefs.SetInt(key, best);
            PlayerPrefs.Save();
            if (bestText) bestText.text = $"NEW BEST: {best}!";
        }
        else
        {
            if (bestText) bestText.text = $"Best: {best}";
        }
    }

    public void ReplayScene()
    {
        if (panel) panel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
