using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DropPistol : MonoBehaviour
{
    [SerializeField] private float _spinSpeed = 1000f; // Fýrlatýldýðýnda döneceði hýz
    [SerializeField] private float _stunKnockbackForce = 10f; // Düþmaný savurma gücü

    private Rigidbody2D _rb;
    private int _originalLayer;
    private int _thrownLayer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f; // Yer çekimi etkisini sýfýrla

        // Fýrlatýlýnca zamanla durmasý için sürtünme ekliyoruz.
        _rb.linearDamping = 3f; // .NET Framework / eski unity sürümlerinde drag olarak geçiyordu, .linearDamping ya da drag uygundur.
        _rb.angularDamping = 2f; // Dönme hareketi için sürtünme

        // Baþlangýçtaki katmaný kaydet (GroundItems) ve geçilecek katmaný belirle
        _originalLayer = gameObject.layer;
        _thrownLayer = LayerMask.NameToLayer("ThrownItem");
    }

    public void Throw(Vector2 direction, float force)
    {
        // Fýrlatýldýðýnda katmaný ThrownItem olarak deðiþtir (Düþmanla çarpýþabilmesi için)
        if (_thrownLayer != -1)
        {
            gameObject.layer = _thrownLayer;
        }

        // Kendi etrafýnda dönme kuvveti (torque) uygula
        _rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        _rb.AddTorque(_spinSpeed * (Random.value > 0.5f ? 1f : -1f));
    }

    private void FixedUpdate()
    {
        // Yerdeyken hýz belirli bir seviyenin altýna düþtüðünde tamamen durdur
        if (_rb.linearVelocity.magnitude < 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            // Silah tamamen durduðunda tekrar orijinal katmanýna (GroundItems) geri dön
            gameObject.layer = _originalLayer;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Yeterince hýzlý uçuyorsa bir düþmana çarptýðýnda sersemlet (Stun)
        if (_rb.linearVelocity.magnitude > 0f)
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Silahýn gidiþ yönüne veya çarpma açýsýna göre bir savrulma yönü hesapla
                Vector2 knockbackDir = _rb.linearVelocity.normalized;
                
                // Düþmana savrulma yönü ve kuvveti ile stun uygula
                enemy.Stun(knockbackDir, _stunKnockbackForce);
            }
        }
    }
}
