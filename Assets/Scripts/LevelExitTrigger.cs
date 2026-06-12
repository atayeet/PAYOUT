using UnityEngine;
using UnityEngine.InputSystem;

public class LevelExitTrigger : MonoBehaviour
{
    [Tooltip("Geçilecek sonraki sahnenin tam adı (Örn: Level_2)")]
    public string nextLevelName;
    private bool _playerInRange = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>() != null)
        {
            _playerInRange = true;
            CheckExit();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>() != null)
            _playerInRange = false;
    }

    private void Update()
    {
        CheckExit();
    }

    private void CheckExit()
    {
        if (_playerInRange && LevelManager.Instance != null && LevelManager.Instance.AreAllEnemiesDead())
        {
            LevelManager.Instance.FinishLevelAndLoadNext(nextLevelName);
            _playerInRange = false; // Güvenlik önlemi (İki kez basmaması için)
        }
    }
}