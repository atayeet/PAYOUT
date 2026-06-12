using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour, IDamageable
{
    public bool IsDead { get; private set; }

    [SerializeField] private float _speed;
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private float _rotationOffset = 0f;
    [SerializeField] private Vector2 _screenBorder = new Vector2(0.05f, 0.05f);

    [Header("Body Parts")]
    [SerializeField] private Transform _torsoTransform;
    [SerializeField] private Animator _torsoAnimator;
    [SerializeField] private Transform _legsTransform;
    [SerializeField] private Animator _legsAnimator;

    [Header("Weapon Dynamic Settings")]
    [SerializeField] private List<WeaponPrefabMap> _weaponPrefabs; // Tüm silahların prefab listesi
    [SerializeField] private Transform _dropPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _pickupRadius = 1.5f;

    [Header("Finisher Settings")]
    [SerializeField] private float _finisherRange = 1.5f;
    [SerializeField] private float _maxFinisherStamina = 100f;
    private float _finisherStamina = 100f;

    public float FinisherStaminaRatio => _finisherStamina / _maxFinisherStamina;
    public float CurrentFinisherStamina => _finisherStamina;

    [Header("Punch Settings")]
    [SerializeField] private float _punchRange = 1f;
    [SerializeField] private float _punchRadius = 0.5f;
    [SerializeField] private float _punchCooldown = 0.25f;
    [SerializeField] private float _punchKnockbackForce = 5f;
    [SerializeField] private Transform _punchPoint;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _punchHitSound;
    [SerializeField] private AudioClip _punchWhooshSound;
    [SerializeField] [Range(0f, 1f)] private float _punchWhooshVolume = 0.8f;
    [SerializeField] private AudioClip _finisherBoneBreakSound;
    [SerializeField] [Range(0f, 1f)] private float _finisherVolume = 1f;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup _sfxGroup;
    private AudioSource _audioSource;

    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;

    public Weapon weapon { get; private set; } // Set işlemi private yapıldı
    public bool HasWeapon { get; private set; } = false;

    private Vector2 _movementInput;
    private Vector2 _currentVelocity;

    public bool IsPerformingFinisher { get; private set; } = false;
    private EnemyController _finisherTarget = null;

    private float _nextPunchTime = 0f;
    private bool _isRightPunchNext = true;
    private bool _isPunching = false;
    private float _punchEndTime = 0f;

    private VirtualCursor _virtualCursor;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;

        // Eğer sahne başlarken Torso altında halihazırda bir silah varsa onu otomatik eşle
        weapon = GetComponentInChildren<Weapon>(true);
        if (weapon != null)
        {
            HasWeapon = true;
            weapon.gameObject.SetActive(true);
        }
        else
        {
            HasWeapon = false;
        }

        _virtualCursor = Object.FindAnyObjectByType<VirtualCursor>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        if (_sfxGroup != null)
        {
            _audioSource.outputAudioMixerGroup = _sfxGroup;
        }
    }

    private void Start()
    {
        // Başlangıçta silah yoksa boş başlatıyoruz
    }

    private void Update()
    {
        if (IsDead) return;

        if (_isPunching && Time.time >= _punchEndTime)
        {
            _isPunching = false;
        }

        // Finisher stamina regeneration
        if (!IsPerformingFinisher)
        {
            float regenRate = 10f;
            if (DifficultyManager.Instance != null)
            {
                regenRate = DifficultyManager.Instance.GetFinisherRegenRate();
            }
            _finisherStamina = Mathf.Min(_finisherStamina + regenRate * Time.deltaTime, _maxFinisherStamina);
        }

        ReadInput();
        UpdateAnimations();
    }

    public void AddFinisherStamina(float amount)
    {
        _finisherStamina = Mathf.Min(_finisherStamina + amount, _maxFinisherStamina);
    }

    private void FixedUpdate()
    {
        if (IsDead || IsPerformingFinisher) return;

        Vector2 targetVelocity = _movementInput * _speed;
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _currentVelocity,
            _smoothTime);

        PreventPlayerGoingOffScreen();
        RotateLegs();
    }

    private void ReadInput()
    {
        if (IsPerformingFinisher)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ExecuteFinisherAction();
            }
            return;
        }

        RotateTorsoTowardsMouse();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryInitiateFinisher();
        }

        if (Mouse.current != null)
        {
            if (HasWeapon && weapon != null)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    if (weapon.TryFire())
                    {
                        if (_torsoAnimator != null)
                        {
                            _torsoAnimator.SetTrigger("shoot");
                        }
                    }
                }
            }
            else
            {
                if (Mouse.current.leftButton.isPressed && Time.time >= _nextPunchTime)
                {
                    ExecutePunch();
                    _nextPunchTime = Time.time + _punchCooldown;
                }
            }
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (HasWeapon) ThrowWeapon();
            else TryPickupWeapon();
        }
    }

    private void RotateTorsoTowardsMouse()
    {
        if (_virtualCursor != null)
        {
            Vector2 lookDirection = _virtualCursor.transform.position - _torsoTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            _torsoTransform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);
        }
    }

    private void RotateLegs()
    {
        if (_movementInput.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(_movementInput.y, _movementInput.x) * Mathf.Rad2Deg;
            _legsTransform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);
        }
    }

    private void ExecutePunch()
    {
        _isPunching = true;
        _punchEndTime = Time.time + _punchCooldown;

        string animName = _isRightPunchNext ? "PlayerTorsoIdlePunchRight" : "PlayerTorsoIdlePunchLeft";

        if (_torsoAnimator != null)
            _torsoAnimator.Play(animName, 0, 0f);

        _isRightPunchNext = !_isRightPunchNext;

        Vector2 punchOrigin = _punchPoint != null ? (Vector2)_punchPoint.position : (Vector2)transform.position + (Vector2)_torsoTransform.right * _punchRange;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(punchOrigin, _punchRadius);
        bool hitEnemy = false;
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController enemy = enemyCollider.GetComponent<EnemyController>();
            if (enemy != null && enemy.enabled && !enemy.IsCurrentlyStunned)
            {
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemy.Stun(knockbackDir, _punchKnockbackForce);
                hitEnemy = true;
            }
        }

        if (_audioSource != null)
        {
            if (hitEnemy)
            {
                // Hit sound will play on the enemy's AudioSource inside enemy.Stun()
            }
            else
            {
                if (_punchWhooshSound != null)
                    _audioSource.PlayOneShot(_punchWhooshSound, _punchWhooshVolume);
            }
        }
    }

    private void UpdateAnimations()
    {
        bool isMoving = _movementInput != Vector2.zero;

        if (_legsAnimator != null)
        {
            _legsAnimator.SetBool("isMoving", isMoving);
        }

        if (_torsoAnimator != null)
        {
            _torsoAnimator.SetBool("isMoving", isMoving);
            _torsoAnimator.SetBool("hasWeapon", HasWeapon);
        }
    }

    private void TryInitiateFinisher()
    {
        if (_finisherStamina < _maxFinisherStamina) return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _finisherRange);
        foreach (Collider2D coll in colliders)
        {
            EnemyController enemy = coll.GetComponent<EnemyController>();
            if (enemy != null && enemy.enabled && enemy.IsCurrentlyStunned && !enemy.IsBeingFinished)
            {
                IsPerformingFinisher = true;
                _finisherTarget = enemy;
                _finisherStamina = 0f; // Reset stamina to 0 on use!
                _movementInput = Vector2.zero;

                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;

                transform.position = enemy.transform.position - (enemy.transform.up * 0.2f);
                _torsoTransform.rotation = enemy.GetComponent<Rigidbody2D>().transform.rotation;

                if (_torsoAnimator != null) _torsoAnimator.SetBool("isFinisherReady", true);
                if (_legsAnimator != null) _legsAnimator.SetBool("isFinisherReady", true);

                _finisherTarget.StartBeingFinished();
                break;
            }
        }
    }

    private void ExecuteFinisherAction()
    {
        if (_finisherTarget != null)
        {
            if (_torsoAnimator != null) _torsoAnimator.SetTrigger("executeFinisher");
            _finisherTarget.ExecuteFinisherDeath();

            if (_audioSource != null && _finisherBoneBreakSound != null)
            {
                _audioSource.PlayOneShot(_finisherBoneBreakSound, _finisherVolume);
            }

            IsPerformingFinisher = false;
            _finisherTarget = null;
            if (_torsoAnimator != null) _torsoAnimator.SetBool("isFinisherReady", false);
            if (_legsAnimator != null) _legsAnimator.SetBool("isFinisherReady", false);
        }
    }

    private void PreventPlayerGoingOffScreen()
    {
        // Eğer oyuncu shift'e basıp etrafa bakıyorsa (görüş açısını esnetiyorsa), ekran dışına çıkma sınırlandırmasını geçici olarak devre dışı bırakıyoruz.
        if (_virtualCursor != null && _virtualCursor.IsLookingAhead) return;

        Vector3 screenPosition = _mainCamera.WorldToViewportPoint(transform.position);
        screenPosition.x = Mathf.Clamp(screenPosition.x, _screenBorder.x, 1f - _screenBorder.x);
        screenPosition.y = Mathf.Clamp(screenPosition.y, _screenBorder.y, 1f - _screenBorder.y);
        _rigidbody.position = _mainCamera.ViewportToWorldPoint(screenPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision) { }

    private void OnMove(InputValue inputValue)
    {
        _movementInput = inputValue.Get<Vector2>();
    }

    // Yeni Yardımcı Metot: Map listesinden istenen tipe ait prefaba ulaşır
    private WeaponPrefabMap? GetWeaponMap(WeaponType type)
    {
        foreach (var map in _weaponPrefabs)
        {
            if (map.type == type) return map;
        }
        return null;
    }

    // Yeni Metot: Dinamik silah kuşanma
    public void EquipWeapon(WeaponType type, int ammoCount = -1)
    {
        if (HasWeapon && weapon != null)
        {
            ThrowWeapon(); // Elimizde silah varsa yere at
        }

        WeaponPrefabMap? map = GetWeaponMap(type);
        if (map.HasValue && map.Value.equippedPrefab != null)
        {
            // Silahı Torso'nun altına instantiate et
            GameObject weaponObj = Instantiate(map.Value.equippedPrefab, _torsoTransform);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.transform.localScale = Vector3.one;

            weapon = weaponObj.GetComponent<Weapon>();
            HasWeapon = true;

            if (weapon != null)
            {
                if (ammoCount >= 0)
                {
                    weapon.CurrentAmmo = ammoCount; // Kalan mermiyi aktar
                }
                else
                {
                    weapon.Reload(); // Mermiyi doldur
                }
            }

            if (_torsoAnimator != null)
            {
                _torsoAnimator.SetBool("hasWeapon", true);
            }
        }
    }

    private void ThrowWeapon()
    {
        if (!HasWeapon || weapon == null) return;

        HasWeapon = false;

        WeaponPrefabMap? map = GetWeaponMap(weapon.weaponType);
        if (map.HasValue && map.Value.dropPrefab != null)
        {
            Vector2 dropOrigin = _dropPoint != null ? (Vector2)_dropPoint.position : (Vector2)transform.position;
            GameObject droppedWeaponObj = Instantiate(map.Value.dropPrefab, dropOrigin, _torsoTransform.rotation);
            DropPistol dropScript = droppedWeaponObj.GetComponent<DropPistol>();

            if (dropScript != null)
            {
                // Silahın sprite'ını ve tipini, ayrıca kalan mermisini yerdeki silaha aktar
                dropScript.SetupDrop(weapon.dropSprite, weapon.weaponType, weapon.CurrentAmmo);
                dropScript.Throw(_torsoTransform.right, _throwForce);
            }
        }

        // Elimizdeki silahı yok et
        Destroy(weapon.gameObject);
        weapon = null;

        if (_torsoAnimator != null)
        {
            _torsoAnimator.SetBool("hasWeapon", false);
        }
    }

    private void TryPickupWeapon()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _pickupRadius);
        foreach (Collider2D coll in colliders)
        {
            DropPistol droppedWeapon = coll.GetComponent<DropPistol>();
            if (droppedWeapon != null)
            {
                // Yerdeki silahın tipine ve mermisine göre yeni silahı kuşan!
                EquipWeapon(droppedWeapon.weaponType, droppedWeapon.currentAmmo);
                Destroy(droppedWeapon.gameObject);
                break;
            }
        }
    }

    private void Die(Vector2 hitPoint, Vector2 hitDirection)
    {
        IsDead = true;

        Vector2 playerCenter = GetComponent<Collider2D>().bounds.center;
        Vector2 backPoint = playerCenter + (hitDirection * 0.4f);
        float backAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        EffectPool.Instance.SpawnEffect("BackBlood", backPoint, Quaternion.Euler(0, 0, backAngle));

        _movementInput = Vector2.zero;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;

        if (weapon != null)
        {
            Destroy(weapon.gameObject); // Ölünce silahı tamamen yok et
            weapon = null;
        }

        if (_torsoAnimator != null)
        {
            _torsoAnimator.SetBool("isDead", true);
            _torsoAnimator.SetBool("isMoving", false);
            _torsoAnimator.Play("PlayerDeath" + Random.Range(1, 5));
        }

        if (_legsAnimator != null)
        {
            _legsAnimator.gameObject.SetActive(false);
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = _torsoTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses";

        this.enabled = false;
    }

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;
        Die(hitPoint, hitDirection);
    }
}