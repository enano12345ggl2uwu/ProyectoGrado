using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de seleccion de dificultad que aparece antes de iniciar cualquier minijuego.
/// Muestra difficultyPanel al inicio, oculta gamePanel.
/// Al pulsar "START", oculta el panel y arranca el juego con la dificultad elegida.
///
/// SETUP en Unity:
///  1. Canvas > crear Panel "DifficultyPanel" (fondo oscuro semitransparente)
///  2. Dentro: titulo TMP, 3 botones (EasyBtn, MediumBtn, HardBtn), boton StartBtn
///  3. El resto de la UI del juego va en otro Panel "GamePanel" (o el Canvas raiz)
///  4. Asignar en Inspector:
///       difficultyPanel -> DifficultyPanel
///       gamePanel       -> GamePanel
///       easyBtnStyle / mediumBtnStyle / hardBtnStyle -> el componente UIButtonStyle de cada boton
///       (Si los botones no tienen UIButtonStyle, asigna easyBtnImage etc. como fallback)
///  5. Conectar OnClick de cada boton:
///       EasyBtn   -> DifficultySelector.SelectEasy()
///       MediumBtn -> DifficultySelector.SelectMedium()
///       HardBtn   -> DifficultySelector.SelectHard()
///       StartBtn  -> DifficultySelector.StartGame()
/// </summary>
public class DifficultySelector : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject difficultyPanel;
    public GameObject gamePanel;

    [Header("Pose cursor (opcional — solo para el panel de dificultad)")]
    [Tooltip("Arrastra el GameObject PoseCursor. Se desactiva cuando arranca el juego.")]
    public GameObject poseCursor;

    [Header("Juego (asignar solo el que este en la escena)")]
    public ColorJumpGameUDP      colorJumpGame;
    public MirrorWordGameUDP     mirrorWordGame;
    public SizeSortGameUDP       sizeSortGame;
    public BalloonPopGameUDP     balloonPopGame;
    public NumberBalloonGameUDP  numberBalloonGame;

    [Header("Botones de dificultad — asignar UIButtonStyle de cada boton")]
    public UIButtonStyle easyBtnStyle;
    public UIButtonStyle mediumBtnStyle;
    public UIButtonStyle hardBtnStyle;

    [Header("Textos de descripcion (opcional)")]
    public TextMeshProUGUI descriptionText;

    private int  _selectedLevel  = 0; // 0=Easy, 1=Medium, 2=Hard
    private bool _initialized    = false; // evita que UpdateUI corra antes de Start()

    private readonly string[] _descriptions = {
        "EASY\nMore time, forgiving poses",
        "MEDIUM\nBalanced challenge",
        "HARD\nFast rounds, strict poses"
    };

    void Start()
    {
        if (difficultyPanel) difficultyPanel.SetActive(true);
        if (gamePanel)       gamePanel.SetActive(false);
        if (poseCursor)      poseCursor.SetActive(true);

        // Marcamos como inicializado ANTES de llamar SelectEasy para que
        // UpdateUI pueda ejecutarse. UIButtonStyle ya tiene _originalScale
        // capturado en su Awake(), asi que SetSelected() es seguro aqui.
        _initialized = true;
        SelectEasy();
    }

    // -------------------------------------------------------------------------
    // Seleccion de dificultad
    // -------------------------------------------------------------------------

    /// <summary>Selecciona dificultad Facil y actualiza los botones.</summary>
    public void SelectEasy()   { _selectedLevel = 0; UpdateUI(); }

    /// <summary>Selecciona dificultad Media y actualiza los botones.</summary>
    public void SelectMedium() { _selectedLevel = 1; UpdateUI(); }

    /// <summary>Selecciona dificultad Dificil y actualiza los botones.</summary>
    public void SelectHard()   { _selectedLevel = 2; UpdateUI(); }

    // -------------------------------------------------------------------------
    // Inicio del juego
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oculta el panel de seleccion e inicia el minijuego con la dificultad elegida.
    /// </summary>
    public void StartGame()
    {
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (gamePanel)       gamePanel.SetActive(true);
        if (poseCursor)      poseCursor.SetActive(false);

        if (colorJumpGame     != null) colorJumpGame.StartGame(_selectedLevel);
        if (mirrorWordGame    != null) mirrorWordGame.StartGame(_selectedLevel);
        if (sizeSortGame      != null) sizeSortGame.StartGame(_selectedLevel);
        if (balloonPopGame    != null) balloonPopGame.StartGame(_selectedLevel);
        if (numberBalloonGame != null) numberBalloonGame.StartGame(_selectedLevel);
    }

    // -------------------------------------------------------------------------
    // Logica interna
    // -------------------------------------------------------------------------

    /// <summary>
    /// Actualiza el estado visual de los tres botones de dificultad.
    /// Solo se ejecuta si Start() ya corrio (_initialized == true).
    /// </summary>
    private void UpdateUI()
    {
        // Proteccion: si alguna funcion Select* se llama antes del Start() de este
        // componente, no hacemos nada. UIButtonStyle tampoco estaria lista.
        if (!_initialized) return;

        SetBtn(easyBtnStyle,   _selectedLevel == 0);
        SetBtn(mediumBtnStyle, _selectedLevel == 1);
        SetBtn(hardBtnStyle,   _selectedLevel == 2);

        if (descriptionText && _selectedLevel < _descriptions.Length)
            descriptionText.text = _descriptions[_selectedLevel];
    }

    /// <summary>
    /// Aplica estado seleccionado/deseleccionado a un boton.
    /// Si el boton no esta asignado en el Inspector, lo ignora silenciosamente.
    /// </summary>
    private void SetBtn(UIButtonStyle btn, bool selected)
    {
        if (btn == null) return;
        btn.SetSelected(selected);
    }
}
