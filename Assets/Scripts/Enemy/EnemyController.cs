using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerAwarenessController))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private float _speed;

    [SerializeField] private float _rotationSpeed;

    [SerializeField] private Vector2 _screenBorder = new Vector2(0.05f, 0.05f);

    private Rigidbody2D _rigidbody;
    private PlayerAwarenessController _playerAwarenessController;
    private Vector2 _targetDirection;
    private Animator _animator;
    private float _changeDirectionCooldown;
    private Camera _mainCamera;

    // Stun için gereken değişkenler
    private bool _isStunned = false; 
    private float _stunTimer = 0f;

    // Sınıfın üst kısmındaki değişkenlerin yanına orijinal katmanı tutacak değişkeni ekleyin
    private int _originalLayer;
    private int _stunnedLayer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _playerAwarenessController = GetComponent<PlayerAwarenessController>();
        _animator = GetComponent<Animator>();
        _targetDirection = transform.right;
        _mainCamera = Camera.main;

        // Başlangıçtaki katmanı kaydet ve geçilecek stun katmanını belirle
        _originalLayer = gameObject.layer;
        _stunnedLayer = LayerMask.NameToLayer("StunnedEntities");
    }

    private void FixedUpdate()
    {
        // Eğer sersemletilmişse normal hareket etmesini engelle ve geri sayımı başlat
        if (_isStunned)
        {
            _stunTimer -= Time.fixedDeltaTime;

            // Savrulmanın kademeli olarak yavaşlamasını sağlar (Sürtünme hissi)
            _rigidbody.linearVelocity = Vector2.Lerp(_rigidbody.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);

            if (_stunTimer <= 0f)
            {
                _isStunned = false; // Süre bitince normale dön

                // Sersemleme bittiğinde katmanı tekrar orijinal haline getir (oyuncu/düşman çarpışması açılsın)
                gameObject.layer = _originalLayer;

                SetAnimation(); // Animasyonu güncelle
            }
            return;
        }

        UpdateTargetDirection();
        RotateTowardsTarget();
        SetVelocity();
        SetAnimation();
    }

    private void SetAnimation()
    {
        bool isMoving = _targetDirection != Vector2.zero && _rigidbody.linearVelocity.sqrMagnitude > 0.01f;

        _animator.SetBool("isMoving", isMoving);

        // Stun bittiğinde Animator'ı "EnemyStunned" durumundan koparmak için
        // zorunlu olarak Idle veya Run animasyonuna yönlendir:
        if (!_isStunned)
        {
            if (isMoving)
            {
                _animator.Play("EnemyRun");
            }
            else
            {
                _animator.Play("EnemyIdle");
            }
        }
    }

    private void UpdateTargetDirection()
    {
        HandleRandomDirectionChange();
        HandlePlayerTargeting();
        HandleEnemyOffScreen();

        //else
        //{
        //    _targetDirection = Vector2.zero; // Player'� g�rm�yorsa hareket etmez
        //}
    }

    private void HandleRandomDirectionChange()
    {
        _changeDirectionCooldown -= Time.deltaTime;

        if (_changeDirectionCooldown <= 0f)
        {
            float angleChange = Random.Range(-90f, 90f); // Rastgele bir açı değişikliği
            Quaternion rotation = Quaternion.AngleAxis(angleChange, transform.forward);
            _targetDirection = rotation * _targetDirection;
            _changeDirectionCooldown = Random.Range(1f, 5f); // Bir sonraki rastgele yön değişikliği için süre
        }
    }

    private void HandlePlayerTargeting()
    {
        if (_playerAwarenessController.AwareOfPlayer)
        {
            _targetDirection = _playerAwarenessController.DirectionToPlayer;
        }
    }

    private void HandleEnemyOffScreen()
    {
        // Karakterin mevcut pozisyonunu Viewport (0 ile 1 aralığı) uzayına çevir
        Vector3 screenPosition = _mainCamera.WorldToViewportPoint(transform.position);

        // Pozisyonu Viewport içinde sınırla (Clamp)
        // Artık X için _screenBorder.x, Y için _screenBorder.y kullanıyoruz
        screenPosition.x = Mathf.Clamp(screenPosition.x, _screenBorder.x, 1f - _screenBorder.x);
        screenPosition.y = Mathf.Clamp(screenPosition.y, _screenBorder.y, 1f - _screenBorder.y);

        // Hesaplanıp sınırlandırılan yeni pozisyonu tekrar dünyaya çevirip Rigidbody'e uygula
        _rigidbody.position = _mainCamera.ViewportToWorldPoint(screenPosition);
    }

    private void RotateTowardsTarget()
    {
        //if (_targetDirection == Vector2.zero)
        //{
        //    return; // Hedef y�n� yoksa d�nmeye gerek yok
        //}

        Quaternion targetRotation = Quaternion.LookRotation(transform.forward, _targetDirection);
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        _rigidbody.SetRotation(rotation);
    }

    private void SetVelocity()
    {
        //if (_targetDirection == Vector2.zero)
        //{
        //    _rigidbody.linearVelocity = Vector2.zero; // Hedef y�n� yoksa dur
        //}
        //else
        //{
            _rigidbody.linearVelocity = transform.up * _speed; // Hedef y�n�nde hareket et
        //}
    }

    // Stun Metodu
    public void Stun(Vector2 knockbackDir, float knockbackForce)
    {
        if (!this.enabled || _isStunned) return; // Zaten ölü veya sersemlemişse işlemi geç

        _isStunned = true;
        _stunTimer = 1f; // 1 Saniye boyunca hiçbir şey yapamayacak

        // Sersemleme başladığında katmanı StunnedEntities yap (diğerleriyle çarpışmayı kapat)
        if (_stunnedLayer != -1)
        {
            gameObject.layer = _stunnedLayer;
        }

        if (_animator != null)
        {
            // İkinci parametre (-1) herhangi bir layer'da ara demek. 
            // Üçüncü parametre (0f) animasyonu 0. saniyeden itibaren başlatır.
            _animator.Play("EnemyStunned", -1, 0f);
        }

        // Fiziksel savrulma kuvveti (Mevcut hareketini iptal edip savrulma yönüne güç uygula)
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
    }

    // Ölüm Metodu
    public void Die()
    {
        // 1. Scripti devre dışı bırak, artık takip veya hareket işlemleri update edilmesin
        this.enabled = false;
        
        // 2. Düşmanın fiziksel hızını sıfırla ki kaymaya devam etmesin
        _rigidbody.linearVelocity = Vector2.zero;
        
        // Cesedin fizik motorunu meşgul etmesini engellemek için Rigidbody simülasyonunu kapatıyoruz
        _rigidbody.simulated = false;

        // 3. Üst üste hasar almaması için (veya içinden geçilebilmesi için) collider'ı kapat
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 4. 1, 2, 3 ve 4 numaralarından birini rastgele seç
        int randomDeathIndex = Random.Range(1, 5); 
        
        // 5. Animasyonu tetikle ("EnemyDeath1", "EnemyDeath2" vb.)
        _animator.Play("EnemyDeath" + randomDeathIndex);

        // 1. SpriteRenderer'ı al
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            // 2. Düşmanın layer'ını yerdeki eşyalar katmanına çek
            spriteRenderer.sortingLayerName = "Corpses"; 
        }        
    }
}
