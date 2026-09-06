# PAYOUT — Kodlama Standartları ve Prensipleri (CODE_STYLE)

---

## 1. Genel İlkeler ve Temel Felsefe

* **Tek Sorumluluk İlkesi (Single Responsibility):** Her sınıf tek bir amaca hizmet eder. Örneğin `EnemyController` düşmanın canını, animasyonlarını ve infaz durumunu yönetirken; `PlayerAwarenessController` hareket, devriye ve hedef takibi mantığını üstlenir.
* **Performans Önceliği (Zero Allocation in Hot Paths):** Her karede (`Update`, `FixedUpdate`) çalışan kodlar içinde çöp toplayıcı (Garbage Collector) tetikleyecek nesne tahsislerinden (`new`, `Instantiate`, `Destroy`) ve pahalı sorgulardan (`Find`, `GetComponent`) kesinlikle kaçınılır.
* **Kapsülleme ve Güvenlik:** Sınıf içi değişkenler dışarıya doğrudan public olarak açılmaz; C# özellikleri (Properties) ile salt-okunur (`{ get; private set; }`) biçimde sunulur.

---

## 2. İsimlendirme Kuralları (Naming Conventions)

| Öğe Türü | Kural | Örnek |
| :--- | :--- | :--- |
| **Sınıflar & Structlar** | `PascalCase` | `PlayerController`, `WeaponPrefabMap` |
| **Arayüzler (Interfaces)** | `I` ön eki + `PascalCase` | `IDamageable`, `IInteractable` |
| **Public Özellikler (Properties)** | `PascalCase` | `CurrentAmmo`, `IsDead`, `FinisherStaminaRatio` |
| **Private & Protected Alanlar** | `_camelCase` (Alt tire ile başlar) | `_moveSpeed`, `_fireRate`, `_rigidbody` |
| **Metotlar** | `PascalCase` | `EquipWeapon()`, `TryFire()`, `Stun()` |
| **Parametreler & Yerel Değişkenler** | `camelCase` | `targetVelocity`, `hitDirection`, `angleOffset` |
| **Sabitler (Constants)** | `PascalCase` veya `UPPER_SNAKE` | `MusicParam`, `MAX_POOL_SIZE` |
| **Olaylar (Events & Actions)** | `On` ön eki + `PascalCase` | `OnPlayerAmmoChanged`, `OnEnemyDied` |
| **Enum Türleri ve Değerleri** | `PascalCase` | `enum WeaponType { Pistol, Shotgun }` |

---

## 3. Unity Inspector ve Serileştirme Pratikleri

* **Alan Görünürlüğü:** Inspector üzerinde gösterilecek değişkenler için `public` yerine **`[SerializeField] private`** kalıbı zorunludur:
  ```csharp
  // DOĞRU
  [SerializeField] private float _speed = 5f;
  [SerializeField] private AudioClip _fireSound;

  // YANLIŞ
  public float speed = 5f;
  ```
* **Gruplandırma ve Belgelendirme:**
  * İlgili alanlar `[Header("...")]` ile mantıksal gruplara ayrılmalıdır.
  * Belirsiz veya kritik alanlar `[Tooltip("...")]` ile açıklanmalıdır.
  * Sayısal sınırlar `[Range(min, max)]` ile güvenceye alınmalıdır:
  ```csharp
  [Header("Movement Settings")]
  [Tooltip("Karakterin saniye başına hareket hızı")]
  [SerializeField] private float _speed = 4.5f;

  [Header("Audio Settings")]
  [SerializeField] [Range(0f, 1f)] private float _volume = 0.8f;
  ```

---

## 4. Unity Yaşam Döngüsü (Lifecycle Best Practices)

### 4.1. `Awake()`
* Kendi GameObject'i üzerindeki bileşenlerin alınması (`GetComponent`).
* Temel fizik veya ses başlangıç ayarlarının yapılması.
* Katman (Layer) ID'lerinin önbelleğe alınması (`LayerMask.NameToLayer(...)`).

### 4.2. `Start()`
* Sahnedeki diğer nesnelere referans kurulması veya başlatma olaylarının tetiklenmesi.
* UI ilk değerlerinin basılması.

