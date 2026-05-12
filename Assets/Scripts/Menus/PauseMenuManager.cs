using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Projenizdeki yeni Input System için gerekli

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [SerializeField] private GameObject _pauseMenuUI;
    [SerializeField] private GameObject _restartTextUI;
    
    private PlayerController _playerController;

    private void Start()
    {
        // Oyun baþladýðýnda Pause menüsünün gizli olduðundan emin olun
        if (_pauseMenuUI != null) _pauseMenuUI.SetActive(false);
        
        // Oyun baþýnda Restart yazýsýný da gizle
        if (_restartTextUI != null) _restartTextUI.SetActive(false);

        Time.timeScale = 1f;
        GameIsPaused = false;

        // Sahnede bulunan PlayerController'ý bularak referansýný al
        _playerController = Object.FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        ReadInputOnPause();
        CheckRestartInput();
    }

    private void ReadInputOnPause()
    {
        // ESC tuþuna basýlýp basýlmadýðýný kontrol et
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Oyuncu öldüyse pause menüsünün açýlmasýný engelle
            if (_playerController != null && _playerController.IsDead) return;

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
                
                // DEÐÝÞEN KISIM: Additive sistem olduðu için, 
                // doðrudan "CoreScene" i baþtan yükleyip her þeyi sýfýrlýyoruz.
                // LevelManager statik tuttuðu isim sayesinde oyuncuyu doðru haritada spawn edecek.
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

        // Fareyi görünür ve serbest yap (Menü kullanýmý için)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;

        // Ana menüye dönerken fare imlecini serbest býrak
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("MainMenu"); 
    }
}