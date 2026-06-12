using UnityEngine;
using System;
using System.Collections;

public enum WeaponType { Pistol, Shotgun, Rifle, Minigun }

// Yeni eklenen: Silah e�le�tirme yap�s�
[System.Serializable]
public struct WeaponPrefabMap
{
    public WeaponType type;
    public GameObject equippedPrefab; // Oyuncunun elindeki prefab (�rn: PistolWeapon)
    public GameObject dropPrefab;     // Yerdeki s�r�klenen fiziksel prefab (�rn: DropPistol)
}

public class Weapon : MonoBehaviour
{
    public WeaponType weaponType;
    public GameObject bullet;
    public Transform firePoint;
    public float fireForce;
    public LayerMask _obstacleLayer;

    [Header("Fire Rate Settings")]
    public float _fireRate = 0.2f;
    private float _nextFireTime = 0f;

    [Header("Ammo Settings")]
    public int maxAmmo = 15;
    private int _currentAmmo;

    // Yeni eklenen: Kaps�llenmi� mermi eri�imi (UI'� otomatik tetikler)
    public int CurrentAmmo
    {
        get { return _currentAmmo; }
        set
        {
            _currentAmmo = Mathf.Clamp(value, 0, maxAmmo);
            if (!_isEnemyWeapon)
            {
                UpdateAmmoUI();
            }
        }
    }

    [Header("Drop Settings")]
    public Sprite dropSprite; // Yere d��t���nde g�r�necek pixel-art

    public Animator currentAnimator;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup _sfxGroup;
    private AudioSource _audioSource;

    private bool _isEnemyWeapon;
    public static event Action<int, int> OnPlayerAmmoChanged;

    private void Awake()
    {
        _isEnemyWeapon = GetComponentInParent<EnemyController>() != null;
        _currentAmmo = maxAmmo;
        
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0.5f;
        if (_sfxGroup != null)
        {
            _audioSource.outputAudioMixerGroup = _sfxGroup;
        }
    }

    private void Start()
    {
        if (!_isEnemyWeapon)
        {
            UpdateAmmoUI();
        }
    }

    public bool TryFire()
    {
        if (_currentAmmo > 0 && Time.time >= _nextFireTime)
        {
            float currentFireRate = _fireRate;
            if (_isEnemyWeapon && DifficultyManager.Instance != null)
            {
                currentFireRate *= DifficultyManager.Instance.GetEnemyFireRateMultiplier();
            }
            _nextFireTime = Time.time + currentFireRate;

            if (weaponType == WeaponType.Rifle)
            {
                StartCoroutine(FireBurst());
            }
            else
            {
                ExecuteFireLogic();
            }

            return true;
        }
        return false;
    }

    private void ExecuteFireLogic()
    {
        if (_currentAmmo <= 0) return;

        if (currentAnimator != null) currentAnimator.SetTrigger("shoot");

        if (_audioSource != null && _fireSound != null)
        {
            _audioSource.PlayOneShot(_fireSound);
        }

        if (weaponType == WeaponType.Shotgun)
        {
            for (int i = 0; i < 6; i++)
            {
                float randomAngle = UnityEngine.Random.Range(-15f, 15f);
                FireSingleBullet(randomAngle);
            }
        }
        else
        {
            FireSingleBullet(0f);
        }

        ConsumeAmmo();
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_currentAmmo <= 0) break;

            if (currentAnimator != null) currentAnimator.SetTrigger("shoot");
            if (_audioSource != null && _fireSound != null)
            {
                _audioSource.PlayOneShot(_fireSound);
            }
            FireSingleBullet(0f);
            ConsumeAmmo();

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void FireSingleBullet(float angleOffset)
    {
        Vector2 origin = transform.position;
        Vector2 target = firePoint.position;
        Vector2 spawnPosition = target;

        RaycastHit2D hit = Physics2D.Linecast(origin, target, _obstacleLayer);
        if (hit.collider != null) spawnPosition = hit.point;

        Bullet newBullet = BulletPool.Instance.GetBullet(spawnPosition);
        newBullet.isEnemyBullet = _isEnemyWeapon;

        Quaternion rotation = firePoint.rotation * Quaternion.Euler(0, 0, angleOffset);
        newBullet.transform.rotation = rotation;

        newBullet.rb.AddForce(newBullet.transform.right * fireForce, ForceMode2D.Impulse);
    }

    private void ConsumeAmmo()
    {
        if (!_isEnemyWeapon)
        {
            _currentAmmo--;
            UpdateAmmoUI();
        }
    }

    public void Reload()
    {
        _currentAmmo = maxAmmo;
        if (!_isEnemyWeapon)
        {
            UpdateAmmoUI();
        }
    }

    private void UpdateAmmoUI()
    {
        OnPlayerAmmoChanged?.Invoke(_currentAmmo, maxAmmo);
    }
}