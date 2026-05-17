using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel final de minijuego. Muestra puntaje, estrellas, NEW BEST, y avanza
/// automaticamente al siguiente minijuego despues de N segundos.
///
/// Setup en Unity (en el Canvas del minijuego):
///  1. Crea Panel "ResultsPanel" oculto al inicio.
///  2. Hijos sugeridos:
///       - TitleText        (TMP) "GREAT JOB!"
///       - SubtitleText     (TMP) opcional, ej. "ColorJump"
///       - ScoreText        (TMP) "Score: 12"
///       - BestText         (TMP) "Best: 15" o "NEW BEST!"
///       - Star1, Star2, Star3 (Image) — pon sprites de estrella, color blanco
///       - PlayAgainButton    (Button) -> ReplayScene()
///       - NextButton         (Button) -> NextScene()
///       - LevelSelectButton  (Button) -> BackToLevelSelect()
///       - NextRingImage      (Image, Type=Filled, Radial360, fillAmount=0) hijo de NextButton
///  3. Pega este script en ResultsPanel y arrastra refs.
///  4. Configura minigameKey ("color"|"balloon"|"size"|"mirror") y nextSceneName.
///  5. Conecta los OnClick de cada boton.
///  6. Desde el script del minijuego, al acabar: results.Show(score, rounds, maxPossibleScore).
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    [Tooltip("Panel del juego (stickfigure, HUD). Se oculta cuando aparece el resultado.")]
    public GameObject gamePanel;

    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestText;

    [Header("Music")]
    [Tooltip("Clip que suena cuando se muestra el panel de resultados (ej. panel.mp3).")]
    public AudioClip panelMusic;

    [Header("Celebration Sound")]
    [Tooltip("AudioSource para el sonido de celebracion. Puede ser el mismo que la escena tenga.")]
    public AudioSource celebrationAudioSource;
    [Tooltip("Sonido de celebracion que suena cuando aparece el GREAT JOB! (ej. fanfara, aplausos).")]
    public AudioClip   celebrationClip;

    [Header("Stars (3)")]
    public Image[] stars;
    public Color   starOnColor  = new Color(1f, 0.85f, 0.2f, 1f);
    public Color   starOffColor = new Color(1f, 1f, 1f, 0.25f);
    [Tooltip("Escala de StarYellow respecto al padre (1.3 = 30% más grande).")]
    public float   starYellowScale = 1.3f;

    [Header("Auto-advance")]
    [Tooltip("Imagen Filled/Radial360 hija del boton Next. Se llena durante el countdown.")]
    public Image nextCountdownRing;
    [Tooltip("Segundos antes de avanzar automaticamente al siguiente minijuego.")]
    public float autoAdvanceSeconds = 5f;
    [Tooltip("Si esta en false, queda esperando que el jugador presione Next.")]
    public bool  autoAdvanceEnabled = true;

    [Header("Pose Cursor")]
    [Tooltip("GameObject PoseCursor (ultimo hijo del Canvas). Se activa al mostrar el panel.")]
    public GameObject poseCursor;

    [Header("Config")]
    public string minigameKey   = "color";       // "color" | "balloon" | "size" | "mirror"
    public string mainMenuScene    = "MainMenu";
    public string levelSelectScene = "Islandselector";
    [Tooltip("Escena del siguiente minijuego en la progresion. Vacio = vuelve al menu.")]
    public string nextSceneName = "";

    [Header("Stars thresholds (% of maxScore)")]
    [Range(0f, 1f)] public float oneStarPct   = 0.33f;
    [Range(0f, 1f)] public float twoStarsPct  = 0.66f;
    [Range(0f, 1f)] public float threeStarsPct = 0.95f;

    private Coroutine _autoCo;
    private bool _advanced;

    // NO ocultamos el panel en Start(). Razon:
    // Si el panel apunta al MISMO GameObject que tiene este script y esta inactivo
    // al inicio de la escena, Start() NO corre. Cuando Show() lo activa, Start()
    // corre por primera vez y vuelve a llamar SetActive(false), ocultando el panel.
    // Por eso el panel debe desactivarse MANUALMENTE en el Inspector al editar la escena.

    /// <summary>Llamar al terminar la sesion del minijuego.</summary>
    public void Show(int finalScore, int rounds, int maxScore = 0)
    {
        if (panel == null)
        {
            Debug.LogError("[ResultsScreen] El campo 'Panel' NO esta asignado. Arrastra el GameObject ResultsPanel a si mismo en el Inspector.");
            return;
        }
        if (gamePanel) gamePanel.SetActive(false);
        panel.SetActive(true);
        if (poseCursor) poseCursor.SetActive(true);

        if (panelMusic != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.SetClip(panelMusic);
        }

        if (titleText)    titleText.text    = "GREAT JOB!";
        if (subtitleText) subtitleText.text = PrettyName(minigameKey);
        if (scoreText)    scoreText.text    = $"Score: {finalScore}\nRounds: {rounds}";

        if (celebrationAudioSource && celebrationClip)
            celebrationAudioSource.PlayOneShot(celebrationClip);

        // best score
        string key = $"best_{minigameKey}";
        int best   = PlayerPrefs.GetInt(key, 0);
        bool newBest = finalScore > best;
        if (newBest)
        {
            best = finalScore;
            PlayerPrefs.SetInt(key, best);
            PlayerPrefs.Save();
            if (bestText) bestText.text = $"NEW BEST: {best}!";
        }
        else
        {
            if (bestText) bestText.text = $"Best: {best}";
        }

        // stars
        ApplyStars(finalScore, maxScore);

        // celebration
        if (CelebrationBurst.Instance != null)
        {
            CelebrationBurst.Instance.Trigger(Vector3.zero);
            if (newBest) StartCoroutine(SecondBurst());
        }

        // auto-advance
        if (_autoCo != null) StopCoroutine(_autoCo);
        if (autoAdvanceEnabled && !string.IsNullOrEmpty(nextSceneName))
        {
            _autoCo = StartCoroutine(AutoAdvanceCountdown());
        }
        else if (nextCountdownRing)
        {
            nextCountdownRing.fillAmount = 0f;
        }
    }

    private void ApplyStars(int finalScore, int maxScore)
    {
        if (stars == null || stars.Length == 0) return;
        int starCount = 0;
        if (maxScore > 0)
        {
            float pct = Mathf.Clamp01((float)finalScore / maxScore);
            if (pct >= threeStarsPct) starCount = 3;
            else if (pct >= twoStarsPct) starCount = 2;
            else if (pct >= oneStarPct)  starCount = 1;
        }
        else
        {
            starCount = Mathf.Clamp(finalScore / 3, 0, 3);
        }

        // Guarda el mejor resultado de estrellas para la barra de progreso global
        string starKey  = $"stars_{minigameKey}";
        int    prevStars = PlayerPrefs.GetInt(starKey, 0);
        Debug.Log($"[ResultsScreen] minigameKey='{minigameKey}', score={finalScore}/{maxScore}, starCount={starCount}, prevStars={prevStars}");
        if (starCount > prevStars)
        {
            PlayerPrefs.SetInt(starKey, starCount);
            PlayerPrefs.Save();
            Debug.Log($"[ResultsScreen] Guardado '{starKey}' = {starCount}");
        }
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            bool earned = i < starCount;
            stars[i].enabled = !earned;          // oculta silueta cuando está ganada
            Transform yellowChild = stars[i].transform.Find("StarYellow");
            if (yellowChild != null)
            {
                yellowChild.gameObject.SetActive(earned);
                if (earned) yellowChild.localScale = Vector3.one * starYellowScale;
            }
        }
    }

    private IEnumerator SecondBurst()
    {
        yield return new WaitForSeconds(0.3f);
        if (CelebrationBurst.Instance != null)
            CelebrationBurst.Instance.Trigger(Vector3.zero);
    }

    private IEnumerator AutoAdvanceCountdown()
    {
        float t = 0f;
        while (t < autoAdvanceSeconds)
        {
            t += Time.deltaTime;
            if (nextCountdownRing) nextCountdownRing.fillAmount = Mathf.Clamp01(t / autoAdvanceSeconds);
            yield return null;
        }
        NextScene();
    }

    public void ReplayScene()
    {
        _advanced = true;
        if (_autoCo != null) StopCoroutine(_autoCo);
        if (panel) panel.SetActive(false);
        SceneTransition.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextScene()
    {
        if (_advanced) return;
        _advanced = true;
        if (_autoCo != null) StopCoroutine(_autoCo);
        string target = string.IsNullOrEmpty(nextSceneName) ? mainMenuScene : nextSceneName;
        SceneTransition.LoadScene(target);
    }

    public void BackToMenu()
    {
        _advanced = true;
        if (_autoCo != null) StopCoroutine(_autoCo);
        SceneTransition.LoadScene(mainMenuScene);
    }

    public void BackToLevelSelect()
    {
        _advanced = true;
        if (_autoCo != null) StopCoroutine(_autoCo);
        SceneTransition.LoadScene(levelSelectScene);
    }

    private static string PrettyName(string key)
    {
        switch (key)
        {
            case "color":   return "Color Jump";
            case "balloon": return "Balloon Pop";
            case "size":    return "Size Sort";
            case "mirror":  return "Mirror Word";
            default:        return key;
        }
    }
}
