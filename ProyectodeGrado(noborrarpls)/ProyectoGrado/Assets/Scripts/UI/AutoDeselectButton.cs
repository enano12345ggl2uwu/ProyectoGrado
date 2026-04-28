using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Evita que el boton se quede en estado "Selected/Highlighted" despues de hover o click.
/// Pegalo en el GameObject de cualquier boton que se quede pintado al pasar el mouse.
///
/// Tambien se puede agregar a un Canvas raiz: encuentra todos los Button hijos y les desactiva
/// la navigation para que el EventSystem no los mantenga seleccionados.
/// </summary>
[RequireComponent(typeof(Button))]
public class AutoDeselectButton : MonoBehaviour, IPointerExitHandler, IPointerUpHandler, IDeselectHandler
{
    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            // Desactiva Navigation para evitar seleccion persistente
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }
    }

    public void OnPointerExit(PointerEventData eventData)    { Deselect(); }
    public void OnPointerUp(PointerEventData eventData)      { Deselect(); }
    public void OnDeselect(BaseEventData eventData)          { /* nada */ }

    void Deselect()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
