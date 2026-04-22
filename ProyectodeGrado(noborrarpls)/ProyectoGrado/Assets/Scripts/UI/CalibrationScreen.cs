using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla de calibracion de camara. Muestra estado de MediaPipe y deja al niño moverse libremente.
/// Setup:
///   1. Crea escena nueva "Calibration" (File > New Scene) y añadela a Build Settings.
///   2. En la escena: PoseReceiver (PoseReceiverUDP), StickFigure (StickFigureUDP), Camera, Light.
///   3. Canvas con: statusText (TMP grande, arriba), hintText (TMP medio, abajo),
///      Boton "Back to Menu" -> OnClick -> CalibrationScreen.BackToMenu().
///   4. Pega este script en un GameObject "CalibrationManager" y arrastra los textos.
/// </summary>
public class CalibrationScreen : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hintText;

    [Header("Config")]
    public float updateInterval   = 0.5f;
    public string mainMenuScene   = "MainMenu";

    [TextArea(2, 6)]
    public string hintMessage =
        "1. Make sure pose_sender_udp.py is running\n" +
        "2. Stand 2 meters from your webcam\n" +
        "3. Move around — the stick figure should copy you\n" +
        "4. Press BACK when ready";

    private float _nextUpdate;

    void Start()
    {
        if (hintText) hintText.text = hintMessage;
    }

    void Update()
    {
        if (Time.time < _nextUpdate) return;
        _nextUpdate = Time.time + updateInterval;
        if (statusText == null) return;

        if (PoseReceiverUDP.Instance == null)
        {
            statusText.text  = "NO RECEIVER";
            statusText.color = Color.red;
            return;
        }
        if (PoseReceiverUDP.Instance.poseDetected)
        {
            statusText.text  = "CAMERA OK";
            statusText.color = new Color(0.2f, 1f, 0.3f, 1f);
        }
        else
        {
            statusText.text  = "NO POSE DETECTED";
            statusText.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
