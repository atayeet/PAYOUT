using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(HingeJoint2D))]
public class DoorController : MonoBehaviour
{
    [Header("Door Motor Settings")]
    [Tooltip("Motorun kapýyý açma hýzý (Derece/Saniye)")]
    [SerializeField] private float _motorSpeed = 500f;
    
    [Tooltip("Motorun uygulayacaðý güç (Tork)")]
    [SerializeField] private float _motorTorque = 1000f;
    
    [Tooltip("Kapýnýn açýk kalacaðý süre (Saniye)")]
    [SerializeField] private float _closeDelay = 3f;

    [Header("Stun Settings")]
    [Tooltip("Kapýnýn çarpacaðý alaný belirleyen yarýçap")]
    [SerializeField] private float _hitRadius = 1.5f;

    [Tooltip("Düþmaný savurma gücü")]
    [SerializeField] private float _stunKnockbackForce = 12f;

    private HingeJoint2D _hingeJoint;
    private Quaternion _closedRotation;
    private bool _isOpen = false;

    private void Awake()
    {
        _hingeJoint = GetComponent<HingeJoint2D>();
        _closedRotation = transform.rotation;
        
        // Baþlangýçta motor kapalý olsun
        _hingeJoint.useMotor = false;
        
        // KAPALI DURUMDAYKEN KAPIYI KÝLÝTLE (Fizik motoru ittirmesin diye limitleri 0 yapýyoruz)
        LockDoorPhysically();
    }

    // Kapýyý fiziksel olarak kilitleyen yardýmcý metot
    private void LockDoorPhysically()
    {
        _hingeJoint.useLimits = true;
        JointAngleLimits2D limits = _hingeJoint.limits;
        limits.min = 0f;
        limits.max = 0f;
        _hingeJoint.limits = limits;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool isPlayer = collision.gameObject.GetComponent<PlayerController>() != null;
        bool isEnemy = collision.gameObject.GetComponent<EnemyController>() != null;

        // Kapý kapalýysa ve çarpan obje Player veya Enemy ise
        if (!_isOpen && (isPlayer || isEnemy))
        {
            Vector2 hitDirection = (transform.position - collision.transform.position).normalized;

            float dotProduct = Vector2.Dot(transform.right, hitDirection);
            
            float speedSign = dotProduct > 0 ? -1f : 1f;

            StartCoroutine(DoorRoutine(hitDirection, speedSign, isPlayer));
        }
    }

    private IEnumerator DoorRoutine(Vector2 knockbackDirection, float speedSign, bool isOpenedByPlayer)
    {
        _isOpen = true;

        if (isOpenedByPlayer)
        {
            CheckForEnemiesToStun(knockbackDirection);
        }

        // AÇILIRKEN JOINT LIMITLERINI ÝSTENÝLEN AÇILARA AYARLIYORUZ
        JointAngleLimits2D limits = _hingeJoint.limits;
        if (speedSign > 0)
        {
            limits.min = 0f;
            limits.max = 90f;
        }
        else
        {
            limits.min = -90f;
            limits.max = 0f;
        }
        _hingeJoint.limits = limits;

        // Motoru açýlýþ için çalýþtýr
        _hingeJoint.useMotor = true;
        JointMotor2D motor = _hingeJoint.motor;
        motor.maxMotorTorque = _motorTorque;
        motor.motorSpeed = _motorSpeed * speedSign;
        _hingeJoint.motor = motor;

        yield return new WaitForSeconds(_closeDelay);

        // Kapanýþ için motoru tam ters yöne ayarla
        motor.motorSpeed = -(_motorSpeed * speedSign);
        _hingeJoint.motor = motor;

        // Kapýnýn açýsal olarak 0'a (kapanma notkasýna) yakýnlaþmasýný bekle
        while (Mathf.Abs(_hingeJoint.jointAngle) > 2f)
        {
            yield return null;
        }

        // KAPANDI! YAPILMASI GEREKENLER:

        // 1. Kapýyý fiziksel ittirmelere karþý tekrar tamamen kilitliyoruz. (min: 0, max: 0)
        LockDoorPhysically();
        
        // 2. Motoru tamamen durdur
        motor.motorSpeed = 0f;
        _hingeJoint.motor = motor;
        _hingeJoint.useMotor = false;
        
        // 3. Fiziksel olarak Rigidbody hýzlarýný sil ve rotasyonu tamamen "Baþlangýç" rotasyonuna eþitle
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.SetRotation(_closedRotation); 
        }
        else
        {
            transform.rotation = _closedRotation;
        }

        _isOpen = false;
    }

    private void CheckForEnemiesToStun(Vector2 stunDirection)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _hitRadius);

        foreach (Collider2D hitCollider in hits)
        {
            EnemyController enemy = hitCollider.GetComponent<EnemyController>();

            if (enemy != null && enemy.enabled && !enemy.IsCurrentlyStunned)
            {
                enemy.Stun(stunDirection, _stunKnockbackForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _hitRadius);
    }
}