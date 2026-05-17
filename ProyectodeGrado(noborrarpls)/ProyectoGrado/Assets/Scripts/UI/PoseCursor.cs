using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cursor controlado por pose. Usa la muneca derecha (landmark 16) para mover
/// un puntero en pantalla.
///
/// Gestos disponibles:
///  - Dwell: mantener cursor sobre boton X segundos → click
///  - Push (opcional): empujar hacia camara → click inmediato
///  - Brazo arriba: subir el otro brazo alto → click en el boton hovereado
///
/// SETUP en Unity:
///  1. En el Canvas, crea "PoseCursor" como ULTIMO hijo (siempre arriba).
///  2. Hijos: CursorDot (Image circular) + DwellRing (Image Filled Radial360).
///  3. Asigna cursorRect, dwellRingImage, canvas en el Inspector.
///  4. EventSystem y GraphicRaycaster deben existir en la escena.
/// </summary>
public class PoseCursor : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform cursorRect;
    public Image         dwellRingImage;
    public Canvas        canvas;

    [Header("Hand")]
    [Tooltip("16 = muneca derecha, 15 = muneca izquierda")]
    public int  handLandmark      = 15;
    [Tooltip("Usa automaticamente la mano que este mas arriba.")]
    public bool autoSwitchHand    = false;
    public float autoSwitchThreshold = 0.05f;

    [Header("Smoothing")]
    [Range(1f, 40f)] public float cursorSmoothing = 25f;
    public float deadzonePixels = 2f;

    [Header("Click — Dwell")]
    public bool  dwellEnabled = true;
    [Tooltip("Segundos que hay que mantener el cursor sobre un boton para hacer click.")]
    public float dwellTime    = 3f;

    [Header("Click — Push (opcional)")]
    public bool  pushClickEnabled       = false;
    public float pushVelocityThreshold  = 1.2f;

    [Header("Click — Brazo arriba")]
    [Tooltip("Subir el OTRO brazo (el que no controla el cursor) activa el boton hovereado.")]
    public bool  armRaiseEnabled   = true;
    [Tooltip("Umbral Y de MediaPipe (0=top,1=bottom). Valor bajo = mano muy arriba. 0.25 = arriba del 75%.")]
    public float armRaiseThreshold = 0.25f;
    public float armRaiseCooldown  = 1.0f;

    [Header("Cooldown")]
    public float clickCooldown = 0.8f;

    [Header("Screen mapping")]
    public float mappingGain = 1.0f;
    public bool  mirrorX     = false;

    // --- internos ---
    private Vector2 _cursorScreenPos;
    private Vector2 _cursorSmoothed;
    private float   _lastZ;
    private float   _zVelocity;
    private float   _clickLockUntil;
    private float   _armRaiseLockUntil;

    private Button  _hoveredButton;
    private Button  _prevHoveredButton;
    private Button  _lastDwellButton;
    private float   _dwellAccum;

    private PointerEventData          _ped;
    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    void Awake()
    {
        _ped = new PointerEventData(EventSystem.current);
        if (dwellRingImage != null)
        {
            dwellRingImage.fillAmount = 0f;
            dwellRingImage.gameObject.SetActive(dwellEnabled);
        }
        DisableRaycastOnSelfAndChildren();
    }

    void DisableRaycastOnSelfAndChildren()
    {
        if (cursorRect == null) return;
        foreach (var g in cursorRect.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
    }

    // Oculta/muestra los visuales sin deshabilitar el GameObject del script
    // (necesario porque cursorRect puede ser el propio PoseCursor).
    void SetCursorVisible(bool visible)
    {
        if (cursorRect == null) return;
        foreach (var g in cursorRect.GetComponentsInChildren<Graphic>(true))
            g.enabled = visible;
    }

    public void SwitchHand()             => handLandmark = handLandmark == 16 ? 15 : 16;
    public void SetHand(int landmark)    => handLandmark = landmark;

    void Update()
    {
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
        {
            SetCursorVisible(false);
            FireExitIfNeeded();
            return;
        }
        SetCursorVisible(true);

        // Auto-switch: usa la mano mas arriba (Y mas baja en MediaPipe)
        if (autoSwitchHand)
        {
            Vector3 r = PoseReceiverUDP.Instance.GetLandmark(16);
            Vector3 l = PoseReceiverUDP.Instance.GetLandmark(15);
            float diff = l.y - r.y;
            if (diff >  autoSwitchThreshold) handLandmark = 16;
            if (diff < -autoSwitchThreshold) handLandmark = 15;
        }

        // 1. Leer mano → pantalla
        Vector3 lm = PoseReceiverUDP.Instance.GetLandmark(handLandmark);
        float nx = Mathf.Clamp01(0.5f + (lm.x - 0.5f) * mappingGain);
        float ny = Mathf.Clamp01(0.5f + (0.5f - lm.y) * mappingGain);
        if (mirrorX) nx = 1f - nx;
        _cursorScreenPos = new Vector2(nx * Screen.width, ny * Screen.height);

        // 2. Suavizar con deadzone
        float dist = Vector2.Distance(_cursorScreenPos, _cursorSmoothed);
        if (dist > deadzonePixels)
            _cursorSmoothed = Vector2.Lerp(_cursorSmoothed, _cursorScreenPos,
                                           Mathf.Clamp01(Time.unscaledDeltaTime * cursorSmoothing));
        if (cursorRect) cursorRect.position = _cursorSmoothed;

        // 3. Velocidad Z (push)
        float zDelta = lm.z - _lastZ;
        _zVelocity   = Mathf.Lerp(_zVelocity, -zDelta / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.3f);
        _lastZ       = lm.z;

        // 4. Raycast UI → boton hovereado
        _hoveredButton = RaycastButton(_cursorSmoothed);
        FireHoverEvents();

        bool canClick = Time.time >= _clickLockUntil;

        // 5. Push click
        if (pushClickEnabled && canClick && _zVelocity > pushVelocityThreshold && _hoveredButton != null)
        {
            InvokeButton(_hoveredButton); ResetDwell(); return;
        }

        // 6. Brazo arriba
        if (armRaiseEnabled && canClick && Time.time >= _armRaiseLockUntil && _hoveredButton != null)
        {
            int other = handLandmark == 16 ? 15 : 16;
            Vector3 otherLm = PoseReceiverUDP.Instance.GetLandmark(other);
            if (otherLm.y < armRaiseThreshold)
            {
                InvokeButton(_hoveredButton);
                _armRaiseLockUntil = Time.time + armRaiseCooldown;
                ResetDwell();
                return;
            }
        }

        // 7. Dwell
        if (dwellEnabled && _hoveredButton != null)
        {
            if (_hoveredButton == _lastDwellButton)
            {
                _dwellAccum += Time.unscaledDeltaTime;
                if (dwellRingImage) dwellRingImage.fillAmount = Mathf.Clamp01(_dwellAccum / dwellTime);
                if (canClick && _dwellAccum >= dwellTime)
                {
                    InvokeButton(_hoveredButton); ResetDwell();
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

    // -------------------------------------------------------------------------
    // Hover events — dispara OnPointerEnter/Exit en UIButtonStyle
    // -------------------------------------------------------------------------

    void FireHoverEvents()
    {
        if (_hoveredButton == _prevHoveredButton) return;

        if (_prevHoveredButton != null)
            ExecuteEvents.Execute(_prevHoveredButton.gameObject, _ped,
                                  ExecuteEvents.pointerExitHandler);

        if (_hoveredButton != null)
            ExecuteEvents.Execute(_hoveredButton.gameObject, _ped,
                                  ExecuteEvents.pointerEnterHandler);

        _prevHoveredButton = _hoveredButton;
    }

    void FireExitIfNeeded()
    {
        if (_prevHoveredButton == null) return;
        ExecuteEvents.Execute(_prevHoveredButton.gameObject, _ped,
                              ExecuteEvents.pointerExitHandler);
        _prevHoveredButton = null;
        ResetDwell();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    Button RaycastButton(Vector2 screenPos)
    {
        if (EventSystem.current == null) return null;
        _ped.position = screenPos;
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_ped, _raycastResults);
        foreach (var r in _raycastResults)
        {
            var b = r.gameObject.GetComponentInParent<Button>();
            if (b != null && b.interactable) return b;
        }
        return null;
    }

    void InvokeButton(Button b)
    {
        ExecuteEvents.Execute(b.gameObject, _ped, ExecuteEvents.pointerClickHandler);
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
