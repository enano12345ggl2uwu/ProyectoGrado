using UnityEngine;

public class SalirDelJuego : MonoBehaviour
{
    // Esta es la función que el botón va a llamar
    public void Salir()
    {
        Debug.Log("Saliendo del juego..."); // Esto es para avisarte en la consola
        Application.Quit(); // Esta línea cierra el ejecutable
    }
}