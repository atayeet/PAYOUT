using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody2D rb;
    public GameObject impactEffect;
    
    
    private Camera _mainCamera;

    // Bir önceki karenin pozisyonunu tutmak için
    private Vector2 _previousPosition;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        // Mermi havuzdan her çaðýrýldýðýnda ilk pozisyonu güncellenir
        _previousPosition = transform.position;

        // Önceki hareketten arta kalan fiziksel hýzlarý sýfýrla
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void FixedUpdate()
    {
        CheckHighSpeedCollision();
        DestroyWhenOffScreen(); 
    }

    private void CheckHighSpeedCollision()
    {
        // Bir önceki kareden þu anki kareye kadar olan çizgideki TÜM objeleri kontrol et
        RaycastHit2D[] hits = Physics2D.LinecastAll(_previousPosition, transform.position);

        foreach (RaycastHit2D hit in hits)
        {
            // Merminin kendi collider'ýný kazara vurmasýný önle
            if (hit.collider.gameObject == this.gameObject) continue;

            Vector2 bulletDirection = ((Vector2)transform.position - _previousPosition).normalized;
            if (bulletDirection == Vector2.zero) 
                bulletDirection = transform.right;

            // Düþman (veya hasar alabilen yapý) kontrolü
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Hasarý, merminin vurduðu noktayý ve yönü ilet
                damageable.TakeDamage(1, hit.point, bulletDirection);
                BulletPool.Instance.ReturnBullet(this); // Mermiyi havuza geri gönder
                break; // Mermi patladýðý için döngüden çýk
            }
            else if (hit.collider.CompareTag("Wall"))
            {
                // Mermiyi tam çarptýðý noktaya taþý (efektin doðru yerde çýkmasý için)
                transform.position = hit.point;
                Impact();
                break; // Duvara çarptýðý için döngüden çýk
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
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity); 
        }
        
        BulletPool.Instance.ReturnBullet(this);
    }
}
