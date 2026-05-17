using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu de pausa universal. Se auto-gestiona: detecta ESC, gesto de brazos
/// cruzados y perdida de tracking. Pausa el juego (Time.timeScale = 0) y
/// muestra el panel hasta que el jugador elija continuar o salir al menu.
///
/// Setup en cada escena de minijuego:
///   1. En el Canvas crea un GameObject "PauseMenu" como ultimo hijo
///      (despues del TutorialOverlay si existe, asi queda encima).
///   2. Estructura sugerida:
///        PauseMenu       (este script + CanvasGroup + Image fondo oscuro)
///          └─ Card
///               ├─ TitleText      (TMP "Pausa")
///               ├─ ResumeButton   (Button "Continuar")
///               └─ QuitButton     (Button "Salir al menu")
///   3. Asigna refs en el Inspector. Pon "Menu Scene Name" = "Islandselector".
///
/// El script NO toca a los managers de los minijuegos: usa Time.timeScale.
/// El PoseCursor ya esta migrado a unscaledDeltaTime, asi que sigue funcionando.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Refs UI")]
    public CanvasGroup     canvasGroup;
    public TextMeshProUGUI titleText;
    public Button          resumeButton;
    public Button          quitButton;

    [Header("Textos")]
    public string title     = "Pausa";

    [Header("Escena de menu")]
    [Tooltip("Nombre de la escena a la que se va al pulsar 'Salir al menu'.")]
    public string menuSceneName = "Islandselector";

    [Header("Trigger: tecla")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Trigger: gesto brazos cruzados")]
    [Tooltip("Detecta brazos cruzados sobre el pecho. Hay que mantenerlo.")]
    public bool  crossedArmsEnabled   = true;
    [Tooltip("Segundos que hay que mantener los brazos cruzados para pausar.")]
    public float crossedArmsHoldTime  = 1.5f;
    [Tooltip("Cooldown despues de cerrar el menu para no re-abrirlo al instante.")]
    public float crossedArmsCooldown  = 1.5f;

    [Header("Trigger: tracking perdido")]
    [Tooltip("Si se pierde el tracking del cuerpo por mas de N segundos, pausa.")]
    public bool  autoPauseOnTrackingLost     = true;
    public float trackingLostThresholdSeconds = 2f;

    [Header("Fade")]
    public float fadeDuration = 0.25f;

    // estado
    private float _prevTimeScale  = 1f;
    private bool  _isVisible;
    private float _crossedAccum;
    private float _trackingLostAccum;
    private float _reopenLockUntil;
    private bool  _trackingWasEverDetected;

    void Awake()
    {
        Debug.Log($"[PauseMenu] Awake corriendo en {gameObject.name}. Hijos: {transform.childCount}");
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (titleText) titleText.text = title;

        if (resumeButton)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }
        if (quitButton)
        {
            quitButton.onClick.RemoveListener(QuitToMenu);
            quitButton.onClick.AddListener(QuitToMenu);
        }

        // Refuerzo: desactiva todos los hijos visuales al inicio.
        SetChildrenActive(false);
    }

    void SetChildrenActive(bool on)
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(on);

        // El Image del propio root (fondo oscuro) tambien se enciende/apaga.
        var img = GetComponent<Image>();
        if (img != null) img.enabled = on;
    }

    void Update()
    {
        // ESC siempre disponible (testing)
        if (Input.GetKeyDown(pauseKey))
        {
            Debug.Log($"[PauseMenu] ESC detectado. _isVisible={_isVisible}, canvasGroup={(canvasGroup != null ? "OK" : "NULL")}");
            Toggle();
            return;
        }

        // Si el menu ya esta abierto, no procesamos gestos para abrirlo de nuevo.
        if (_isVisible) return;
        if (Time.unscaledTime < _reopenLockUntil) return;

        DetectCrossedArms();
        DetectTrackingLost();
    }

    void DetectCrossedArms()
    {
        if (!crossedArmsEnabled) return;
        if (PoseReceiverUDP.Instance == null || !PoseReceiverUDP.Instance.poseDetected)
        {
            _crossedAccum = 0f;
            return;
        }

        var I = PoseReceiverUDP.Instance;
        Vector3 lSh = I.GetLandmark(11);    // hombro izquierdo (user) -> aparece a la derecha en selfie
        Vector3 rSh = I.GetLandmark(12);
        Vector3 lWr = I.GetLandmark(15);    // muneca izquierda (user)
        Vector3 rWr = I.GetLandmark(16);
        Vector3 lHp = I.GetLandmark(23);
        Vector3 rHp = I.GetLandmark(24);

        float shoulderWidth = Mathf.Abs(lSh.x - rSh.x);
        if (shoulderWidth < 0.05f) { _crossedAccum = 0f; return; }

        float shoulderY = (lSh.y + rSh.y) * 0.5f;
        float hipY      = (lHp.y + rHp.y) * 0.5f;

        // Brazos cruzados:
        //  (a) ambas munecas a altura de pecho (entre hombros y caderas en Y)
        //  (b) munecas cerca una de otra en X (menos de ~50% del shoulderWidth)
        bool atChest = (lWr.y > shoulderY && lWr.y < hipY)
                    && (rWr.y > shoulderY && rWr.y < hipY);

        float wristGap = Mathf.Abs(lWr.x - rWr.x);
        bool wristsClose = wristGap < shoulderWidth * 0.5f;

        if (atChest && wristsClose)
        {
            _crossedAccum += Time.unscaledDeltaTime;
            if (_crossedAccum >= crossedArmsHoldTime)
            {
                _crossedAccum = 0f;
                Show();
            }
        }
        else
        {
            _crossedAccum = 0f;
        }
    }

    void DetectTrackingLost()
    {
        if (!autoPauseOnTrackingLost) return;

        bool detected = PoseReceiverUDP.Instance != null && PoseReceiverUDP.Instance.poseDetected;
        if (detected)
        {
            _trackingWasEverDetected = true;
            _trackingLostAccum = 0f;
        }
        else if (_trackingWasEverDetected)
        {
            _trackingLostAccum += Time.unscaledDeltaTime;
            if (_trackingLostAccum >= trackingLostThresholdSeconds)
            {
                _trackingLostAccum = 0f;
                Show();
            }
        }
    }

    public void Toggle()
    {
        if (_isVisible) Resume();
        else            Show();
    }

    public void Show()
    {
        if (_isVisible) return;
        _isVisible = true;

        SetChildrenActive(true);

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, true));
    }

    public void Resume()
    {
        if (!_isVisible) return;
        StopAllCoroutines();
        StartCoroutine(ResumeRoutine());
    }

    IEnumerator ResumeRoutine()
    {
        yield return FadeTo(0f, false);
        SetChildrenActive(false);
        Time.timeScale     = _prevTimeScale;
        _isVisible         = false;
        _crossedAccum      = 0f;
        _trackingLostAccum = 0f;
        _reopenLockUntil   = Time.unscaledTime + crossedArmsCooldown;
    }

    public void QuitToMenu()
    {
        // Antes de cargar la otra escena, asegurate de restaurar timeScale.
        Time.timeScale = 1f;
        SceneTransition.LoadScene(menuSceneName);
    }

    IEnumerator FadeTo(float target, bool interactableWhenDone)
    {
        if (canvasGroup == null) yield break;

        float start    = canvasGroup.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float t        = 0f;

        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = true;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha          = target;
        canvasGroup.interactable   = interactableWhenDone;
        canvasGroup.blocksRaycasts = interactableWhenDone;
    }

    void OnDisable()
    {
        // Seguridad: si el GameObject se desactiva con el juego pausado, restaura.
        if (_isVisible) Time.timeScale = _prevTimeScale;
    }
}
