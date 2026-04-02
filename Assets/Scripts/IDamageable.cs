using UnityEngine;

public interface IDamageable
{
    // Merminin çarptýðý noktayý ve geliþ yönünü iletiyoruz
    void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitDirection);
}
