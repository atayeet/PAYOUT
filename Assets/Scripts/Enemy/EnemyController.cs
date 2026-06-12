using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerAwarenessController))]
public class EnemyController : MonoBehaviour, IDamageable
{
    private Rigidbody2D _rigidbody;
    private PlayerAwarenessController _playerAwarenessController;
    private Animator _animator;

    [Header("Combat Settings")]
    [SerializeField] private float _bulletKnockbackForce = 500f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _stunSound;
    [SerializeField] [Range(0f, 1f)] private float _stunSoundVolume = 1f;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup _sfxGroup;
    private AudioSource _audioSource;

    [Header("Weapon Dynamic Settings")]
    public bool HasWeapon = true;
    [SerializeField] private WeaponType _startingWeaponType = WeaponType.Pistol; // Başlangıç silah türü
    [SerializeField] private List<WeaponPrefabMap> _weaponPrefabs; // Silah Prefab Haritaları
    [SerializeField] private Transform _weaponDropPoint;

    private Weapon _weapon; // Kuşanılmış olan dinamik Weapon referansı

    private bool _isStunned = false;
    private float _stunTimer = 0f;
    private int _originalLayer;
    private int _stunnedLayer;
    public bool IsCurrentlyStunned => _isStunned;

    public bool IsBeingFinished { get; private set; } = false;
    private Collider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _playerAwarenessController = GetComponent<PlayerAwarenessController>();
        _animator = GetComponent<Animator>();

        _originalLayer = gameObject.layer;
        _stunnedLayer = LayerMask.NameToLayer("StunnedEntities");

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1.0f;
        if (_sfxGroup != null)
        {
            _audioSource.outputAudioMixerGroup = _sfxGroup;
        }

