using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(HingeJoint2D))]
public class DoorController : MonoBehaviour
{
    [Header("Door Motor Settings")]
    [Tooltip("Motorun kap�y� a�ma h�z� (Derece/Saniye)")]
    [SerializeField] private float _motorSpeed = 500f;
    
    [Tooltip("Motorun uygulayaca�� g�� (Tork)")]
    [SerializeField] private float _motorTorque = 1000f;
    
    [Tooltip("Kap�n�n a��k kalaca�� s�re (Saniye)")]
    [SerializeField] private float _closeDelay = 3f;

    [Header("Stun Settings")]
    [Tooltip("Kap�n�n �arpaca�� alan� belirleyen yar��ap")]
    [SerializeField] private float _hitRadius = 1.5f;

    [Tooltip("D��man� savurma g�c�")]
    [SerializeField] private float _stunKnockbackForce = 12f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip _doorSound;
    [SerializeField] [Range(0f, 2f)] private float _doorSoundVolume = 1.2f;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup _sfxGroup;
    private AudioSource _audioSource;

    private HingeJoint2D _hingeJoint;
    private Quaternion _closedRotation;
    private bool _isOpen = false;
    private bool _canPlaySound = true;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1.0f;
        if (_sfxGroup != null)
        {
            _audioSource.outputAudioMixerGroup = _sfxGroup;
        }

        _hingeJoint = GetComponent<HingeJoint2D>();
        _closedRotation = transform.rotation;
        
        // Ba�lang��ta motor kapal� olsun
        _hingeJoint.useMotor = false;
        
        // KAPALI DURUMDAYKEN KAPIYI K�L�TLE (Fizik motoru ittirmesin diye limitleri 0 yap�yoruz)
        LockDoorPhysically();
    }

    // Kap�y� fiziksel olarak kilitleyen yard�mc� metot
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

        // Kap� kapal�ysa ve �arpan obje Player veya Enemy ise
        if (!_isOpen && (isPlayer || isEnemy))
        {
            if (_canPlaySound)
            {
                if (_audioSource != null && _doorSound != null)
                {
                    _audioSource.PlayOneShot(_doorSound, _doorSoundVolume);
                }
                _canPlaySound = false;
            }

            Vector2 hitDirection = (transform.position - collision.transform.position).normalized;

            float dotProduct = Vector2.Dot(transform.right, hitDirection);
            
            float speedSign = dotProduct > 0 ? -1f : 1f;

            StartCoroutine(DoorRoutine(hitDirection, speedSign, isPlayer));
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        bool isPlayer = collision.gameObject.GetComponent<PlayerController>() != null;
        bool isEnemy = collision.gameObject.GetComponent<EnemyController>() != null;

        if (isPlayer || isEnemy)
        {
            _canPlaySound = true;
        }
    }

    private IEnumerator DoorRoutine(Vector2 knockbackDirection, float speedSign, bool isOpenedByPlayer)
    {
        _isOpen = true;

        if (isOpenedByPlayer)
        {
            CheckForEnemiesToStun(knockbackDirection);
        }

        // A�ILIRKEN JOINT LIMITLERINI �STEN�LEN A�ILARA AYARLIYORUZ
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

        // Motoru a��l�� i�in �al��t�r
        _hingeJoint.useMotor = true;
        JointMotor2D motor = _hingeJoint.motor;
        motor.maxMotorTorque = _motorTorque;
        motor.motorSpeed = _motorSpeed * speedSign;
        _hingeJoint.motor = motor;

        yield return new WaitForSeconds(_closeDelay);

        // Kapan�� i�in motoru tam ters y�ne ayarla
        motor.motorSpeed = -(_motorSpeed * speedSign);
        _hingeJoint.motor = motor;

        // Kap�n�n a��sal olarak 0'a (kapanma notkas�na) yak�nla�mas�n� bekle
        while (Mathf.Abs(_hingeJoint.jointAngle) > 2f)
        {
            yield return null;
        }

        // KAPANDI! YAPILMASI GEREKENLER:

        // 1. Kap�y� fiziksel ittirmelere kar�� tekrar tamamen kilitliyoruz. (min: 0, max: 0)
        LockDoorPhysically();
        
        // 2. Motoru tamamen durdur
        motor.motorSpeed = 0f;
        _hingeJoint.motor = motor;
        _hingeJoint.useMotor = false;
        
        // 3. Fiziksel olarak Rigidbody h�zlar�n� sil ve rotasyonu tamamen "Ba�lang��" rotasyonuna e�itle
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