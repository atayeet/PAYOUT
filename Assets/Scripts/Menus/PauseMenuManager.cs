using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Projenizdeki yeni Input System için gerekli

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [SerializeField] private GameObject _pauseMenuUI;
    [SerializeField] private GameObject _restartTextUI;
    [SerializeField] private GameObject _settingsPanel;
    
    private PlayerController _playerController;

    private void Start()
    {
        // Oyun başladığında Pause menüsünün gizli olduğundan emin olun
        if (_pauseMenuUI != null) _pauseMenuUI.SetActive(false);
        
        // Oyun başında Restart yazısını da gizle
        if (_restartTextUI != null) _restartTextUI.SetActive(false);

        Time.timeScale = 1f;
        GameIsPaused = false;

        // Sahnede bulunan PlayerController'ı bularak referansını al
        _playerController = Object.FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        ReadInputOnPause();
        CheckRestartInput();
    }

    private void ReadInputOnPause()
    {
        // ESC tuşuna basılıp basılmadığını kontrol et
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Oyuncu öldüyse pause menüsünün açılmasını engelle
            if (_playerController != null && _playerController.IsDead) return;

            if (_settingsPanel != null && _settingsPanel.activeSelf)
            {
                _settingsPanel.SetActive(false);
                return;
            }

            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void CheckRestartInput()
    {
        if (_playerController != null && _playerController.IsDead)
        {
            if (_restartTextUI != null && !_restartTextUI.activeSelf)
            {
                _restartTextUI.SetActive(true);
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f; 
                GameIsPaused = false;
                
                // DEĞİŞEN KISIM: Additive sistem olduğu için, 
                // doğrudan "CoreScene" i baştan yükleyip her şeyi sıfırlıyoruz.
                // LevelManager statik tuttuğu isim sayesinde oyuncuyu doğru haritada spawn edecek.
                SceneManager.LoadScene("CoreScene"); 
            }
        }
    }

    public void Resume()
    {
        if (_pauseMenuUI != null) _pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;  
        GameIsPaused = false;

        // Fareyi oyuna geri gizle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Pause()
    {
        if (_pauseMenuUI != null) _pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;  
        GameIsPaused = true;

        // Fareyi görünür ve serbest yap (Menü kullanımı için)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;

        // Ana menüye dönerken fare imlecini serbest bırak
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("MainMenu"); 
    }
}