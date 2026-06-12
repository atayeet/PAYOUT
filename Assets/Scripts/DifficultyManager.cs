using UnityEngine;

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

public class DifficultyManager : MonoBehaviour
{
    private static DifficultyManager _instance;

    public static DifficultyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<DifficultyManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DifficultyManager");
                    _instance = go.AddComponent<DifficultyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [SerializeField] private DifficultyLevel _defaultDifficulty = DifficultyLevel.Normal;

    private DifficultyLevel _currentDifficulty;

    public DifficultyLevel CurrentDifficulty
    {
        get => _currentDifficulty;
        set
        {
            _currentDifficulty = value;
            PlayerPrefs.SetInt("GameDifficulty", (int)_currentDifficulty);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDifficulty();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void LoadDifficulty()
    {
        _currentDifficulty = (DifficultyLevel)PlayerPrefs.GetInt("GameDifficulty", (int)_defaultDifficulty);
    }

    public float GetAlertDuration()
    {
        switch (_currentDifficulty)
        {
            case DifficultyLevel.Easy: return 1.0f;
            case DifficultyLevel.Normal: return 0.5f;
            case DifficultyLevel.Hard: return 0.15f;
            default: return 0.5f;
        }
    }

    public float GetEnemyFireRateMultiplier()
    {
        switch (_currentDifficulty)
        {
            case DifficultyLevel.Easy: return 1.6f;
            case DifficultyLevel.Normal: return 1.0f;
            case DifficultyLevel.Hard: return 0.75f;
            default: return 1.0f;
        }
    }

    public float GetFinisherRegenRate()
    {
        switch (_currentDifficulty)
        {
            case DifficultyLevel.Easy: return 15f;
            case DifficultyLevel.Normal: return 10f;
            case DifficultyLevel.Hard: return 6.67f;
            default: return 10f;
        }
    }

    public float GetFinisherStaminaOnKill()
    {
        switch (_currentDifficulty)
        {
            case DifficultyLevel.Easy: return 40f;
            case DifficultyLevel.Normal: return 25f;
            case DifficultyLevel.Hard: return 15f;
            default: return 25f;
        }
    }
}
