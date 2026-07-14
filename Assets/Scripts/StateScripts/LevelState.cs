using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelState : MonoBehaviour
{
    public static LevelState Instance { get; private set; }

    [Header("Level Catalog")]
    [Tooltip("Index in this list is the key used to select a LevelData.")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("Current Selection")]
    [SerializeField] private int selectedLevelIndex = -1;

    public int SelectedLevelIndex => selectedLevelIndex;
    public LevelData SelectedLevelData { get; private set; }

    public string levelSceneName = "Classroom";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public LevelData GetLevelData(int index)
    {
        if (index < 0 || index >= levels.Count)
            return null;

        return levels[index];
    }

    public bool SelectLevel(int index)
    {
        LevelData levelData = GetLevelData(index);
        if (levelData == null)
        {
            Debug.LogError($"LevelState: No LevelData found at index {index}.");
            return false;
        }

        selectedLevelIndex = index;
        SelectedLevelData = levelData;
        return true;
    }

    public void SelectLevelAndLoadScene(int index, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("LevelState: Scene name is empty.");
            return;
        }

        if (!SelectLevel(index))
            return;

        SceneManager.LoadScene(sceneName);
    }

    public void SelectLevelAndLoadDefaultScene(int index) {
        SelectLevelAndLoadScene(index, levelSceneName);
    }

    public void SelectLevelAndLoadScene(int index, int sceneBuildIndex)
    {
        if (!SelectLevel(index))
            return;

        SceneManager.LoadScene(sceneBuildIndex);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
