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
            _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerController>() != null)
            _playerInRange = false;
    }

    private void Update()
    {
        // Manager var mı, tüm düşmanlar öldü mü, oyuncu bölgede mi?
        if (_playerInRange && LevelManager.Instance != null && LevelManager.Instance.AreAllEnemiesDead())
        {
            // O alanda "E" tuşuna basarsa bir sonraki sahneye geç
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                LevelManager.Instance.FinishLevelAndLoadNext(nextLevelName);
                _playerInRange = false; // Güvenlik önlemi (İki kez basmaması için)
            }
        }
    }
}