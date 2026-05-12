using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        // CoreScene'i yükler
        SceneManager.LoadScene("CoreScene");
    }

    public void OpenSettings()
    {
        Debug.Log("Ayarlar menüsü açýldý (Yapým aþamasýnda).");
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan çýkýlýyor...");
        Application.Quit();
    }
}