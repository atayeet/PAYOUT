using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    public float fireForce;
    
    [Tooltip("Duvarlarýn ait olduðu katmaný (Layer) seçin")]
    public LayerMask _obstacleLayer; // Duvarlarý ayýrt etmek için
    
    [Tooltip("Ýki mermi arasýndaki bekleme süresi (Saniye)")]

    [Header("Fire Rate Settings")]
    public float _fireRate = 0.2f; 
    private float _nextFireTime = 0f;

    // Ateþ tuþuna basýlý tutulduðunda bu metodu çaðýrýn
    public void TryFire()
    {
        // Eðer þu anki zaman, bir sonraki izin verilen ateþ zamanýna ulaþtýysa ateþ et
        if (Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + _fireRate; // Bir sonraki ateþ zamanýný ileriye taþý
        }
    }

    // Doðrudan çaðrýlmasýný önlemek için private veya protected yapabilirsiniz
    private void Fire()
    {
        Vector2 origin = transform.position; // Silahýn kendi konumu
        Vector2 target = firePoint.position;
        Vector2 spawnPosition = target;

        // Silahýn merkezi ile firepoint arasýna bir çizgi çeker. 
        // Eðer arada belirttiðimiz Layer'a ait bir obje (duvar) varsa tespit eder.
        RaycastHit2D hit = Physics2D.Linecast(origin, target, _obstacleLayer);

        if (hit.collider != null)
        {
            // Eðer arada duvar varsa, mermiyi silahýn ucundan deðil, duvara çarptýðý noktadan çýkar
            spawnPosition = hit.point;
        }

        //GameObject _projectile = Instantiate(_bullet, spawnPosition, _firePoint.rotation);

        Bullet bullet = BulletPool.Instance.GetBullet(spawnPosition);

        bullet.transform.rotation = firePoint.rotation;

        bullet.rb.AddForce(firePoint.right * fireForce, ForceMode2D.Impulse);
    }
}
