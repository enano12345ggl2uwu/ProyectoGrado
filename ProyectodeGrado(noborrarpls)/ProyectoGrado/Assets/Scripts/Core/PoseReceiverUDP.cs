using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class PoseData
{
    public bool detected;
    public float[][] landmarks;
}

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
    private bool tempDetected = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        for (int i = 0; i < 33; i++) { landmarks[i] = Vector3.zero; rawLandmarks[i] = Vector3.zero; }
    }

    void Start() { StartReceiving(); }

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
        catch (Exception e) { Debug.LogError($"[PoseReceiverUDP] Error al iniciar: {e.Message}"); }
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
            catch (Exception e) { Debug.LogWarning($"[PoseReceiverUDP] {e.Message}"); }
        }
    }

    void ParseJson(string json)
    {
        try
        {
            // Parser manual robusto: busca todos los grupos [x,y,z]
            bool detected = json.Contains("\"detected\": true") || json.Contains("\"detected\":true");

            int idx = 0;
            int landmarkIdx = 0;
            Vector3[] tempLandmarks = new Vector3[33];

            // Saltar hasta encontrar "landmarks"
            int lmStart = json.IndexOf("\"landmarks\"");
            if (lmStart < 0) return;
            idx = json.IndexOf("[[", lmStart);
            if (idx < 0) return;
            idx += 2; // saltar [[

            while (landmarkIdx < 33 && idx < json.Length)
            {
                // Buscar 3 numeros separados por coma
                float[] coords = new float[3];
                for (int c = 0; c < 3; c++)
                {
                    // Saltar espacios y caracteres no numericos hasta encontrar digito o signo
                    while (idx < json.Length && json[idx] != '-' && (json[idx] < '0' || json[idx] > '9')) idx++;
                    int numStart = idx;
                    while (idx < json.Length && (json[idx] == '-' || json[idx] == '.' || (json[idx] >= '0' && json[idx] <= '9') || json[idx] == 'e' || json[idx] == 'E' || json[idx] == '+')) idx++;
                    if (numStart == idx) break;
                    string numStr = json.Substring(numStart, idx - numStart);
                    float.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out coords[c]);
                }
                tempLandmarks[landmarkIdx] = new Vector3(coords[0], coords[1], coords[2]);
                landmarkIdx++;

                // Buscar siguiente '[' o salir si encuentra ']]'
                while (idx < json.Length && json[idx] != '[' && json[idx] != ']') idx++;
                if (idx >= json.Length || json[idx] == ']') break;
                idx++; // saltar [
            }

            lock (lockObj)
            {
                for (int i = 0; i < 33; i++) rawLandmarks[i] = tempLandmarks[i];
                tempDetected = detected;
                newDataAvailable = true;
            }
        }
        catch (Exception e) { Debug.LogWarning($"[PoseReceiverUDP] Parse error: {e.Message}"); }
    }

    void Update()
    {
        lock (lockObj)
        {
            if (newDataAvailable)
            {
                for (int i = 0; i < 33; i++)
                    landmarks[i] = Vector3.Lerp(landmarks[i], rawLandmarks[i], smoothing);
                poseDetected = tempDetected;
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