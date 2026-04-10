using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu principal. Asigna los métodos a los botones desde el Inspector.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public string firstSceneName = "Island1";

    public void PlayGame()
    {
        SceneManager.LoadScene(firstSceneName);
    }

    public void OpenIslandSelector()
    {
        SceneManager.LoadScene("IslandSelector");
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
