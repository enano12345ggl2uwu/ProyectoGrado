using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dibuja un stickman fantasma en la pose objetivo para que el niño la imite.
/// Coloca este script en un GameObject vacio. En cada ronda llama ShowPose("HANDS UP") etc.
///
/// SETUP en Unity:
///   1. Hierarchy → Create Empty → renombrar "PoseSilhouette"
///   2. Position: (-3, 0, 0)  (a la izquierda del stickman del jugador)
///   3. Add Component → PoseSilhouette
///   4. En MirrorGame → MirrorWordGameUDP → arrastrar PoseSilhouette al campo "silhouette"
/// </summary>
public class PoseSilhouette : MonoBehaviour
{
    [Header("Visual")]
    public float sphereSize = 0.14f;
    public float lineWidth  = 0.05f;
    public Color color = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Transform")]
    public float   scale  = 4f;
    public Vector3 offset = Vector3.zero;

    private GameObject[]   joints;
    private LineRenderer[] bones;

    private static readonly int[] usedJoints =
        { 0, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };

    private readonly int[,] connections = new int[,]
    {
        {11,12},{11,13},{13,15},{12,14},{14,16},
        {11,23},{12,24},{23,24},
        {23,25},{25,27},{24,26},{26,28},
        {27,29},{29,31},{27,31},
        {28,30},{30,32},{28,32}
    };

    private static Dictionary<string, Vector2[]> poseData;

    void Awake()
    {
        BuildPoseData();
    }

    void Start()
    {
        joints = new GameObject[33];
        foreach (int i in usedJoints)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.parent     = transform;
            s.transform.localScale = Vector3.one * (i == 0 ? sphereSize * 2f : sphereSize);
            s.GetComponent<Renderer>().material.color = color;
            Destroy(s.GetComponent<Collider>());
            joints[i] = s;
        }

        int boneCount = connections.GetLength(0);
        bones = new LineRenderer[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            var go = new GameObject($"SilBone_{i}");
            go.transform.parent = transform;
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = lr.endWidth = lineWidth;
            lr.material      = new Material(Shader.Find("Sprites/Default"));
            lr.startColor    = lr.endColor = color;
            lr.positionCount = 2;
            bones[i] = lr;
        }
    }

    public void ShowPose(string poseName)
    {
        if (poseData == null || !poseData.ContainsKey(poseName)) return;

        Vector2[] lms   = poseData[poseName];
        Vector3   anchor = transform.position + offset;

        foreach (int i in usedJoints)
        {
            if (joints[i] == null) continue;
            joints[i].transform.position = new Vector3(
                (lms[i].x - 0.5f) * scale,
                (0.5f - lms[i].y)  * scale,
                0f
            ) + anchor;
            joints[i].SetActive(true);
        }

        for (int i = 0; i < bones.Length; i++)
        {
            int a = connections[i, 0], b = connections[i, 1];
            if (joints[a] == null || joints[b] == null) continue;
            bones[i].SetPosition(0, joints[a].transform.position);
            bones[i].SetPosition(1, joints[b].transform.position);
            bones[i].gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (joints == null) return;
        foreach (int i in usedJoints)
            if (joints[i] != null) joints[i].SetActive(false);
        if (bones == null) return;
        foreach (var b in bones)
            if (b != null) b.gameObject.SetActive(false);
    }

    // ── Construccion de poses ──────────────────────────────────────────────

    static void BuildPoseData()
    {
        if (poseData != null) return;
        poseData = new Dictionary<string, Vector2[]>();

        Vector2[] N = NeutralLandmarks();

        poseData["HANDS UP"]      = MakePose(N, (13,.62f,.20f),(14,.38f,.20f),(15,.60f,.04f),(16,.40f,.04f));
        poseData["T POSE"]        = MakePose(N, (13,.74f,.34f),(14,.26f,.34f),(15,.90f,.34f),(16,.10f,.34f));
        poseData["TOUCH HEAD"]    = MakePose(N, (13,.60f,.22f),(15,.52f,.08f));
        poseData["ARMS WIDE"]     = MakePose(N, (13,.72f,.40f),(14,.28f,.40f),(15,.92f,.42f),(16,.08f,.42f));
        poseData["HANDS DOWN"]    = MakePose(N, (13,.62f,.54f),(14,.38f,.54f),(15,.59f,.73f),(16,.41f,.73f));
        poseData["SQUAT"]         = MakePose(N, (23,.57f,.70f),(24,.43f,.70f),
                                                (25,.63f,.76f),(26,.37f,.76f),
                                                (27,.60f,.92f),(28,.40f,.92f),
                                                (13,.64f,.52f),(14,.36f,.52f),
                                                (15,.66f,.68f),(16,.34f,.68f));
        poseData["ONE ARM UP"]    = MakePose(N, (13,.60f,.20f),(15,.58f,.04f));
        poseData["HANDS ON HIPS"] = MakePose(N, (13,.63f,.48f),(14,.37f,.48f),(15,.57f,.62f),(16,.43f,.62f));
    }

    static Vector2[] NeutralLandmarks()
    {
        var lm = new Vector2[33];
        for (int i = 0; i < 33; i++) lm[i] = new Vector2(0.5f, 0.5f);
        lm[0]  = new Vector2(0.50f, 0.08f);
        lm[11] = new Vector2(0.58f, 0.34f); lm[12] = new Vector2(0.42f, 0.34f);
        lm[13] = new Vector2(0.64f, 0.50f); lm[14] = new Vector2(0.36f, 0.50f);
        lm[15] = new Vector2(0.66f, 0.65f); lm[16] = new Vector2(0.34f, 0.65f);
        lm[23] = new Vector2(0.55f, 0.62f); lm[24] = new Vector2(0.45f, 0.62f);
        lm[25] = new Vector2(0.55f, 0.78f); lm[26] = new Vector2(0.45f, 0.78f);
        lm[27] = new Vector2(0.55f, 0.93f); lm[28] = new Vector2(0.45f, 0.93f);
        lm[29] = new Vector2(0.56f, 0.96f); lm[30] = new Vector2(0.44f, 0.96f);
        lm[31] = new Vector2(0.54f, 0.97f); lm[32] = new Vector2(0.46f, 0.97f);
        return lm;
    }

    static Vector2[] MakePose(Vector2[] neutral, params (int i, float x, float y)[] overrides)
    {
        var lm = (Vector2[])neutral.Clone();
        foreach (var (i, x, y) in overrides) lm[i] = new Vector2(x, y);
        return lm;
    }
}
