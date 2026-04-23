using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cursor controlado por pose. Usa la muneca derecha (landmark 16) para mover
/// un puntero en pantalla y detecta "push hacia adelante" para hacer click.
/// Si push falla (ruido en Z), activa fallback dwell (mantener sobre boton X segundos).
///
/// SETUP en Unity:
///  1. En el Canvas del menu, crea un GameObject vacio "PoseCursor" como ULTIMO hijo (siempre arriba).
///  2. Adentro crea dos Image:
///       - CursorDot   (Image circular pequena, tipo 32x32, color amarillo o blanco)
///       - DwellRing   (Image con sprite "UIMask" o anillo, Image Type = Filled, Fill Method = Radial 360, Fill = 0)
///  3. Pon este script en el GameObject "PoseCursor".
///  4. Asigna en Inspector:
///       cursorRect     -> RectTransform del PoseCursor (arrastra el mismo GameObject)
///       dwellRingImage -> DwellRing
///       canvas         -> el Canvas padre
///  5. Asegurate que el Canvas tenga un componente GraphicRaycaster (lo tiene por defecto).
///  6. Asegurate que haya un EventSystem en la escena (GameObject > UI > Event System).
///  7. PoseReceiverUDP debe existir en la escena (DontDestroyOnLoad desde MainMenu).
///
/// Gestos:
///  - Push hacia camara (Z cae rapido) → click inmediato
///  - Fallback: si push no se detecta, mantener cursor sobre boton durante dwellTime → click
///
/// El script simula el click invocando button.onClick.Invoke() — compatible con botones estandar de UGUI.
/// </summary>
public class PoseCursor : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform cursorRect;
    public Image         dwellRingImage;
    public Canvas        canvas;

    [Header("Hand")]
    [Tooltip("16 = muneca derecha, 15 = muneca izquierda, 20 = indice derecho")]
    public int handLandmark = 16;

    [Header("Smoothing")]
    [Range(1f, 40f)] public float cursorSmoothing = 18f;

    [Header("Push click")]
    [Tooltip("Velocidad minima (Z por segundo, hacia camara) para contar como push.")]
    public float pushVelocityThreshold = 1.2f;
    [Tooltip("Segundos de cooldown tras un click para evitar dobles.")]
    public float clickCooldown = 0.8f;

    [Header("Dwell fallback")]
    public bool  dwellFallbackEnabled = true;
    public float dwellTime = 1.5f;

    [Header("Screen mapping")]
    [Tooltip("Expande el area util — 1.0 = mapeo directo. 1.3 = mas faciles las esquinas.")]
    public float mappingGain = 1.25f;
    public bool  mirrorX = false;

    // --- internos ---
    private Vector2 _cursorScreenPos;
    private Vector2 _cursorSmoothed;
    private float   _lastZ;
    private float   _zVelocity;
    private float   _clickLockUntil;
    private Button  _hoveredButton;
    private Button  _lastDwellButton;
    private float   _dwellAccum;

    private PointerEventData _ped;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    void Awake()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[PoseCursor] No hay EventSystem en la escena. Crea uno: GameObject > UI > Event System.");
        }
        _ped = new PointerEventData(EventSystem.current);
        if (dwellRingImage != null) dwellRingImage.fillAmount = 0f;
    }

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
        {
            if (cursorRect) cursorRect.gameObject.SetActive(false);
            return;
        }
        if (cursorRect) cursorRect.gameObject.SetActive(true);

        // 1. leer mano
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(handLandmark);

        // 2. mapear a pantalla (coords MediaPipe estan en 0..1 con y invertida)
        float nx = Mathf.Clamp01(0.5f + (lm.x - 0.5f) * mappingGain);
        float ny = Mathf.Clamp01(0.5f + (0.5f - lm.y) * mappingGain);
        if (mirrorX) nx = 1f - nx;
        _cursorScreenPos = new Vector2(nx * Screen.width, ny * Screen.height);

        // 3. suavizar
        float t = Mathf.Clamp01(Time.deltaTime * cursorSmoothing);
        _cursorSmoothed = Vector2.Lerp(_cursorSmoothed, _cursorScreenPos, t);
        if (cursorRect) cursorRect.position = _cursorSmoothed;

        // 4. velocidad Z (push detection)
        // En MediaPipe, Z negativo = mas cerca de camara. Si Z baja rapido = push.
        float zNow   = lm.z;
        float zDelta = zNow - _lastZ;
        _zVelocity   = Mathf.Lerp(_zVelocity, -zDelta / Mathf.Max(Time.deltaTime, 0.0001f), 0.3f);
        _lastZ       = zNow;

        // 5. raycast UI
        _hoveredButton = RaycastButton(_cursorSmoothed);

        // 6. click por push
        bool canClick = Time.time >= _clickLockUntil;
        if (canClick && _zVelocity > pushVelocityThreshold && _hoveredButton != null)
        {
            InvokeButton(_hoveredButton);
            ResetDwell();
            return;
        }

        // 7. fallback dwell
        if (dwellFallbackEnabled && _hoveredButton != null)
        {
            if (_hoveredButton == _lastDwellButton)
            {
                _dwellAccum += Time.deltaTime;
                if (dwellRingImage) dwellRingImage.fillAmount = Mathf.Clamp01(_dwellAccum / dwellTime);
                if (canClick && _dwellAccum >= dwellTime)
                {
                    InvokeButton(_hoveredButton);
                    ResetDwell();
                }
            }
            else
            {
                _lastDwellButton = _hoveredButton;
                _dwellAccum      = 0f;
                if (dwellRingImage) dwellRingImage.fillAmount = 0f;
            }
        }
        else
        {
            ResetDwell();
        }
    }

    Button RaycastButton(Vector2 screenPos)
    {
        if (EventSystem.current == null) return null;
        _ped.position = screenPos;
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_ped, _raycastResults);
        for (int i = 0; i < _raycastResults.Count; i++)
        {
            var b = _raycastResults[i].gameObject.GetComponentInParent<Button>();
            if (b != null && b.interactable) return b;
        }
        return null;
    }

    void InvokeButton(Button b)
    {
        b.onClick.Invoke();
        _clickLockUntil = Time.time + clickCooldown;
        if (dwellRingImage) dwellRingImage.fillAmount = 0f;
    }

    void ResetDwell()
    {
        _lastDwellButton = null;
        _dwellAccum      = 0f;
        if (dwellRingImage) dwellRingImage.fillAmount = 0f;
    }
}
