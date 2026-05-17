using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fade-to-black entre escenas. Singleton autocreado con DontDestroyOnLoad.
///
/// Uso:
///   SceneTransition.LoadScene("ColorJump");   // fade out -> load -> fade in
///   SceneTransition.LoadScene(0);             // por build index
///
/// No requiere setup en escena. La primera llamada (o el primer scene load)
/// crea el Canvas + Image negra y se queda vivo entre escenas.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Fade")]
    [Tooltip("Duracion del fade (out e in) en segundos. Usa unscaled time.")]
    public float fadeDuration = 0.3f;

    private CanvasGroup _group;
    private Image       _blackImage;
    private bool        _busy;

    // ------------------------------------------------------------------
    // Bootstrap automatico
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[SceneTransition]");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SceneTransition>();
        Instance.BuildCanvas();
        SceneManager.sceneLoaded += Instance.OnSceneLoaded;
    }

    void BuildCanvas()
    {
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760; // siempre encima de todo

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("BlackImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        _blackImage = imgGO.AddComponent<Image>();
        _blackImage.color = Color.black;
        _blackImage.raycastTarget = false;

        var rt = _blackImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _group = imgGO.AddComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // Cuando una escena nueva termina de cargar -> fade IN (negro -> claro)
        if (_group == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, false));
    }

    // ------------------------------------------------------------------
    // API publica
    // ------------------------------------------------------------------

    public static void LoadScene(string sceneName)
    {
        if (Instance == null) { SceneManager.LoadScene(sceneName); return; }
        Instance.StartLoad(sceneName, -1);
    }

    public static void LoadScene(int buildIndex)
    {
        if (Instance == null) { SceneManager.LoadScene(buildIndex); return; }
        Instance.StartLoad(null, buildIndex);
    }

    void StartLoad(string sceneName, int buildIndex)
    {
        if (_busy) return;
        StopAllCoroutines();
        StartCoroutine(LoadRoutine(sceneName, buildIndex));
    }

    IEnumerator LoadRoutine(string sceneName, int buildIndex)
    {
        _busy = true;
        Time.timeScale = 1f; // por si veniamos de un menu pausado
        yield return FadeRoutine(1f, true);
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
        else                                  SceneManager.LoadScene(buildIndex);
        // OnSceneLoaded hara el fade-in y resetea _busy abajo
    }

    IEnumerator FadeRoutine(float target, bool blockRaycastsDuring)
    {
        if (_group == null) yield break;

        float start    = _group.alpha;
        float duration = Mathf.Max(0.01f, fadeDuration);
        float t        = 0f;

        _group.blocksRaycasts = blockRaycastsDuring || target > 0.99f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _group.alpha          = target;
        _group.blocksRaycasts = target > 0.99f;
        _busy = false;
    }
}
