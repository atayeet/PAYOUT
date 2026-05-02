using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerAwarenessController))]
public class EnemyController : MonoBehaviour, IDamageable
{

    // Bileşenler
    private Rigidbody2D _rigidbody;
    private PlayerAwarenessController _playerAwarenessController;
    private Animator _animator;

    // Stun Değişkenleri
    private bool _isStunned = false; 
    private float _stunTimer = 0f;
    private int _originalLayer;
    private int _stunnedLayer;
    public bool IsCurrentlyStunned => _isStunned;

    // Finish Değişkenleri
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
                _playerAwarenessController.IsStunned = false; // Harekete izin ver
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
    }

    public void Stun(Vector2 knockbackDir, float knockbackForce)
    {
        if (!this.enabled || _isStunned) return; 

        _isStunned = true;
        _playerAwarenessController.IsStunned = true; // YZ Hareketini durdur
        _stunTimer = 1.5f;

        _playerAwarenessController.SetAgentEnabled(false); 
        
        if (_stunnedLayer != -1) gameObject.layer = _stunnedLayer;
        if (_animator != null) _animator.SetBool("isStunned", true);

        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
    }
    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (!this.enabled) return;

        _playerAwarenessController.SetAgentEnabled(false);

        Vector2 enemyCenter = GetComponent<Collider2D>().bounds.center;
        Vector2 backPoint = enemyCenter;

        float backAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;

        Quaternion bloodRotation = Quaternion.Euler(0f, 0f, backAngle);

        EffectPool.Instance.SpawnEffect("BackBlood", backPoint, bloodRotation);

        Die();
    }

    // Player, Space tuşuna basıp üzerine atladığında çağrılır
    public void StartBeingFinished()
    {
        if (!this.enabled || !_isStunned) return;

        IsBeingFinished = true;
        _stunTimer = 999f; // Kalkmasını engellemek için süreyi dondur/uzat
        _rigidbody.linearVelocity = Vector2.zero; // Hareketi durdur
        
        if (_animator != null)
        {
            _animator.SetBool("isBeingFinished", true); // Yerden doğrulma animasyonu
        }
    }

    // Player sol tıka bastığında çağrılır
    public void ExecuteFinisherDeath()
    {
        if (_animator != null)
        {
            // Doğrulma animasyonundan çık
            _animator.SetBool("isBeingFinished", false);
            
            // Artık trigger kullanmamıza gerek yok, animasyonu Die() içinde rastgele oynatacağız.
            // _animator.SetTrigger("finisherDie"); 
        }

        // Kan efekti çıkarma (Kafanın olduğu yere çıkartmak için ufak bir offset verilebilir)
        Vector2 enemyCenter = GetComponent<Collider2D>().bounds.center;
        EffectPool.Instance.SpawnEffect("FrontBlood", enemyCenter, Quaternion.identity);

        Die(true); // Ölüm fonksiyonunu çağır (Finisher ile öldüğünü belirt)
    }

    // Die fonksiyonuna isteğe bağlı bir parametre ekledik
    public void Die(bool isFinisherDeath = false)
    {
        this.enabled = false;
        
        if (_playerAwarenessController != null) 
        {
            _playerAwarenessController.SetAgentEnabled(false);
            _playerAwarenessController.enabled = false;
        }
        
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.simulated = false;

        if (_animator != null)
        {
            _animator.SetBool("isDead", true);
            _animator.SetBool("isStunned", false);
            _animator.SetBool("isMoving", false);

            if (!isFinisherDeath)
            {
                // Normal ölüm: EnemyDeath1, 2, 3 veya 4
                _animator.Play("EnemyDeath" + Random.Range(1, 5));
            }
            else
            {
                // İnfaz ölümü: EnemyKnocked1, 2, 3 veya 4
                _animator.Play("EnemyKnocked" + Random.Range(1, 5));
            }
        }

        
        if (_collider != null) _collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses"; 
    }
}
