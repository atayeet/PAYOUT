# PAYOUT — Proje Bağlamı ve Teknik Mimari (CONTEXT)

---

## 1. Proje Profili ve Teknoloji Yığını

* **Oyun Başlığı:** PAYOUT
* **Depo URL:** [https://github.com/atayeet/PAYOUT](https://github.com/atayeet/PAYOUT)
* **Unity Sürümü:** `Unity 6000.4.11f1` (Unity 6 LTS)
* **Render Pipeline:** Universal Render Pipeline (URP 2D) `17.4.0`
* **Girdi Sistemi:** Unity New Input System `1.19.0`
* **Kamera Sistemi:** Unity Cinemachine `3.1.6` (CinemachineCamera, CinemachineConfiner2D)
* **Yapay Zeka & Navigasyon:** NavMeshPlus (`com.h8man.2d.navmeshplus`), Unity AI Navigation `2.0.13`
* **Metin & UI:** TextMeshPro (UGUI `2.0.0`)
* **Editör Entegrasyonları:** MCP for Unity (`com.coplaydev.unity-mcp`), Unity AI Assistant `2.11.0-pre.2`

---

## 2. Yazılım Mimarisi ve Tasarım Desenleri

### 2.1. Additive Sahne Yükleme Mimarisi (Additive Level Architecture)
Proje, klasik sahne geçişleri yerine kalıcı bir çekirdek sahne üzerine ek sahnelerin bindirildiği **Additive Scene** mimarisini benimser:

```
┌────────────────────────────────────────────────────────────────────────┐
│                              CoreScene                                 │
│  ├── Main Camera (CinemachineBrain)                                    │
│  ├── CinemachineCamera (Confiner2D)                                    │
│  ├── Player (PlayerController, PlayerInput, Rigidbody2D)               │
│  ├── Crosshair (VirtualCursor)                                         │
│  ├── Canvas (PauseMenu, AmmoUI, FinisherUI, LevelObjectiveUI)          │
│  └── ---MANAGERS---                                                    │
│       ├── LevelManager (Singleton)                                     │
│       ├── BulletPool (Singleton)                                       │
│       └── EffectPool (Singleton)                                       │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                        Additive Load / Unload
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│               Level_1 / Level_2 / Level_3 (Aktif Seviye)               │
│  ├── Tilemaps (Ground, Walls, Obstacles, Props)                        │
│  ├── SpawnPoint (Tag: "Respawn")                                       │
│  ├── LevelBounds (Collider2D -> Cinemachine Confiner)                  │
│  ├── LevelExitTrigger (Sonraki bölüme geçiş tetikleyicisi)             │
│  ├── Enemies (EnemyController, NavMeshAgent, Patroller)                │
│  ├── PatrolPoints (Tag: "PatrolPoint")                                 │
│  └── Environment (Doors, ProximityLamps)                               │
└────────────────────────────────────────────────────────────────────────┘
```

#### Seviye Geçiş Protokolü (`LoadLevelRoutine`):
1. **Karartma:** UI üzerinden siyah perde indirilir (`FadeSequence(0f, 1f)`).
2. **Fizik & Havuz Temizliği:** Oyuncu hızı sıfırlanır, `EffectPool.ClearAllEffects()` ve `BulletPool.ClearAllBullets()` çağrılarak önceki seviyeden kalan kan ve mermiler temizlenir.
3. **Sahne Boşaltma:** `CoreScene` hariç bellekteki tüm seviye sahneleri `SceneManager.UnloadSceneAsync` ile bellekten atılır.
4. **Yeni Seviyeyi Ekleme:** Hedef seviye `LoadSceneMode.Additive` ile yüklenir ve `SetActiveScene` yapılır.
5. **Konumlandırma:** Oyuncu yeni seviyedeki `Respawn` etiketli noktaya ışınlanır.
6. **Kamera Sınırları:** Cinemachine Confiner2D, yeni sahnedeki `LevelBounds` bileşeninin collider'ına bağlanarak sınır önbelleği sıfırlanır (`InvalidateBoundingShapeCache()`).
7. **Aydınlanma:** Siyah perde kaldırılarak oyun akışı başlatılır (`FadeSequence(1f, 0f)`).

---

### 2.2. Nesne Havuzu Deseni (Object Pooling)
Yüksek atış hızına sahip silahlarda ve yoğun kan efektlerinde çöp toplayıcı (Garbage Collector) duraksamalarını önlemek için kalıcı nesne havuzları kullanılır:
* **`BulletPool`:** Mermileri kuyrukta (`Queue<Bullet>`) tutar. `GetBullet(pos)` ile sahnede etkinleştirir; ekran dışına çıkınca veya duvara çarpınca `ReturnBullet` ile havuza iade eder.
* **`EffectPool`:** Sözlük tabanlı (`Dictionary<string, Queue<GameObject>>`) çoklu efekt yöneticisidir. `BackBlood`, `FrontBlood` ve `ObstacleHit` efektlerini boyut kısıtlamalı havuzlarda döndürür.

---

### 2.3. Olay Odaklı (Event-Driven) Arayüz İletişimi
* `Weapon` sınıfı mermi harcandığında veya şarjör doldurulduğunda `OnPlayerAmmoChanged(current, max)` statik olayını tetikler.
* `AmmoUI`, `OnEnable` sırasında bu olaya abone olur ve `OnDisable` sırasında aboneliği iptal ederek sahne geçişlerinde bellek sızıntısını (memory leak) önler.
* UI doğrudan oyuncu bileşenlerini her karede `Find` veya `GetComponent` ile sorgulamak yerine yalnızca veri değiştiğinde kendini günceller.

---

### 2.4. Kesintisiz Çarpışma Tespiti (Continuous Collision Detection)
* Yüksek hızlı 2D mermilerin fizik güncellemeleri arasında duvarlardan veya düşmanlardan atlamasını önlemek amacıyla `Bullet.cs` içerisinde `Physics2D.LinecastAll` uygulanır:
  ```csharp
  RaycastHit2D[] hits = Physics2D.LinecastAll(_previousPosition, transform.position);
  ```
* Bu sayede mermi hızı ne olursa olsun iki kare arasındaki rota eksiksiz taranır.

---

## 3. Katmanlar ve Etiketler Dizini (Layers & Tags)

### 3.1. Physics Layers (Katmanlar)
| ID | Katman Adı | Kullanım Amacı |
| :---: | :--- | :--- |
| **0** | `Default` | Genel dünya nesneleri ve zemin |
| **1** | `TransparentFX` | Şeffaf görsel efektler |
| **2** | `Ignore Raycast` | Görüş ve raycast sorgularını yok sayan nesneler |
| **3** | `Obstacle` | Mermileri ve hareketi durduran katı duvarlar |
| **4** | `Water` | Standart Unity katmanı |
| **5** | `UI` | Canvas ve ekran arayüz elemanları |
| **6** | `GroundItems` | Yerde yatan silahlar ve toplanabilir eşyalar |
| **7** | `Particles` | Parçacık sistemleri |
| **8** | `ThrownItem` | Havada uçan fırlatılmış silahlar (düşmanı sersemletir) |
| **9** | `Entities` | Canlı düşmanlar |
| **10**| `Player` | Oyuncu gövdesi ve bacakları |
| **11**| `StunnedEntities`| Sersemlemiş, savunmasız düşmanlar (mermiler içlerinden geçebilir) |
| **12**| `Door` | Fiziksel hareketli kapılar |

### 3.2. Sorting Layers (Görsel Render Sırası)
2D derinlik karmaşasını önlemek için katı bir hiyerarşi tanımlanmıştır:
1. `Default` (Arka plan)
2. `Props` (Zemin dekorları, halılar)
3. `Corpses` (Yerdeki kalıcı cesetler)
4. `BloodParticles` (Zemindeki kan birikintileri)
5. `StunnedEntities` (Yere yığılmış sersem düşmanlar)
6. `GroundItems` (Yerdeki silahlar)
7. `Entities` (Ayakta devriye gezen düşmanlar)
8. `WeaponBullet` (Havada uçuşan mermiler)
9. `Player` (Oyuncu karakteri)
10. `Obstacle` (Ön duvarlar ve engeller)
11. `Lamps` (Tavan lambaları ve ışık kaynakları)
12. `HitParticles` (Darbe ve kıvılcım efektleri)
13. `Crosshair` (En üstte duran sanal nişangah)

### 3.3. Tags (Etiketler)
* `Respawn`: Oyuncunun seviye başladığında spawn olacağı boş GameObject.
* `Wall`: Mermilerin çarptığında yok olup toz efekti çıkaracağı duvarlar.
* `Enemy`: Düşman nesneleri.
* `PatrolPoint`: AI devriye rotaları için odaya yerleştirilen referans noktaları.
* `Player`: Oyuncu ana nesnesi.
* `MainCamera`: Cinemachine tarafından kontrol edilen ana kamera.

---

## 4. Script Envanteri ve Sınıf Sorumlulukları

```
Assets/Scripts/
├── Bullet.cs                     # CCD balistik hareketi, duvar ve hasar çarpışmaları
├── DifficultyManager.cs          # Easy/Normal/Hard zorluk ayarları, PlayerPrefs kalıcılığı
├── IDamageable.cs                # Hasar alabilen varlık arayüzü (TakeDamage metodu)
├── LevelBounds.cs                # Cinemachine Confiner2D için oda sınır collider'ı
├── LevelExitTrigger.cs           # Düşmanlar bitince sonraki seviyeye geçiş tetiği
├── LevelManager.cs               # Additive sahne yükleyici, düşman sayacı, ekran karartıcı
├── PersistentAudioSource.cs      # Sahneler arası müziği kesintisiz çalan DontDestroyOnLoad bileşeni
├── VirtualCursor.cs              # Sanal nişangah, fare sınırlayıcı, Shift Look-Ahead kamera kaydırıcı
│
├── Enemy/
│   ├── EnemyController.cs        # Düşman canı, animasyonları, infaz durumu, sersemletme (Stun)
│   └── PlayerAwarenessController.cs # NavMesh devriye, kovalama, kayıp görüş takibi, silaha koşma
│
├── Environment/
│   ├── DoorController.cs         # HingeJoint2D kapı motoru, tekmeleme ve düşman sersemletme
│   ├── DropPistol.cs             # Yerdeki silah fiziği, fırlatılma torku, mermi hafızası
│   └── ProximityLamp.cs          # Canlı varlık ve görüş hattı duyarlı akıllı lamba
│
├── Menus/
│   ├── DifficultySettingController.cs # Ana menü zorluk seçici UI butonu ve renkleri
│   ├── MainMenuManager.cs        # Play, Settings, Quit ana menü işlevleri
│   ├── PauseMenuManager.cs       # ESC ile duraklatma, R ile yeniden başlatma kontrolü
│   └── SettingsMenuController.cs # SFX/Müzik ses kaydırıcıları ve AudioMixer desibel dönüşümü
│
├── Player/
│   ├── PlayerController.cs       # Gövde/bacak rotasyonu, yumruk, infaz, silah kuşanma/fırlatma
│   └── Weapon.cs                 # Ateş etme, saçılma, burst yaylımı, mermi düşümü, sesler
│
├── Pools/
│   ├── BulletPool.cs             # Mermi nesne havuzu
│   └── EffectPool.cs             # Kan ve darbe efektleri sözlük havuzu
│
└── UI/
    ├── AmmoUI.cs                 # Kalan ve maksimum mermi göstergesi (0'da kırmızı uyarısı)
    ├── FinisherUI.cs             # İnfaz stamina kaydırıcısı ve hazır durumu parlama efekti
    └── LevelObjectiveUI.cs       # Kalan düşman sayacı ve merdiven açılış bildirimi
```

---

## 5. Ses Mimarisi ve Miksaj (Audio Architecture)

* **AudioMixer:** `Assets/Audio/MainMixer.mixer`
* **Gruplar:** Master $\rightarrow$ Music / SFX
* **Lineer - Logaritmik Dönüşüm:** Slider değerleri (`0.0001` - `1.0`) Unity AudioMixer desibel ölçeğine dönüştürülür:
  $$\text{dB} = \log_{10}(\text{sliderValue}) \times 20$$
* **Ses Varlıkları:**
  * Silahlar: `Pistol-9mm`, `Shotgun-firing`, `Shotgun_rack`, `Rifle-22LR`, `Minigun-gunshot`
  * Dövüş: `Punch-04`, `Punch-Whoosh`, `Finisher-bone-break`
  * Çevre & UI: `Open_Door`, `AmbienceMusic-Ticking_in_the_Masonry`, `ThemeMusic-The_Narrow_Passage`

---

## 6. Geliştirme Geçmişi ve Evrim (Git History)

| Commit Hash | Commit Mesajı | İçerik ve Mimari İlerleme |
| :--- | :--- | :--- |
| `231fbab` | Initial commit | Depo ilk kurulumu |
| `a15975c` | PAYOUT Project Start | Temel proje dosyaları ve motor ayarları |
| `0739ed4` | PAYOUT-Update_01 | Temel oyuncu hareketi ve bacak rotasyonları |
| `549a138` | PAYOUT-Update_02 | Silah sistemi, mermi balistiği ve ateş mekanikleri |
| `b5dc855` | Payout-Update_03 | Temel düşman yapay zekası ve devriye rotaları |
| `5e719ce` | PAYOUT-Update_04 | Sanal imleç (`VirtualCursor`) ve nişan alma mekaniği |
| `cbcfdf0` | PAYOUT-Update_05 | Kan parçacıkları ve nesne havuzu (`EffectPool`, `BulletPool`) |
| `101d305` | PAYOUT-Update_06 | Kapı fiziği (`DoorController`) ve HingeJoint2D çarpmaları |
| `70ff16f` | PAYOUT-Update_07 | Silah fırlatma, yerden alma ve yumruk sistemi |
| `3c59412` | PAYOUT-Update_08 | İnfaz (Finisher) mekaniği, sersemletme ve kemik sesleri |
| `e235be7` | PAYOUT-Update_08.01 | İleri bakış (Shift Look-Ahead) ve kamera kaydırma |
| `3fa2bd8` | PAYOUT-Update_09 | Additive sahne mimarisi, CoreScene ve Level_1 / Level_2 entegrasyonu |
| `bf713c9` | PAYOUT-Update_10 | Zorluk yöneticisi (`DifficultyManager`), AudioMixer ayar menüleri ve Level_3 |
