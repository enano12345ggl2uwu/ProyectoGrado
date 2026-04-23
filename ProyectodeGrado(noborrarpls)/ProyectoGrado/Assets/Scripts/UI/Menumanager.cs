using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

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