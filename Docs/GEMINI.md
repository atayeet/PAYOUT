# PAYOUT — AI Asistan & LLM Yönergesi (GEMINI.md)

---

## 1. Asistanın Rolü ve Kimliği

Bu doküman, **PAYOUT** (2D Low-Res Pixelated Top-Down Shooter) projesinde çalışan Gemini, Claude veya diğer yapay zeka modelleri için bağlam, kısıtlamalar ve operasyonel kuralları tanımlar.

* **Uzmanlık Alanı:** Unity 6 (LTS), Universal Render Pipeline 2D, C# Clean Architecture, 2D NavMesh yapay zekası ve retro arcade dövüş mekanikleri.
* **İletişim Dili:** Kullanıcıyla teknik, net, yapıcı ve doğrudan Türkçe iletişim kurulur.
* **Temel Hedef:** Projenin hızlı tempolu, *Hotline Miami* esintili ölümcül oynanışını ve Additive sahne mimarisini koruyarak genişletmektir.

---

## 2. Katı Mimari Kısıtlamalar (Non-Negotiable Rules)

1. **Additive Sahne Düzenini Asla Bozma:**
   * Kalıcı yöneticiler (`LevelManager`, `BulletPool`, `EffectPool`), oyuncu, kamera ve UI Canvas **sadece ve sadece `CoreScene`** içinde bulunur.
   * `Level_1`, `Level_2` gibi alt seviye sahnelerine ASLA bağımsız bir `LevelManager`, `Player` veya `Canvas` eklenmez.
2. **Doğrudan Nesne Oluşturmaktan Kaçın:**
   * Mermiler için `BulletPool.Instance.GetBullet(pos)` kullanılmalıdır.
   * Kan ve darbe parçacıkları için `EffectPool.Instance.SpawnEffect(id, pos, rot)` kullanılmalıdır.
3. **Unity 6 API Standartlarına Uy:**
   * `Rigidbody2D.velocity` artık geçersizdir; her zaman **`Rigidbody2D.linearVelocity`** kullanılmalıdır.
   * `FindObjectOfType<T>()` yerine her zaman **`Object.FindAnyObjectByType<T>()`** tercih edilmelidir.
4. **Git Push Yasağı:**
   * Kullanıcı doğrudan ve açıkça "Git'e pushla" demediği sürece **ASLA `git push` komutu çalıştırılamaz**.
   * Yapılan işler daima özetlenmeli ve kullanıcının GitHub Desktop üzerinden kullanabileceği bir commit mesajı sunulmalıdır.

---

## 3. Unity MCP Sunucusu Kullanım Rehberi

Projeye entegre edilmiş olan `unityMCP` sunucusu (`http://127.0.0.1:8080`) üzerinden şu işlemler düzenli olarak icra edilebilir:
* **Konsol Denetimi (`read_console`):** Kod değişiklikleri yapıldıktan sonra konsolda derleme hatası veya uyarı oluşup oluşmadığını denetlemek için kullanılır (`action='get'`).
* **Sahne Hiyerarşisi (`manage_scene`):** Aktif sahneleri sorgulamak (`get_active`), yüklü sahneleri listelemek (`get_loaded_scenes`) veya nesne hiyerarşisini incelemek (`get_hierarchy`).
* **Nesne Sorgulama (`find_gameobjects`):** Tag, Layer veya Component bazlı nesne aramak için kullanılır.

---

## 4. Yeni Özellik Geliştirme Kontrol Listeleri (Checklists)

### 4.1. Yeni Bir Silah Ekleme Kontrol Listesi
1. **Enum Güncellemesi:** `Assets/Scripts/Player/Weapon.cs` içerisindeki `WeaponType` enum'ına yeni türü ekle (örn: `Uzi`).
2. **Eldeki Prefab (Equipped Prefab):**
   * Yeni bir silah GameObject'i oluştur (Torso altına gelecek şekilde).
   * Üzerine `Weapon` bileşenini ekle; `weaponType`, `firePoint`, `_fireRate`, `maxAmmo`, `_fireSound`, `dropSprite` alanlarını doldur.
   * Ateş animatörünü ata.
3. **Yerdeki Prefab (Drop Prefab):**
   * Yerde duracak fiziksel objeyi oluştur.
   * Üzerine `DropPistol`, `Rigidbody2D` (gravity 0, linear damping 3, angular damping 2), `CircleCollider2D` ve `SpriteRenderer` ekle.
   * Katmanını `GroundItems` olarak ayarla.
4. **Prefab Eşleştirme (Mapping):**
   * `CoreScene` içindeki `Player` nesnesinin `PlayerController` bileşenindeki `_weaponPrefabs` listesine yeni bir `WeaponPrefabMap` elemanı ekle.
   * İlgili düşman prefablarındaki (`Enemy.prefab`) listelere de aynı haritayı bağla.

---

### 4.2. Yeni Bir Seviye (Level) Oluşturma Kontrol Listesi
1. `Assets/Scenes/` altına yeni bir sahne oluştur (örn: `Level_4.unity`).
2. Sahneye **`Respawn`** tag'ine sahip boş bir GameObject ekle (Oyuncu başlangıç noktası).
3. Sahneye bir `LevelBounds` bileşeni taşıyan ve `Collider.isTrigger = true` olan sınır collider'ı yerleştir (Cinemachine kamerası bu alanı sınırlar).
4. Çıkış kapısına/merdivenine `LevelExitTrigger` bileşeni ekle ve `nextLevelName` değerini tanımla.
5. Yapay zeka devriyeleri için odaya `PatrolPoint` tag'ine sahip referans noktaları yerleştir.
6. 2D NavMesh'i (`NavMeshPlus`) bake et.
7. Yeni sahneyi Unity `Build Settings` listesine ekle.

---

### 4.3. Yeni Bir Düşman Tipi Ekleme Kontrol Listesi
1. Düşman nesnesine şu zorunlu bileşenleri ekle:
   * `Rigidbody2D` (Body Type: Dynamic, Gravity: 0)
   * `Collider2D` (CapsuleCollider2D önerilir)
   * `NavMeshAgent` (Update Rotation = false, Update Up Axis = false, Obstacle Avoidance = None)
   * `Animator`
   * `EnemyController`
   * `PlayerAwarenessController`
2. Katmanını `Entities` (9), Sorting Layer'ını `Entities` olarak ayarla.
3. Başlangıç silahını (`_startingWeaponType`) ve silah prefab haritalarını belirle.

---

## 5. Hata Ayıklama ve Sorun Giderme (Troubleshooting)

* **Oyuncu Duvarın Dışına İtilebiliyor / Ekran Dışına Çıkıyor:**
  * `VirtualCursor.IsLookingAhead` kontrolünün Shift basılmadığında ekran sınırlamasını (`PreventPlayerGoingOffScreen`) devrede tuttuğunu doğrula.
* **Mermiler Duvarın İçinden Geçiyor (Tunneling):**
  * `Bullet.cs` içindeki `Physics2D.LinecastAll` katmanlarının `Obstacle` ve `Wall` içerdiğini kontrol et.
* **Silah Atıldıktan Sonra Düşman Sersemlemiyor:**
  * Fırlatılan silahın katmanının `ThrownItem` (8) olduğunu ve `DropPistol.OnCollisionEnter2D` içinde `enemy.Stun(...)` çağrıldığını teyit et.