        // Başlangıçta silahı dinamik olarak kuşan
        if (HasWeapon)
        {
            EquipWeapon(_startingWeaponType);
        }
    }

    private void Update()
    {
        SetAnimation();
    }

    private void FixedUpdate()
    {
        if (_isStunned)
        {
            _stunTimer -= Time.fixedDeltaTime;
            _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);

            if (_stunTimer <= 0f)
            {
                _isStunned = false;
                _playerAwarenessController.IsStunned = false;
                gameObject.layer = _originalLayer;
                _playerAwarenessController.SetAgentEnabled(true);
            }
        }
    }

    private void SetAnimation()
    {
        Vector2 velocity = _playerAwarenessController.GetAgentVelocity();
        bool isMoving = !_isStunned && velocity.sqrMagnitude > 0.01f;

        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("isStunned", _isStunned);
        _animator.SetBool("hasWeapon", HasWeapon);
    }

    public void Stun(Vector2 knockbackDir, float knockbackForce)
    {
        if (!this.enabled || _isStunned) return;

        if (HasWeapon)
        {
            DropWeapon(knockbackDir);
        }

        _isStunned = true;
        _playerAwarenessController.IsStunned = true;
        _stunTimer = 1.5f;

        _playerAwarenessController.SetAgentEnabled(false);

        if (_stunnedLayer != -1) gameObject.layer = _stunnedLayer;
        if (_animator != null) _animator.SetBool("isStunned", true);

        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

        if (_audioSource != null && _stunSound != null)
        {
            _audioSource.PlayOneShot(_stunSound, _stunSoundVolume);
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "StunnedEntities";
    }

    private WeaponPrefabMap? GetWeaponMap(WeaponType type)
    {
        foreach (var map in _weaponPrefabs)
        {
            if (map.type == type) return map;
        }
        return null;
    }

    // Yeni Metot: Düşman için dinamik silah kuşanımı
    public void EquipWeapon(WeaponType type, int ammoCount = -1)
    {
        WeaponPrefabMap? map = GetWeaponMap(type);
        if (map.HasValue && map.Value.equippedPrefab != null && _weaponDropPoint != null)
        {
            GameObject weaponObj = Instantiate(map.Value.equippedPrefab, _weaponDropPoint);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            weaponObj.transform.localScale = Vector3.one;

            _weapon = weaponObj.GetComponent<Weapon>();
            HasWeapon = true;

            if (_weapon != null)
            {
                if (ammoCount >= 0) _weapon.CurrentAmmo = ammoCount;
                else _weapon.Reload();
            }

            if (_animator != null)
            {
                _animator.SetBool("hasWeapon", true);
            }
        }
    }

    // AI'ın yerden silah alabilmesi için mevcut imzayı koruyoruz
    public void EquipWeapon(GameObject droppedWeaponObj)
    {
        DropPistol dropScript = droppedWeaponObj.GetComponent<DropPistol>();
        if (dropScript != null)
        {
            EquipWeapon(dropScript.weaponType, dropScript.currentAmmo);
        }
        Destroy(droppedWeaponObj);
    }

    public void DropWeapon(Vector2 fallDirection)
    {
        if (!HasWeapon || _weapon == null) return;

        HasWeapon = false;

        WeaponPrefabMap? map = GetWeaponMap(_weapon.weaponType);
        if (map.HasValue && map.Value.dropPrefab != null && _weaponDropPoint != null)
        {
            GameObject droppedWeapon = Instantiate(map.Value.dropPrefab, _weaponDropPoint.position, transform.rotation);
            DropPistol dropScript = droppedWeapon.GetComponent<DropPistol>();

            if (dropScript != null)
            {
                dropScript.SetupDrop(_weapon.dropSprite, _weapon.weaponType, _weapon.CurrentAmmo);
                dropScript.Throw(-fallDirection + (Vector2)Random.insideUnitCircle * 0.5f, 4f);
            }
        }

        // Elindeki silahı yok et
        Destroy(_weapon.gameObject);
        _weapon = null;

        if (_animator != null)
        {
            _animator.SetBool("hasWeapon", false);
        }
    }

    public void FireWeapon()
    {
        if (HasWeapon && _weapon != null)
        {
            if (_weapon.TryFire())
            {
                if (_animator != null)
                {
                    _animator.SetTrigger("shoot");
                }
            }
        }
    }

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (!this.enabled) return;
        if (_isStunned) return;

        _playerAwarenessController.SetAgentEnabled(false);

        Vector2 enemyCenter = GetComponent<Collider2D>().bounds.center;
        Vector2 backPoint = enemyCenter;

        float backAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        Quaternion bloodRotation = Quaternion.Euler(0f, 0f, backAngle);

        EffectPool.Instance.SpawnEffect("BackBlood", backPoint, bloodRotation);
        _rigidbody.AddForce(hitDirection * _bulletKnockbackForce, ForceMode2D.Impulse);

        Die();
    }

    public void StartBeingFinished()
    {
        if (!this.enabled || !_isStunned) return;

        IsBeingFinished = true;
        _stunTimer = 999f;
        _rigidbody.linearVelocity = Vector2.zero;

        if (_animator != null)
        {
            _animator.SetBool("isBeingFinished", true);
        }
    }

    public void ExecuteFinisherDeath()
    {
        if (_animator != null)
        {
            _animator.SetBool("isBeingFinished", false);
        }

        Vector2 enemyCenter = GetComponent<Collider2D>().bounds.center;
        EffectPool.Instance.SpawnEffect("FrontBlood", enemyCenter, Quaternion.identity);

        Die(true);
    }

    public void Die(bool isFinisherDeath = false)
    {
        if (!this.enabled) return;

        if (HasWeapon)
        {
            DropWeapon(Random.insideUnitCircle.normalized);
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnEnemyDied();
        }

        this.enabled = false;

        if (_playerAwarenessController != null)
        {
            _playerAwarenessController.SetAgentEnabled(false);
            _playerAwarenessController.enabled = false;
        }

        if (_animator != null)
        {
            _animator.SetBool("isDead", true);
            _animator.SetBool("isStunned", false);
            _animator.SetBool("isMoving", false);

            if (!isFinisherDeath)
            {
                _animator.Play("EnemyDeath" + Random.Range(1, 5));
            }
            else
            {
                _animator.Play("EnemyKnocked" + Random.Range(1, 5));
            }
        }

        if (_collider != null) _collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses";
    }
}