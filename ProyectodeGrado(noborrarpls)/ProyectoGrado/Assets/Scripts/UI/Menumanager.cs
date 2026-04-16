using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class Menumanager : MonoBehaviour
{
    [Header("Paneles del Menú")]
    public GameObject panelOpciones;

    // Función para el botón PLAY
    public void Jugar()
    {
        // Ya puse el nombre exacto de tu escena: ColorJump
        SceneManager.LoadScene("ColorJump"); 
    }

    // Funciones para el panel de SETTINGS
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