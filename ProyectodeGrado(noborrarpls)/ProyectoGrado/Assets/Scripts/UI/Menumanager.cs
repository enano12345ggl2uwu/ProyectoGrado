using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu manager legacy (reemplazado por MainMenuController.cs).
/// Se mantiene por compatibilidad con la escena antigua "Islandselector".
/// SETUP:
///  1. En Canvas/MainMenu pon este script.
///  2. Arrastra panelOpciones (Panel oculto de Opciones) en el Inspector.
///  3. Conecta OnClick de los botones a Jugar / AbrirOpciones / CerrarOpciones / SalirDelJuego.
/// NOTA: Para nuevas escenas usa MainMenuController.cs.
/// </summary>
public class Menumanager : MonoBehaviour
{
    [Header("Paneles del Menú")]
    public GameObject panelOpciones;

    
    public void Jugar()
    {
        
        SceneManager.LoadScene("Islandselector"); 
    }

    
    public void AbrirOpciones()
    {
        panelOpciones.SetActive(true);
    }

    public void CerrarOpciones()
    {
        panelOpciones.SetActive(false);
    }

    public void SalirDelJuego()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }
}