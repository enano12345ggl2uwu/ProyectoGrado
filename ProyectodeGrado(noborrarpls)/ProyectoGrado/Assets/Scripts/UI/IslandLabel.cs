using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Agrega un nombre de minijuego a cada boton de isla en Islandselector.
/// El nombre siempre es visible (pequeno y translucido); al hover se agranda y opaquea.
///
/// SETUP por isla:
///  1. En el GameObject de cada isla (el que tiene BotonIsla o Button):
///       - Add Component -> IslandLabel
///       - Crea un hijo TMP "IslandNameText" y arrastralo al campo labelText.
///       - Escribe el nombre en "minigameName" (ej. "Color Jump").
///  2. El TMP hijo puede estar debajo del sprite/imagen de la isla.
/// </summary>
public class IslandLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    public TextMeshProUGUI labelText;

    [Header("Config")]
    public string minigameName = "Color Jump";

    [Header("Idle state")]
    public float idleScale   = 0.85f;
    public float idleAlpha   = 0.55f;

    [Header("Hover state")]
    public float hoverScale  = 1.15f;
    public float hoverAlpha  = 1f;
    public float animSpeed   = 8f;

    private float _targetScale;
    private float _targetAlpha;
    private RectTransform _rt;

    void Awake()
    {
        _rt          = GetComponent<RectTransform>();
        _targetScale = idleScale;
        _targetAlpha = idleAlpha;

        if (labelText != null)
        {
            labelText.text = minigameName;
            SetAlpha(idleAlpha);
        }
        SetScale(idleScale);
    }

    void Update()
    {
        float curScale = _rt.localScale.x;
        float newScale = Mathf.Lerp(curScale, _targetScale, Time.deltaTime * animSpeed);
        _rt.localScale = Vector3.one * newScale;

        if (labelText != null)
        {
            Color c = labelText.color;
            c.a = Mathf.Lerp(c.a, _targetAlpha, Time.deltaTime * animSpeed);
            labelText.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        _targetScale = hoverScale;
        _targetAlpha = hoverAlpha;
    }

    public void OnPointerExit(PointerEventData _)
    {
        _targetScale = idleScale;
        _targetAlpha = idleAlpha;
    }

    private void SetScale(float s)  => _rt.localScale = Vector3.one * s;
    private void SetAlpha(float a)
    {
        if (labelText == null) return;
        Color c = labelText.color; c.a = a; labelText.color = c;
    }
}
