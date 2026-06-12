/*
2026-06-12 AI-Tag
This was created with the help of Assistant, a Unity Artificial Intelligence product.
*/
using System;
using UnityEditor;
using UnityEngine;

public class ProximityLamp : MonoBehaviour
{
    [Header("Visuals & Toggles")]
    [SerializeField] private GameObject _lampOnVisual;  // Açık lamba prefab/objesi (sarı)
    [SerializeField] private GameObject _lampOffVisual; // Kapalı lamba prefab/objesi (siyah)

    [Header("Detection Settings")]
    [SerializeField] private float _detectionRadius = 5f;       // Algılama yarıçapı
    [SerializeField] private float _turnOffDelay = 2f;         // Algılama bittikten kaç saniye sonra sönsün?
    [SerializeField] private LayerMask _targetLayers;          // Algılanacak katmanlar (Player ve Entities)
    [SerializeField] private LayerMask _obstacleLayers;        // Görüşü engelleyen katmanlar (Obstacle ve Door)

    private float _timer;
    private bool _isLit;

    private void Start()
    {
        // Başlangıçta lambayı kapalı hale getirelim
        SetLampState(false);
    }

    private void Update()
    {
        bool targetDetected = CheckForNearbyEntities();

        if (targetDetected)
        {
            // Yakında bir canlı tespit edilirse zamanlayıcıyı sıfırla ve lambayı yak
            _timer = _turnOffDelay;
            SetLampState(true);
        }
        else
        {
            // Kimse yoksa zamanlayıcıyı geri say
            if (_isLit)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    SetLampState(false);
                }
            }
        }
    }

    private bool CheckForNearbyEntities()
    {
        // Belirlenen yarıçaptaki tüm hedef collider'ları tespit et
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _targetLayers);

        foreach (Collider2D col in colliders)
        {
            bool isAlive = false;

            // 1. Oyuncu kontrolü
            if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null && !player.IsDead)
                {
                    isAlive = true;
                }
            }
            // 2. Düşman kontrolü
            else
            {
                EnemyController enemy = col.GetComponent<EnemyController>();
                // EnemyController aktifse (yani düşman yaşıyorsa)
                if (enemy != null && enemy.enabled)
                {
                    isAlive = true;
                }
            }

            // Eğer canlı bir karakter bulunduysa, aradaki görüş hattını (LOS) kontrol et
            if (isAlive)
            {
                // Lambanın merkezinden hedefin merkezine ışın gönder
                RaycastHit2D hit = Physics2D.Linecast(transform.position, col.transform.position, _obstacleLayers);

                // Eğer ışın bir engele çarpmadıysa görüş hattı temizdir
                if (hit.collider == null)
                {
                    return true; // En az bir canlı tespit edildi, diğerlerine bakmaya gerek yok
                }
            }
        }

        return false;
    }

    private void SetLampState(bool lit)
    {
        _isLit = lit;

        if (_lampOnVisual != null) _lampOnVisual.SetActive(lit);
        if (_lampOffVisual != null) _lampOffVisual.SetActive(!lit);
    }

    // Sahne tasarımını kolaylaştırmak için algılama alanını Editörde sarı bir çemberle gösterir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}
