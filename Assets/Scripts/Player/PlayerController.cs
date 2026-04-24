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

    [Header("Weapon Drop/Pickup Settings")]
    [SerializeField] private GameObject _dropPistolPrefab;
    [SerializeField] private Transform _dropPoint;
    [SerializeField] private float _throwForce = 15f;
    [SerializeField] private float _pickupRadius = 1.5f;

    [Header("Finisher Settings")]
    [SerializeField] private float _finisherRange = 1.5f; // Düşmana ne kadar yakından infaz yapılabileceği

    [Header("Punch Settings")]
    [SerializeField] private float _punchRange = 1f; // Yumruk menzili
    [SerializeField] private float _punchRadius = 0.5f; // Yumruk genişliği (OverlapCircle için)
    [SerializeField] private float _punchCooldown = 0.25f; // İki yumruk arası bekleme
    [SerializeField] private float _punchKnockbackForce = 5f; // Düşmanı ittirme gücü
    [SerializeField] private Transform _punchPoint; // Yumruğun çıkacağı nokta (Empty GameObject)

    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;
    private Animator _animator;

    public Weapon weapon; // Silah bileşeni
    public bool HasWeapon { get; private set; } = false; // İlk başta silah yok

    private Vector2 _movementInput;
    private Vector2 _currentVelocity;

    public bool IsPerformingFinisher { get; private set; } = false;
    private EnemyController _finisherTarget = null; 

    private float _nextPunchTime = 0f;
    private bool _isRightPunchNext = true; 
    
    // Yumruk atılıyor durumunu takip için (Opsiyonel ama hareket ile birleştirirken iyi olur)
    private bool _isPunching = false; 
    private float _punchEndTime = 0f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main; // Fare pozisyonunu çevirmek için ana kamera
        _animator = GetComponent<Animator>();
        weapon = GetComponentInChildren<Weapon>(); // Silah bileşenini çocuklardan bul
    }
    private void Start()
    {
        if (!HasWeapon && weapon != null)
        {
            weapon.gameObject.SetActive(false); // Oyuncudaki silahı gizle (ilk başta)
        }
    }

    private void Update()
    {
        if (IsDead) return;

        // Punch bitiş zamanı kontrolü (Sadece state takibi için)
        if (_isPunching && Time.time >= _punchEndTime) 
        {
            _isPunching = false;
        }

        ReadInput();
    }

    private void FixedUpdate()
    {
        if (IsDead || IsPerformingFinisher) return; // Finisher yapıyorken hareketi durdur

        SetAnimation();

        // Hedeflenen hiz
        Vector2 targetVelocity = _movementInput * _speed;

        // Mevcut hizdan hedeflenen hiza yumusak bir gecis
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _currentVelocity,
            _smoothTime);

        PreventPlayerGoingOffScreen();
    }

    private void ReadInput()
    {
        // ------------- FİNİSHER DURUMUNDA İSE SADECE SOL TIK BEKLE -------------

        if (IsPerformingFinisher)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                ExecuteFinisherAction();
            }
            return; // Finisher yaparken başka tuşları ve dönmeyi engelle
        }

        // ------------- NORMAL GİRDİLER -------------

        // Silah varsa ve sol tıka basılıyorsa ateş et
        if (Mouse.current != null)
        {
            // Ekran koordinatini dunya koordinatina cevir
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            // Karakterden fareye dogru olan yon vektorunu hesapla
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            Vector2 lookDirection = mouseWorldPosition - transform.position;

            // Bakis yonunun acisini (Atan2 ile) hesapla ve dereceye cevir
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            _rigidbody.rotation = angle + _rotationOffset;
        }

        // Space tuşu ile Finisher Başlatma
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryInitiateFinisher();
        }

        // Ateş etme VEYA Yumruk atma
        if (Mouse.current != null)
        {
            if (HasWeapon)
            {
                 // EĞER silaha sahipken ateş etme
                if (Mouse.current.leftButton.isPressed)
                {
                    weapon.TryFire();
                }
            }
            else
            {
                // SİLAHSIZSA: Sol tıka tıklandığında veya basılı tutulduğunda yumruk at
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

    private void ExecutePunch()
    {
        _isPunching = true;
        _punchEndTime = Time.time + _punchCooldown; // Animasyonun yaklaşık bitiş süresi

        // Hareket ediyor mu kontrol et (RunPunch vs IdlePunch)
        bool isMoving = _movementInput.sqrMagnitude > 0.01f;
        string animName = "";

        if (isMoving)
        {
            animName = _isRightPunchNext ? "PlayerPunchRunRight" : "PlayerPunchRunLeft";
        }
        else
        {
            animName = _isRightPunchNext ? "PlayerPunchIdleRight" : "PlayerPunchIdleLeft";
        }

        // Animator'u Override Et (Trigger vs yerine direkt Animasyonu çal!)
        // Katman 0 (Base Layer) ve baştan oynaması için 0, 0 parametreleri
        _animator.Play(animName, 0, 0f);

        _isRightPunchNext = !_isRightPunchNext;

        // Düşmana hasar / stun kontrolü
        Vector2 punchOrigin = _punchPoint != null ? (Vector2)_punchPoint.position : (Vector2)transform.position + (Vector2)transform.right * _punchRange;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(punchOrigin, _punchRadius);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyController enemy = enemyCollider.GetComponent<EnemyController>();
            
            if (enemy != null && enemy.enabled)
            {
                // Düşmana yumrukla vurursak sadece Stun yiyor (Hasar yok)
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemy.Stun(knockbackDir, _punchKnockbackForce);
            }
        }
    }

    private void SetAnimation()
    {
        // Eğer Punch atıyorsak (Play() ile çaldık üstte), Standart Walk/Idle animasyonlarını ezmesin diye kontrol
        // (Çünkü biz Play dedikten hemensonra burası işleyip SetBool "isMoving" diyeecek.
        // EĞER Unity Animator'ünde "PlayerPunch..." animasyonlarında tekrar Walk'a ok çekmeyeceksense
        // Play methodu en temizi. Ancak Animator'de bu değerlerin yine de set edilmesinde sorun yok.
        
        bool isMoving = _movementInput != Vector2.zero;

        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("hasWeapon", HasWeapon); 
    }

    private void TryInitiateFinisher()
    {
        // Etraftaki objeleri tara
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _finisherRange);
        foreach (Collider2D coll in colliders)
        {
            EnemyController enemy = coll.GetComponent<EnemyController>();
            
            // Eğer düşmansa, yaşıyorsa, stun yemişse ve halihazırda infaz edilmiyorsa
            if (enemy != null && enemy.enabled && enemy.IsCurrentlyStunned && !enemy.IsBeingFinished)
            {
                // Finisher'ı başlat
                IsPerformingFinisher = true;
                _finisherTarget = enemy;
                _movementInput = Vector2.zero;
                
                // Oyuncuyu aniden durdur (dönme kuvvetlerini de sıfırla ki fare yüzünden kaymasın)
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;

                // 1. Oyuncunun konumunu yaklaşık olarak (kayma payıyla) düşmanın üzerine sabitle
                transform.position = enemy.transform.position - (enemy.transform.up * 0.2f);

                // 2. Oyuncunun yönünü düşmanın yönüyle BİREBİR AYNI yap (Vücutlar üst üste otursun)
                _rigidbody.rotation = enemy.GetComponent<Rigidbody2D>().rotation;
                
                // 3. Animatorlara state gönder
                _animator.SetBool("isFinisherReady", true); // Oyuncu diz çöker
                _finisherTarget.StartBeingFinished();     // Düşman hafif doğrulur
                break;
            }
        }
    }

    private void ExecuteFinisherAction()
    {
        if (_finisherTarget != null)
        {
            // 1. Animasyonu tetikle (3 Karelik boyun kırma animasyonu)
            _animator.SetTrigger("executeFinisher");
            
            // 2. Düşmanın ölümünü ve kan efektini tetikle
            _finisherTarget.ExecuteFinisherDeath();
            
            // 3. Finisher Modundan Çık
            IsPerformingFinisher = false;
            _finisherTarget = null;
            _animator.SetBool("isFinisherReady", false); // Tekrar ayağa kalk/normal state'e geç
        }
    }

    private void PreventPlayerGoingOffScreen()
    {
        // Karakterin mevcut pozisyonunu Viewport (0 ile 1 aralığı) uzayına çevir
        Vector3 screenPosition = _mainCamera.WorldToViewportPoint(transform.position);
        
        // Pozisyonu Viewport içinde sınırla (Clamp)
        // Artık X için _screenBorder.x, Y için _screenBorder.y kullanıyoruz
        screenPosition.x = Mathf.Clamp(screenPosition.x, _screenBorder.x, 1f - _screenBorder.x);
        screenPosition.y = Mathf.Clamp(screenPosition.y, _screenBorder.y, 1f - _screenBorder.y);

        // Hesaplanıp sınırlandırılan yeni pozisyonu tekrar dünyaya çevirip Rigidbody'e uygla
        _rigidbody.position = _mainCamera.ViewportToWorldPoint(screenPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead) return;

        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();

        // Düşman yaşıyor (enabled = true) ve stunlanmamış ise
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
        weapon.gameObject.SetActive(false); // Oyuncudaki silahı gizle

        // Yerdeki silah prefab'ını oluştur ve fırlat
        Vector2 dropOrigin = _dropPoint != null ? (Vector2)_dropPoint.position : (Vector2)transform.position; 
        GameObject droppedPistol = Instantiate(_dropPistolPrefab, dropOrigin, transform.rotation);
        
        // Karakterin baktığı yöne doğru fırlat
        DropPistol dropScript = droppedPistol.GetComponent<DropPistol>();
        if (dropScript != null)
        {
            dropScript.Throw(transform.right, _throwForce); // Eklediğimiz script fonksiyonu
        }
    }

    private void TryPickupWeapon()
    {
        // Karakterin etrafındaki silahları ara
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _pickupRadius);
        foreach (Collider2D coll in colliders)
        {
            DropPistol droppedWeapon = coll.GetComponent<DropPistol>();
            if (droppedWeapon != null)
            {
                // Silahı al
                Destroy(droppedWeapon.gameObject);
                HasWeapon = true;
                weapon.gameObject.SetActive(true); // Oyuncudaki silahı tekrar görünür yap
                break; // İlk bulduğumuz silahı alınca döngüden çık
            }
        }
    }

    private void Die(Vector2 hitPoint, Vector2 hitDirection)
    {
        IsDead = true;

        //// FrontBlood Effect (Pool)
        //float frontAngle = Mathf.Atan2(-hitDirection.y, -hitDirection.x) * Mathf.Rad2Deg;
        //EffectPool.Instance.SpawnEffect("FrontBlood", hitPoint, Quaternion.Euler(0, 0, frontAngle));

        // BackBlood Effect (Pool)
        Vector2 playerCenter = GetComponent<Collider2D>().bounds.center;
        Vector2 backPoint = playerCenter + (hitDirection * 0.4f);
        float backAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        EffectPool.Instance.SpawnEffect("BackBlood", backPoint, Quaternion.Euler(0, 0, backAngle));

        _movementInput = Vector2.zero;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;

        if (weapon != null) weapon.gameObject.SetActive(false);

        if (_animator != null)
        {
            _animator.SetBool("isDead", true);
            _animator.SetBool("isMoving", false);
            _animator.Play("PlayerDeath" + Random.Range(1, 5));
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses";

        this.enabled = false;
    }
}
