using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private LevelManager levelManager;
    private LevelHUD levelHUD;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        levelHUD = FindAnyObjectByType<LevelHUD>();

        if (levelManager == null)
        {
            Debug.LogError("[GameManager] LevelManager not found in scene");
            return;
        }

        BindEvents();
        levelManager.StartLevel();
    }

    private void BindEvents()
    {
        levelManager.OnCutUpdated += HandleCutUpdated;
        levelManager.OnScoreUpdated += HandleScoreUpdated;
        levelManager.OnLevelComplete += HandleLevelComplete;
    }

    private void HandleCutUpdated(int currentCut, int totalCuts)
    {
        levelHUD?.UpdateCutCounter(currentCut, totalCuts);
    }

    private void HandleScoreUpdated(int totalScore)
    {
        levelHUD?.UpdateScore(totalScore);
    }

    private void HandleLevelComplete(int finalScore)
    {
        Debug.Log($"[GameManager] Level Complete! Final Score: {finalScore}");
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.OnCutUpdated -= HandleCutUpdated;
            levelManager.OnScoreUpdated -= HandleScoreUpdated;
            levelManager.OnLevelComplete -= HandleLevelComplete;
        }

        if (Instance == this)
            Instance = null;
    }
}
