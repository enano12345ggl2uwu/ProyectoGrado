using UnityEngine;

/// <summary>
/// Overlay de debug. Presiona F1 para mostrar/ocultar.
/// Muestra: FPS, paquetes/seg, tiempo último paquete, estado de conexión.
/// No hace falta agregar nada al Inspector — se auto-crea con OnGUI.
/// </summary>
public class PoseDebugHUD : MonoBehaviour
{
    private bool visible = false;
    private float fps = 0f;
    private float fpsTimer = 0f;
    private int frameCount = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            visible = !visible;

        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fps = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        GUIStyle box = new GUIStyle(GUI.skin.box);
        box.fontSize = 16;
        box.normal.textColor = Color.white;

        GUIStyle label = new GUIStyle(GUI.skin.label);
        label.fontSize = 15;
        label.normal.textColor = Color.white;

        GUI.Box(new Rect(10, 10, 280, 150), "");

        float y = 15f;
        float lineH = 22f;

        GUI.Label(new Rect(15, y, 270, lineH), $"FPS: {fps:F1}", label); y += lineH;

        if (PoseReceiverUDP.Instance != null)
        {
            GUI.Label(new Rect(15, y, 270, lineH),
                $"Conectado: {PoseReceiverUDP.Instance.IsConnected}", label); y += lineH;
            GUI.Label(new Rect(15, y, 270, lineH),
                $"Paquetes/seg: {PoseReceiverUDP.Instance.PacketsPerSecond:F1}", label); y += lineH;
            GUI.Label(new Rect(15, y, 270, lineH),
                $"Último paquete: {PoseReceiverUDP.Instance.LastPacketAge * 1000f:F0} ms", label); y += lineH;
            GUI.Label(new Rect(15, y, 270, lineH),
                $"Pose detectada: {PoseReceiverUDP.Instance.poseDetected}", label); y += lineH;
        }
        else
        {
            GUI.Label(new Rect(15, y, 270, lineH), "PoseReceiverUDP: no encontrado", label);
        }
    }
}
