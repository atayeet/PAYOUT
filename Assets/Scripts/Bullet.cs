using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody2D _rigidbody;
    public GameObject _impactEffect;
    
    [Header("Blood Effects")]
    public GameObject _frontBloodEffect; // Önden çýkacak ufak kan (Giriþ yarasý)
    public GameObject _backBloodEffect;  // Arkadan çýkacak büyük açýlý kan (Çýkýþ yarasý)
    
    private Camera _mainCamera;

    // Bir önceki karenin pozisyonunu tutmak için
    private Vector2 _previousPosition;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        // Mermi doðduðunda ilk pozisyonu kaydet
        _previousPosition = transform.position;
    }

    private void Update()
    {
        CheckHighSpeedCollision();
        DestroyWhenOffScreen(); 
    }

    private void CheckHighSpeedCollision()
    {
        // Bir önceki kareden þu anki kareye kadar olan mesafeyi kontrol et
        RaycastHit2D hit = Physics2D.Linecast(_previousPosition, transform.position);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // 1. Merminin hareket yönünü hesapla
                Vector2 bulletDirection = ((Vector2)transform.position - _previousPosition).normalized;
                if (bulletDirection == Vector2.zero) 
                    bulletDirection = transform.right; // Eðer 0 ise merminin baktýðý yönü baz al

                // 2. ÖN KAN EFEKTÝ (Giriþ Yarasý)
                if (_frontBloodEffect != null)
                {
                    // Çarpýþma normalini baz al (çarptýðý yüzeyden dýþarý doðru püskürmesi için)
                    float frontAngle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg;
                    Instantiate(_frontBloodEffect, hit.point, Quaternion.Euler(0, 0, frontAngle));
                }

                // 3. ARKA KAN EFEKTÝ (Çýkýþ Yarasý)
                if (_backBloodEffect != null)
                {
                    // Düþmanýn merkezinden merminin gidiþ yönüne doðru bir offset (uzaklýk) belirliyoruz
                    // Böylece kan, düþmanýn önünden deðil doðrudan arkasýndan fýþkýrýyor hissiyatý verecek.
                    Vector2 enemyCenter = hit.collider.bounds.center;
                    float backOffset = 0.4f; // Düþman Sprite'ýnýn boyutuna göre bu deðeri ayarlayabilirsiniz (örn: 0.5f)
                    Vector2 backPoint = enemyCenter + (bulletDirection * backOffset);

                    // Kanýn fýþkýrma açýsý, merminin yönüyle ayný olacak
                    float backAngle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
                    Instantiate(_backBloodEffect, backPoint, Quaternion.Euler(0, 0, backAngle));
                }

                // Düþmaný öldür
                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.Die();
                }
                else
                {
                    Destroy(hit.collider.gameObject); // Yedek güvenlik
                }

                BulletPool.Instance.ReturnBullet(this);
            }
            else if (hit.collider.CompareTag("Wall"))
            {
                // Mermiyi tam çarptýðý noktaya taþý (efektin doðru yerde çýkmasý için)
                transform.position = hit.point;
                Impact();
            }
        }

        // Pozisyonu bir sonraki kare için güncelle
        _previousPosition = transform.position;
    }

    // Linecast kullandýðýmýz için normal OnTriggerEnter2D'yi silin veya yedek olarak býrakabilirsiniz.
    // Ýkisi ayný anda çalýþýp çift tetiklenme yapmasýn diye OnTriggerEnter2D'yi kaldýrmak genelde daha temizdir.
    
    private void DestroyWhenOffScreen()
    {
        Vector2 screenPoint = _mainCamera.WorldToViewportPoint(transform.position);
        if (screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1)
        {
            BulletPool.Instance.ReturnBullet(this);
        }
    }

    public void Impact()
    {
        // Efekti oluþtur (Eðer _impactEffect atandýysa)
        if (_impactEffect != null)
        {
            Instantiate(_impactEffect, transform.position, Quaternion.identity); 
        }
        
        BulletPool.Instance.ReturnBullet(this);
    }
}
