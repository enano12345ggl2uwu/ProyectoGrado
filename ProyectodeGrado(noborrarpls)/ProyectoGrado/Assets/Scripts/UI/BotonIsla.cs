using UnityEngine;
using UnityEngine.SceneManagement;

public class BotonIsla : MonoBehaviour
{
    // Al poner "string" aquí, Unity te dará una cajita en el Inspector 
    // para escribir a qué escena quieres ir con CADA isla.
    public void IrAEscena(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }
}