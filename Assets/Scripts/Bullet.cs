using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody2D _rigidbody;

    public GameObject _impactEffect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Destroy(collision.gameObject); // Düþmaný yok et
            Destroy(gameObject);           // Efekt oluþturmadan sadece mermiyi yok et
        }
        else if (collision.CompareTag("Wall"))
        {
            Impact(); // Mermiyi yok et ve efekt oluþtur
        }
    }

    public void Impact()
    {
        // Efekti oluþtur (Eðer _impactEffect atandýysa)
        if (_impactEffect != null)
        {
            Instantiate(_impactEffect, transform.position, Quaternion.identity); 
        }
        
        Destroy(gameObject); // Mermiyi yok et
    }
}
