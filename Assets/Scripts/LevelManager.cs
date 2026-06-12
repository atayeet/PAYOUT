using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Scene Management")]
    public string firstLevelName = "Level_1";
    
    // YENİ: Hangi Level'da olduğumuzu hafızada tutacak statik değişken (Ölünce hatırlamak için)
    private static string _savedLevelName = ""; 

    [Header("UI Transitions")]
    public Image fadeImage; 
    public float fadeDuration = 1f;

    private int _totalEnemies;
    private int _deadEnemies = 0;

    public int TotalEnemies => _totalEnemies;
    public int DeadEnemies => _deadEnemies;

    private PlayerController _player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _player = Object.FindAnyObjectByType<PlayerController>();

        // YENİ: Oyun ilk açıldıysa ve hafıza boşsa, firstLevelName'i baz al.
        if (string.IsNullOrEmpty(_savedLevelName))
        {
            _savedLevelName = firstLevelName;
        }

        // Önceden kaldığımız veya ilk defa başladığımız sahneyi yükle
        StartCoroutine(LoadLevelRoutine(_savedLevelName));
    }

    public void OnEnemyDied()
    {
        _deadEnemies++;
        if (_player != null)
        {
            float amount = 25f;
            if (DifficultyManager.Instance != null)
            {
                amount = DifficultyManager.Instance.GetFinisherStaminaOnKill();
            }
            _player.AddFinisherStamina(amount);
        }
    }

    public bool AreAllEnemiesDead()
    {
        return _deadEnemies >= _totalEnemies;
    }

    public void FinishLevelAndLoadNext(string nextLevelName)
    {
        // Sonraki level adını hafızaya kaydet ve yükle
        _savedLevelName = nextLevelName;
        StartCoroutine(LoadLevelRoutine(nextLevelName));
    }

    private IEnumerator LoadLevelRoutine(string targetLevelName)
    {
        // 1. Ekranı karart
        yield return StartCoroutine(FadeSequence(0f, 1f));

        if (_player != null && _player.GetComponent<Rigidbody2D>() != null)
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        // YENİ: Bölüm geçişi yaşanırken yerdeki Kan efektlerini ve havadaki mermileri kapat
        if (EffectPool.Instance != null) EffectPool.Instance.ClearAllEffects();
        if (BulletPool.Instance != null) BulletPool.Instance.ClearAllBullets();

        // 2. Halihazırda CoreScene dışında yüklenmiş olan ek sahneleri temizle
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != "CoreScene" && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        // 3. Yeni level'ı yükle
        yield return SceneManager.LoadSceneAsync(targetLevelName, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetLevelName));

        // 4. Yeni Level'daki verileri topla
        SetupNewLevel();

        // 5. Ekran Siyahlığını Geri Aç
        yield return StartCoroutine(FadeSequence(1f, 0f));
    }

    private void SetupNewLevel()
    {
        _deadEnemies = 0;
        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>();
        _totalEnemies = enemies.Length;

        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        if (spawnPoint != null && _player != null)
        {
            _player.transform.position = spawnPoint.transform.position;
            
            // Eğer isterseniz, canlanınca oyuncunun canını vb bu kısımda tekrar sıfırlayabilirsiniz.
        }
        else
        {
            Debug.LogWarning("Sahneye 'Respawn' etiketli bir SpawnPoint koymayı unuttunuz!");
        }

        // YENİ: Sahneye ait LevelBounds objesini bulup Cinemachine Confiner'a ata
        UpdateCameraBounds();
    }

    private void UpdateCameraBounds()
    {
        // Cinemachine kamerasını ve üzerindeki Confiner2D bileşenini buluyoruz
        var vcam = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            var confiner = vcam.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>();
            if (confiner != null)
            {
                // Yeni yüklenen sahnedeki LevelBounds bileşenini ara
                LevelBounds bounds = Object.FindAnyObjectByType<LevelBounds>();
                if (bounds != null && bounds.Collider != null)
                {
                    confiner.BoundingShape2D = bounds.Collider;
                    confiner.InvalidateBoundingShapeCache(); // Önbelleği temizleerek yeni sınırları etkinleştir
                }
                else
                {
                    // Fallback: Sahneye özel bir sınır tanımlanmamışsa, CoreScene'deki varsayılan sınırları aramayı dene
                    GameObject defaultBounds = GameObject.Find("A1");
                    if (defaultBounds != null)
                    {
                        confiner.BoundingShape2D = defaultBounds.GetComponent<Collider2D>();
                        confiner.InvalidateBoundingShapeCache();
                    }
                }
            }
        }
    }

    private IEnumerator FadeSequence(float startAlpha, float endAlpha)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color color = fadeImage.color;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
                fadeImage.color = color;
                yield return null;
            }

            color.a = endAlpha;
            fadeImage.color = color;
            
            if (endAlpha == 0f) fadeImage.gameObject.SetActive(false); 
        }
    }
}