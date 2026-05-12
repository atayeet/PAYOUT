using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    public float fireForce;
    
    [Tooltip("Duvarlarýn ait olduðu katmaný (Layer) seçin")]
    public LayerMask _obstacleLayer; // Duvarlarý ayýrt etmek için
    
    [Header("Fire Rate Settings")]
    public float _fireRate = 0.2f; 
    private float _nextFireTime = 0f;

    private bool _isEnemyWeapon; // YENÝ: Silahýn sahibi Enemy mi?

    private void Awake()
    {
        // En üst ebeveynde (Player veya Enemy) düþman scripti bulursa true olur
        _isEnemyWeapon = GetComponentInParent<EnemyController>() != null;
    }

    // void olan TryFire fonksiyonunu, ateþ edebilirse 'true' dönecek þekilde (bool) güncelliyoruz.
    public bool TryFire()
    {
        // Eðer þu anki zaman, bir sonraki izin verilen ateþ zamanýna ulaþtýysa ateþ et
        if (Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + _fireRate; // Bir sonraki ateþ zamanýný ileriye taþý
            return true; // Baþarýyla ateþ edildi
        }
        
        return false; // Bekleme süresinden dolayý ateþ edilemedi
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

        Bullet bullet = BulletPool.Instance.GetBullet(spawnPosition);

        // YENÝ: Mermiye, bir düþman tarafýndan sýkýldýðýný bildiriyoruz
        bullet.isEnemyBullet = _isEnemyWeapon; 

        bullet.transform.rotation = firePoint.rotation;
        bullet.rb.AddForce(firePoint.right * fireForce, ForceMode2D.Impulse);
    }
}
