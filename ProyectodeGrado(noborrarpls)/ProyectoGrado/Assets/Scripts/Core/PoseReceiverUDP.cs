using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Recibe landmarks de MediaPipe vía UDP desde Python.
/// Singleton persistente entre escenas con suavizado Lerp.
/// Puerto: 5052
/// </summary>
public class PoseReceiverUDP : MonoBehaviour
{
    public static PoseReceiverUDP Instance { get; private set; }

    [Header("Network")]
    public int port = 5052;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float smoothing = 0.5f;

    [Header("State")]
    public bool poseDetected = false;
    public Vector3[] landmarks = new Vector3[33];

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running = false;
    private readonly object lockObj = new object();
    private Vector3[] rawLandmarks = new Vector3[33];
    private bool newDataAvailable = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < 33; i++)
        {
            landmarks[i] = Vector3.zero;
            rawLandmarks[i] = Vector3.zero;
        }
    }

    void Start()
    {
        StartReceiving();
    }

    void StartReceiving()
    {
        try
        {
            udpClient = new UdpClient(port);
            running = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log($"[PoseReceiverUDP] Escuchando puerto {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PoseReceiverUDP] Error al iniciar: {e.Message}");
        }
    }

    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(data);
                ParseJson(json);
            }
            catch (SocketException) { break; }
            catch (ThreadAbortException) { break; }
            catch (Exception e)
            {
                Debug.LogWarning($"[PoseReceiverUDP] {e.Message}");
            }
        }
    }

    void ParseJson(string json)
    {
        try
        {
            // Formato esperado: {"detected":true,"landmarks":[[x,y,z],[x,y,z],...]}
            int detectedIdx = json.IndexOf("\"detected\"");
            bool detected = json.IndexOf("true", detectedIdx) > 0 && json.IndexOf("true", detectedIdx) < json.IndexOf(",", detectedIdx) + 10;

            int startIdx = json.IndexOf("[[");
            int endIdx = json.LastIndexOf("]]");
            if (startIdx < 0 || endIdx < 0) return;

            string body = json.Substring(startIdx + 2, endIdx - startIdx - 2);
            string[] points = body.Split(new string[] { "],[" }, StringSplitOptions.None);

            lock (lockObj)
            {
                for (int i = 0; i < points.Length && i < 33; i++)
                {
                    string clean = points[i].Replace("[", "").Replace("]", "");
                    string[] coords = clean.Split(',');
                    if (coords.Length >= 3)
                    {
                        float x = float.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
                        float y = float.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
                        float z = float.Parse(coords[2], System.Globalization.CultureInfo.InvariantCulture);
                        rawLandmarks[i] = new Vector3(x, y, z);
                    }
                }
                poseDetected = detected;
                newDataAvailable = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PoseReceiverUDP] Parse error: {e.Message}");
        }
    }

    void Update()
    {
        // Aplicar suavizado Lerp en el hilo principal
        lock (lockObj)
        {
            if (newDataAvailable)
            {
                for (int i = 0; i < 33; i++)
                {
                    landmarks[i] = Vector3.Lerp(landmarks[i], rawLandmarks[i], smoothing);
                }
                newDataAvailable = false;
            }
        }
    }

    public Vector3 GetLandmark(int index)
    {
        if (index < 0 || index >= 33) return Vector3.zero;
        return landmarks[index];
    }

    void OnApplicationQuit() { Cleanup(); }
    void OnDestroy() { Cleanup(); }

    void Cleanup()
    {
        running = false;
        try { udpClient?.Close(); } catch { }
        try { receiveThread?.Abort(); } catch { }
        Debug.Log("[PoseReceiverUDP] Socket cerrado");
    }
}
