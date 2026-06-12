using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DifficultySettingController : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _difficultyText;
    [SerializeField] private UnityEngine.UI.Button _changeDifficultyButton;

    private void Start()
    {
        if (_changeDifficultyButton != null)
        {
            _changeDifficultyButton.onClick.AddListener(CycleDifficulty);
        }
        UpdateDifficultyText();
    }

    private void OnEnable()
    {
        UpdateDifficultyText();
    }

    public void CycleDifficulty()
    {
        if (DifficultyManager.Instance == null) return;

        DifficultyLevel current = DifficultyManager.Instance.CurrentDifficulty;
        DifficultyLevel next;

        switch (current)
        {
            case DifficultyLevel.Easy:
                next = DifficultyLevel.Normal;
                break;
            case DifficultyLevel.Normal:
                next = DifficultyLevel.Hard;
                break;
            case DifficultyLevel.Hard:
                next = DifficultyLevel.Easy;
                break;
            default:
                next = DifficultyLevel.Normal;
                break;
        }

        DifficultyManager.Instance.CurrentDifficulty = next;
        UpdateDifficultyText();
    }

    private void UpdateDifficultyText()
    {
        if (DifficultyManager.Instance == null) return;

        if (_difficultyText != null)
        {
            DifficultyLevel current = DifficultyManager.Instance.CurrentDifficulty;
            switch (current)
            {
                case DifficultyLevel.Easy:
                    _difficultyText.text = "DIFFICULTY: EASY";
                    _difficultyText.color = Color.green;
                    break;
                case DifficultyLevel.Normal:
                    _difficultyText.text = "DIFFICULTY: NORMAL";
                    _difficultyText.color = Color.yellow;
                    break;
                case DifficultyLevel.Hard:
                    _difficultyText.text = "DIFFICULTY: HARD";
                    _difficultyText.color = Color.red;
                    break;
            }
        }
    }
}
