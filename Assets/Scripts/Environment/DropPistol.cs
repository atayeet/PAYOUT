using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DropPistol : MonoBehaviour
{
    [SerializeField] private float _spinSpeed = 1000f;
    [SerializeField] private float _stunKnockbackForce = 10f;

    private Rigidbody2D _rb;
    private int _originalLayer;
    private int _thrownLayer;
    public WeaponType weaponType;
    private SpriteRenderer _spriteRenderer;

    // Yeni eklenen: Silahýn kalan mermisini hafýzada tutar
    public int currentAmmo = -1;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearDamping = 3f;
        _rb.angularDamping = 2f;

        _originalLayer = gameObject.layer;
        _thrownLayer = LayerMask.NameToLayer("ThrownItem");

        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Throw(Vector2 direction, float force)
    {
        if (_thrownLayer != -1)
        {
            gameObject.layer = _thrownLayer;
        }

        _rb.AddForce(direction.normalized * force * 1.5f, ForceMode2D.Impulse);
        _rb.AddTorque(_spinSpeed * (Random.value > 0.5f ? 1f : -1f));
    }

    private void FixedUpdate()
    {
        if (_rb.linearVelocity.magnitude < 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            gameObject.layer = _originalLayer;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_rb.linearVelocity.magnitude > 0f)
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                Vector2 knockbackDir = _rb.linearVelocity.normalized;
                enemy.Stun(knockbackDir, _stunKnockbackForce);
            }
        }
    }

    // Güncellendi: Artýk baþlangýç mermisini de alýyor
    public void SetupDrop(Sprite weaponSprite, WeaponType type, int ammo = -1)
    {
        if (_spriteRenderer != null) _spriteRenderer.sprite = weaponSprite;
        weaponType = type;
        currentAmmo = ammo;
    }
}