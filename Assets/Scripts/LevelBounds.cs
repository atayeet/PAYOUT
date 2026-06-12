using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LevelBounds : MonoBehaviour
{
    private Collider2D _collider;

    public Collider2D Collider
    {
        get
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }
            return _collider;
        }
    }

    private void Awake()
    {
        // Collider'ın tetikleyici (Trigger) olmasını öneriyoruz ki oyuncu veya fizik objeleriyle çakışmasın,
        // sadece kamerayı sınırlandırmak için kullanılsın.
        if (Collider != null)
        {
            Collider.isTrigger = true;
        }
    }
}
