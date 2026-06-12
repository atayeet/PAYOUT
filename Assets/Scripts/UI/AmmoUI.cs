using UnityEngine;
using TMPro; // TextMeshPro kullanıyorsanız

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText; // UI üzerindeki Text referansınız

    private void OnEnable()
    {
        // Oyuncu ateş ettiğinde, UI otomatik olarak bu fonksiyon ile haberdar olacak
        Weapon.OnPlayerAmmoChanged += UpdateText;
    }

    private void OnDisable()
    {
        // Memleak (hafıza sızıntısı) olmaması için aboneliği iptal etmek önemli
        Weapon.OnPlayerAmmoChanged -= UpdateText;
    }

    private void UpdateText(int currentAmmo, int maxAmmo)
    {
        // UI Text'ini ekranda Formatlı olarak (Örn: "8 / 8") gösterir
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
            
            // Sıfıra ulaştığında rengi kırmızı yaparak oyuncuyu uyarabilirsiniz!
            ammoText.color = currentAmmo <= 0 ? Color.red : Color.white;
        }
    }
}