### 4.3. `Update()`
* Yalnızca kullanıcı girdilerinin okunması (`Keyboard.current`, `Mouse.current`).
* Zamanlayıcıların (Timers) eksiltilmesi (`Time.deltaTime`).
* Animator parametrelerinin güncellenmesi (`SetBool`, `SetTrigger`).
* **Kural:** Fiziksel hız veya kuvvet güncellemeleri ASLA `Update()` içinde yapılmaz!

### 4.4. `FixedUpdate()`
* Rigidbody2D hız değişiklikleri (`linearVelocity`).
* Fizik kuvvetleri (`AddForce`, `AddTorque`).
* Fiziksel çarpışma ve linecast kontrolleri (`Time.fixedDeltaTime`).
* **Unity 6 Notu:** `rigidbody.velocity` yerine Unity 6 ile gelen `rigidbody.linearVelocity` kullanılmalıdır.

### 4.5. `OnEnable()` ve `OnDisable()` (Event Hijyeni)
Bellek sızıntılarını ve yok edilmiş nesnelerin olay dinlemeye devam etmesini engellemek için olay abonelikleri mutlaka çiftler halinde yönetilmelidir:
```csharp
private void OnEnable()
{
    Weapon.OnPlayerAmmoChanged += UpdateText;
}

private void OnDisable()
{
    Weapon.OnPlayerAmmoChanged -= UpdateText;
}
```

---

## 5. Fizik, 2D Matematik ve Çarpışma Kuralları

### 5.1. Tünelleme Önleme (Continuous Collision Detection)
* Yüksek hızlı mermilerde standart `OnTriggerEnter2D` yerine iki kare arası fizik rotasını tarayan `Physics2D.LinecastAll` tercih edilmelidir:
  ```csharp
  RaycastHit2D[] hits = Physics2D.LinecastAll(_previousPosition, transform.position);
  ```

### 5.2. Açı ve Rotasyon Hesaplamaları
* 2D düzlemde yön vektöründen açıya geçerken `Mathf.Atan2` kullanılır:
  ```csharp
  Vector2 direction = targetPosition - transform.position;
  float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
  transform.rotation = Quaternion.Euler(0, 0, angle + _rotationOffset);
  ```

### 5.3. Katman Maskeleri (LayerMask) Kullanımı
* Raycast veya Linecast yaparken mutlaka spesifik katman maskesi verilmelidir; aksi takdirde mermiler görünmez tetikleyicilere veya geçersiz nesnelere çarparak kaybolur:
  ```csharp
  RaycastHit2D hit = Physics2D.Linecast(start, end, LayerMask.GetMask("Obstacle", "Door"));
  ```

---

## 6. Performans Kuralları ve Kaçınılacak Hatalar (Anti-Patterns)

| Yapılmaması Gerekenler (Anti-Pattern) | Yapılması Gereken (Best Practice) |
| :--- | :--- |
| `Update()` içinde `GameObject.Find()` veya `GetComponent()` çağırmak. | Referansı `Awake()` içinde alıp sınıf seviyesinde önbelleklemek (`cache`). |
| Mermi veya kan için her atışta `Instantiate()` ve `Destroy()` kullanmak. | `BulletPool` ve `EffectPool` üzerinden nesneleri aktif/pasif yapmak. |
| Unity'nin eski `FindObjectOfType<T>()` metodunu kullanmak. | Unity 6 standardı olan `Object.FindAnyObjectByType<T>()` kullanmak. |
| Sahne geçişlerinde event aboneliklerini açık bırakmak. | `OnDisable()` veya `OnDestroy()` içinde `-=` ile aboneliği sonlandırmak. |
| Kullanıcıdan onay almadan Git üzerinden `push` komutu çalıştırmak. | Değişiklikleri yerelde hazırlayıp kullanıcıya GitHub Desktop özeti sunmak. |

---

## 7. Standart Kod Şablonları

### 7.1. Yeni Bir Etkileşimli Çevre Nesnesi Şablonu
```csharp
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractiveObjectTemplate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _interactionRadius = 2f;
    [SerializeField] private LayerMask _targetLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip _triggerSound;
    [SerializeField] [Range(0f, 1f)] private float _soundVolume = 1f;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
    }

    public void TriggerInteraction()
    {
        if (_triggerSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_triggerSound, _soundVolume);
        }
        // Özel etkileşim mantığı burada işletilir
    }
}
```
