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

    private void Awake()
    {
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
        _stunTimer = 1f;

        _playerAwarenessController.SetAgentEnabled(false); 
        
        if (_stunnedLayer != -1) gameObject.layer = _stunnedLayer;
        if (_animator != null) _animator.SetBool("isStunned", true);

        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
    }

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (!this.enabled) return;

        // Ön taraftaki kan efekti
        float frontAngle = Mathf.Atan2(-hitDirection.y, -hitDirection.x) * Mathf.Rad2Deg;
        EffectPool.Instance.SpawnEffect("FrontBlood", hitPoint, Quaternion.Euler(0, 0, frontAngle));

        _playerAwarenessController.SetAgentEnabled(false);

        // Arka taraftaki kan efekti
        Vector2 enemyCenter = GetComponent<Collider2D>().bounds.center;
        Vector2 backPoint = enemyCenter + (hitDirection * 0.4f);
        float backAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        EffectPool.Instance.SpawnEffect("BackBlood", backPoint, Quaternion.Euler(0, 0, backAngle));

        Die();
    }

    public void Die()
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
            _animator.Play("EnemyDeath" + Random.Range(1, 5));
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingLayerName = "Corpses"; 
    }
}
