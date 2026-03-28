using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject _bullet;
    public Transform _firePoint;
    public float _fireForce;

    public void Fire()
    {
        GameObject _projectile = Instantiate(_bullet, _firePoint.position, _firePoint.rotation);
        _projectile.GetComponent<Rigidbody2D>().AddForce(_firePoint.right * _fireForce, ForceMode2D.Impulse);
    }
}
