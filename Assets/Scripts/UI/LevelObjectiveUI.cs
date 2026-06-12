using UnityEngine;
using TMPro;

public class LevelObjectiveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _enemyCountText;
    [SerializeField] private GameObject _exitNotification;

    private void Update()
    {
        if (LevelManager.Instance == null) return;

        int total = LevelManager.Instance.TotalEnemies;
        int dead = LevelManager.Instance.DeadEnemies;

        // UI 1: Enemy counter showing remaining/total enemies
        if (_enemyCountText != null)
        {
            _enemyCountText.text = $"Enemies: {dead} / {total}";
        }

        // UI 2: Exit notification showing that stairs can now be used
        if (_exitNotification != null)
        {
            bool allDead = LevelManager.Instance.AreAllEnemiesDead();
            // Activate the notification when all enemies are dead
            _exitNotification.SetActive(allDead && total > 0);
        }
    }
}
