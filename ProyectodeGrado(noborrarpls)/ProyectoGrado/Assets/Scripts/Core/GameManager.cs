using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton global que maneja score, progreso de islas y PlayerPrefs.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progreso")]
    public int totalScore = 0;
    public int currentIsland = 1;
    public int unlockedIslands = 1;

    private const string KEY_SCORE = "totalScore";
    private const string KEY_UNLOCKED = "unlockedIslands";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }

    public void AddScore(int points)
    {
        totalScore += points;
        SaveProgress();
    }

    public void UnlockNextIsland()
    {
        if (unlockedIslands < 6)
        {
            unlockedIslands++;
            SaveProgress();
        }
    }

    public bool IsIslandUnlocked(int island)
    {
        return island <= unlockedIslands;
    }

    public void LoadIsland(int island)
    {
        if (!IsIslandUnlocked(island)) return;
        currentIsland = island;
        SceneManager.LoadScene($"Island{island}");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SaveProgress()
    {
        PlayerPrefs.SetInt(KEY_SCORE, totalScore);
        PlayerPrefs.SetInt(KEY_UNLOCKED, unlockedIslands);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        totalScore = PlayerPrefs.GetInt(KEY_SCORE, 0);
        unlockedIslands = PlayerPrefs.GetInt(KEY_UNLOCKED, 1);
    }

    public void ResetProgress()
    {
        totalScore = 0;
        unlockedIslands = 1;
        SaveProgress();
    }
}
