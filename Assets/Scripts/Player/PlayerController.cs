using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public bool IsDead { get; private set; }

    [SerializeField] private float _speed;
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private float _rotationOffset = 0f;

    // float yerine Vector2 kullanıyoruz. X sağ-sol, Y alt-üst sınırlarını belirleyecek.
    [SerializeField] private Vector2 _screenBorder = new Vector2(0.05f, 0.05f);

    [Header("Body Parts")]
    [SerializeField] private Transform _torsoTransform; // Ust govde transformu
    [SerializeField] private Animator _torsoAnimator;   // Ust govde animasyonlari (Atak vs)
    [SerializeField] private Transform _legsTransform;  // Alt govde transformu
    [SerializeField] private Animator _legsAnimator;    // Alt govde animasyonlari (Yurume vs)

    [Header("Weapon Drop/Pickup Settings")]
    [SerializeField] private GameObject _dropPistolPrefab;
    [SerializeField] private Transform _dropPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _pickupRadius = 1.5f;

    [Header("Finisher Settings")]
    [SerializeField] private float _finisherRange = 1.5f; 

    [Header("Punch Settings")]
    [SerializeField] private float _punchRange = 1f; 
    [SerializeField] private float _punchRadius = 0.5f; 
    [SerializeField] private float _punchCooldown = 0.25f; 
    [SerializeField] private float _punchKnockbackForce = 5f; 
    [SerializeField] private Transform _punchPoint;

    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;

    public Weapon weapon; 
    public bool HasWeapon { get; private set; } = false; 

    private Vector2 _movementInput;
    private Vector2 _currentVelocity;

    public bool IsPerformingFinisher { get; private set; } = false;
    private EnemyController _finisherTarget = null; 

    private float _nextPunchTime = 0f;
    private bool _isRightPunchNext = true; 
    
    private bool _isPunching = false; 
    private float _punchEndTime = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main; 
        
        // Weapon objesi Torso'nun altında olmalı
        weapon = GetComponentInChildren<Weapon>(true); 
    }

    private void Start()
    {
        if (!HasWeapon && weapon != null)
        {
            weapon.gameObject.SetActive(false); 
        }
    }

    private void Update()
    {
        if (IsDead) return;

        if (_isPunching && Time.time >= _punchEndTime) 
        {
            _isPunching = false;
        }

        ReadInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (IsDead || IsPerformingFinisher) return; 

        // Hedeflenen hiz
        Vector2 targetVelocity = _movementInput * _speed;

        // Mevcut hizdan hedeflenen hiza yumusak bir gecis
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _currentVelocity,
            _smoothTime);

        PreventPlayerGoingOffScreen();
        RotateLegs(); // Bacakları hareket yönüne çevir
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
            if (HasWeapon)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    weapon.TryFire();
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
        if (Mouse.current != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            
            // Torso farenin yönüne bakar
            Vector2 lookDirection = mouseWorldPosition - _torsoTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            
            _torsoTransform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);
        }
    }

    private void RotateLegs()
    {
        if (_movementInput.sqrMagnitude > 0.01f)
        {
            // Bacaklar hareket tuşlarının yönüne (WASD) bakar
            float angle = Mathf.Atan2(_movementInput.y, _movementInput.x) * Mathf.Rad2Deg;
            _legsTransform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);
        }
    }

    private void ExecutePunch()
    {
        _isPunching = true;
        _punchEndTime = Time.time + _punchCooldown; 

        // Artik SADECE Torso (Ust govde) uzerinde yumruk animasyonu calacak
        bool isMoving = _movementInput.sqrMagnitude > 0.01f;
        string animName = "";

        animName = _isRightPunchNext ? "PlayerTorsoIdlePunchRight" : "PlayerTorsoIdlePunchLeft";

        if(_torsoAnimator != null)
            _torsoAnimator.Play(animName, 0, 0f);

        _isRightPunchNext = !_isRightPunchNext;

        Vector2 punchOrigin = _punchPoint != null ? (Vector2)_punchPoint.position : (Vector2)transform.position + (Vector2)_torsoTransform.right * _punchRange;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(punchOrigin, _punchRadius);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController enemy = enemyCollider.GetComponent<EnemyController>();
            
            if (enemy != null && enemy.enabled)
            {
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemy.Stun(knockbackDir, _punchKnockbackForce);
            }
        }
    }

    private void UpdateAnimations()
    {
        bool isMoving = _movementInput != Vector2.zero;

        // Bacak Animasyonları
        if (_legsAnimator != null)
        {
            _legsAnimator.SetBool("isMoving", isMoving);
        }

        // Ust Govde Animasyonları
        if (_torsoAnimator != null)
        {
            _torsoAnimator.SetBool("isMoving", isMoving);
            _torsoAnimator.SetBool("hasWeapon", HasWeapon); 
        }
    }

    private void TryInitiateFinisher()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _finisherRange);
        foreach (Collider2D coll in colliders)
        {
            EnemyController enemy = coll.GetComponent<EnemyController>();
            
            if (enemy != null && enemy.enabled && enemy.IsCurrentlyStunned && !enemy.IsBeingFinished)
            {
                IsPerformingFinisher = true;
                _finisherTarget = enemy;
                _movementInput = Vector2.zero;
                
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;

                transform.position = enemy.transform.position - (enemy.transform.up * 0.2f);

                // Finisher durumunda Torso'yu da hedefe doğru çevir
                _torsoTransform.rotation = enemy.GetComponent<Rigidbody2D>().transform.rotation;
                
                if(_torsoAnimator != null) _torsoAnimator.SetBool("isFinisherReady", true); 
                if(_legsAnimator != null) _legsAnimator.SetBool("isFinisherReady", true); // Varsa alt kısım da diz çöksün
                
                _finisherTarget.StartBeingFinished();     
                break;
            }
        }
    }

    private void ExecuteFinisherAction()
    {
        if (_finisherTarget != null)
        {
            if(_torsoAnimator != null) _torsoAnimator.SetTrigger("executeFinisher");
            
            _finisherTarget.ExecuteFinisherDeath();
            
            IsPerformingFinisher = false;
            _finisherTarget = null;
            if(_torsoAnimator != null) _torsoAnimator.SetBool("isFinisherReady", false); 
            if(_legsAnimator != null) _legsAnimator.SetBool("isFinisherReady", false);
        }
    }

    private void PreventPlayerGoingOffScreen()
    {
        Vector3 screenPosition = _mainCamera.WorldToViewportPoint(transform.position);
        screenPosition.x = Mathf.Clamp(screenPosition.x, _screenBorder.x, 1f - _screenBorder.x);
        screenPosition.y = Mathf.Clamp(screenPosition.y, _screenBorder.y, 1f - _screenBorder.y);
        _rigidbody.position = _mainCamera.ViewportToWorldPoint(screenPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead) return;

        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();

        if (enemy != null && enemy.enabled && !enemy.IsCurrentlyStunned)
        {
            Vector2 hitPoint = collision.GetContact(0).point;
            Vector2 hitDirection = (transform.position - enemy.transform.position).normalized;
            Die(hitPoint, hitDirection);
        }
    }

    private void OnMove(InputValue inputValue)
    {
        _movementInput = inputValue.Get<Vector2>();
    }

    private void ThrowWeapon()
    {
        HasWeapon = false;
        weapon.gameObject.SetActive(false); 

        Vector2 dropOrigin = _dropPoint != null ? (Vector2)_dropPoint.position : (Vector2)transform.position; 
        GameObject droppedPistol = Instantiate(_dropPistolPrefab, dropOrigin, _torsoTransform.rotation);
        
        DropPistol dropScript = droppedPistol.GetComponent<DropPistol>();
        if (dropScript != null)
        {
            // Silahı Torso'nun yönüne doğru fırlat
            dropScript.Throw(_torsoTransform.right, _throwForce); 
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
                Destroy(droppedWeapon.gameObject);
                HasWeapon = true;
                weapon.gameObject.SetActive(true); 
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

        if (weapon != null) weapon.gameObject.SetActive(false);

        if (_torsoAnimator != null)
        {
            _torsoAnimator.SetBool("isDead", true);
            _torsoAnimator.SetBool("isMoving", false);
            _torsoAnimator.Play("PlayerDeath" + Random.Range(1, 5));
        }
        
        if (_legsAnimator != null)
        {
            // Bacakları muhtemelen ölümde gizlemek veya yatan bacak sprite'i koymak isteyeceksiniz.
            _legsAnimator.gameObject.SetActive(false); 
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        // Artık Torso içindeki sprite'ı almak gerekebilir
        if(spriteRenderer == null) spriteRenderer = _torsoTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses";

        this.enabled = false;
    }
}
