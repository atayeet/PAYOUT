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

    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;
    private Animator _animator;

    public Weapon weapon; // Silah bileşeni
    public bool HasWeapon { get; private set; } = false; // İlk başta silah yok

    private Vector2 _movementInput;
    private Vector2 _currentVelocity;

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
        ReadInput();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        SetAnimation();

        // Hedeflenen h�z
        Vector2 targetVelocity = _movementInput * _speed;

        // Mevcut h�zdan hedeflenen h�za yumu�ak bir ge�i�
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _currentVelocity,
            _smoothTime);

        PreventPlayerGoingOffScreen();
    }

    private void ReadInput()
    {
        // Farenin ekrandaki pozisyonunu al (Yeni Input System gerektirir)
        if (Mouse.current != null)
        {
            // Ekran koordinat�n� d�nya koordinat�na �evir
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            // Karakterden fareye do�ru olan y�n vekt�r�n� hesapla
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            Vector2 lookDirection = mouseWorldPosition - transform.position;

            // Bak�� y�n�n�n a��s�n� (Atan2 ile) hesapla ve dereceye �evir
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // Rigidbody'nin d�n�� a��s�n� g�ncelle
            _rigidbody.rotation = angle + _rotationOffset;
        }

        // Silah varsa ve sol tıka basılıyorsa ateş et
        if (HasWeapon && Mouse.current.leftButton.isPressed)
        {
            weapon.TryFire();
        }

        // Sağ tık kontrolü: Silah atma veya alma
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (HasWeapon)
            {
                ThrowWeapon();
            }
            else
            {
                TryPickupWeapon();
            }
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

    

    private void SetAnimation()
    {
        bool isMoving = _movementInput != Vector2.zero;

        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("hasWeapon", HasWeapon); // Animator'a silah durumunu bildir
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

        // FrontBlood Effect (Pool)
        float frontAngle = Mathf.Atan2(-hitDirection.y, -hitDirection.x) * Mathf.Rad2Deg;
        EffectPool.Instance.SpawnEffect("FrontBlood", hitPoint, Quaternion.Euler(0, 0, frontAngle));

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

    // Seçili iken Inspector'da alma çemberini çizmek için
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _pickupRadius);
    }
}
