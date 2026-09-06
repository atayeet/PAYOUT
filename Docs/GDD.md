# PAYOUT — Game Design Document (GDD)

---

## 1. Yönetici Özeti (Executive Summary)

* **Oyun Başlığı:** PAYOUT
* **Tür:** Hızlı Tempolu 2D Low-Res Pikselli Top-Down Shooter (Fast-Paced 2D Top-Down Shooter)
* **İlham Kaynakları:** *Hotline Miami*, *Ape Out*, klasik neo-noir ve retro arcade twin-stick nişancı oyunları.
* **Platform:** PC (Windows / Standalone)
* **Hedef Kitle:** Hızlı reflekslere dayalı, taktiksel oda temizleme mekaniklerini, yüksek ölümcüllüğü ve retro piksel estetiğini seven aksiyon oyuncuları.
* **Motor ve Teknoloji:** Unity 6 (`6000.4.11f1`), Universal Render Pipeline (URP 2D), Cinemachine 3, NavMeshPlus (2D NavMesh), Unity New Input System.

---

## 2. Çekirdek Tasarım İlkeleri (Core Pillars)

1. **Ölümcül ve Cezalandırıcı Oynanış (Lethality & Fragility):**
   * Hem oyuncu hem de düşmanlar (istisnai durumlar hariç) tek mermi veya sert darbeyle etkisiz hale gelir.
   * Hata affetmeyen, yüksek risk/yüksek ödül dengesi.
2. **Akış Hissi ve Hızlı İntibak (Flow State & Fast Pacing):**
   * Ölüm bir son değil, anlık döngünün parçasıdır. `R` tuşuna basıldığı anda sahne ve oyuncu beklemeden yeniden başlar.
   * Çatışmalar saniyeler içinde başlar ve biter; duraksama olmaksızın sürekli hareket ve karar alma gerektirir.
3. **Doğaçlama Taktikler ve Çevresel Fırsatçılık (Improvisational Combat):**
   * Merminiz bittiğinde düşmana yaklaşabilir, yumruk atabilir, elinizdeki boş silahı kafasına fırlatarak sersemletebilir ve düşenin silahını kapabilirsiniz.
   * Kapıları tekmeleyerek arkasındaki düşmanları ezmek veya akıllı ışıklandırmaları avantaja çevirmek oyunun temel zenginliğidir.
4. **Acımasız ve Tatmin Edici İnfazlar (Brutal Finishers):**
   * Sersemletilmiş düşmanlara yapılan yakın dövüş infazları, oyuncuya hem taktiksel avantaj hem de görsel/işitsel tatmin sağlar.

---

## 3. Çekirdek Oynanış Döngüsü (Core Gameplay Loop)

```
[Odaya / Seviyeye Giriş]
        │
        ▼
[Keşif ve Tehdit Analizi (Shift ile İleri Bakış)]
        │
        ▼
[Çatışma Başlatma: Ateş Açma / Kapı Tekmeleme / Silah Fırlatma]
        │
        ├──► (Başarısız) ──► [Anında Ölüm] ──► [R ile Hızlı Yeniden Doğuş]
        │
        ▼ (Başarılı)
[Sersemletme (Stun) & İnfaz (Finisher) ile Stamina Yenileme]
        │
        ▼
[Yerdeki Yeni Silahları Toplama & Cephane Yönetimi]
        │
        ▼
[Bölgedeki Tüm Düşmanları Temizleme (Objective: Cleared)]
        │
        ▼
[Çıkış Noktasına (Merdivenler / Exit Trigger) Ulaşma & Sonraki Seviyeye Geçiş]
```

---

## 4. Karakter Mekanikleri ve Kontroller (Player Mechanics)

### 4.1. Bağımsız Gövde-Bacak Mimarisi (Decoupled Movement)
* **Bacaklar (Legs):** `WASD` girdisi doğrultusunda dünya ekseninde hareket eder ve hareket vektörüne göre yumuşakça döner.
* **Gövde (Torso):** Hareketi takip etmez; fare imlecinin dünya pozisyonuna (`VirtualCursor`) kilitlenir. Bu sayede geri geri kaçarken ileriye ateş etmek veya çapraz köşeleri taramak mümkündür.

### 4.2. Sanal İmleç ve İleriye Bakış (VirtualCursor & Look-Ahead)
* **Sanal İmleç:** Sistem faresi gizlenir ve kısıtlanır. Dünya üzerinde oyuncu merkezli belirli bir mesafe çemberinde (`_maxAimDistance`) hareket eden sanal bir nişangah oluşturulur.
* **İleriye Bakış (Shift Mekaniği):**
  * Oyuncu `Shift` (Sol veya Sağ) tuşuna bastığında nişangah menzili genişler (`_lookAheadMaxAimDistance`).
  * Cinemachine kamerası oyuncu ile imleç arasındaki orta noktaya yumuşakça kayar (`_cameraFollowSpeed`).
  * Oyuncunun ekran sınırlarına hapsolması (`PreventPlayerGoingOffScreen`) geçici olarak esnetilir; böylece bir sonraki koridor veya oda önceden taranabilir.

### 4.3. Yakın Dövüş ve Yumruk Sistemi (Punch System)
* Silahsız durumdayken **Sol Tık** yumruk atar.
* **Sol/Sağ El Alternatifi:** Her vuruşta karakter sırayla sağ ve sol yumruk animasyonlarını (`PlayerTorsoIdlePunchRight` / `PlayerTorsoIdlePunchLeft`) oynatır.
* **Sersemletme (Stun):** Yumruk isabet eden düşman hasar alıp ölmez; geriye savrulur (`_punchKnockbackForce`), silahını yere düşürür ve `1.5` saniye boyunca savunmasız kalır.
* Boşa atılan yumruklar hava vuruş sesi (`Punch-Whoosh`), isabet eden vuruşlar ise tok darbe sesi (`Punch-04`) üretir.

### 4.4. İnfaz Mekaniği (Finisher / Execution System)
* **Koşul:** Düşmanın sersemletilmiş (`IsCurrentlyStunned`) olması ve oyuncunun Finisher Stamina barının `%100` dolu olması.
* **Başlatma:** Sersemlemiş düşmana yaklaşılıp `Space (Boşluk)` tuşuna basıldığında tetiklenir.
* **Uygulama:**
  * Oyuncu düşmanın üzerine kilitlenir, hareket ve fizik durur.
  * Karakterler özel infaz duruşuna geçer (`PlayerTorsoFinisherReady`, `EnemyBeingFinished`).
  * Oyuncu **Sol Tık** yaptığında infaz gerçekleştirilir (`ExecuteFinisherAction`):
    * Kemik kırılma sesi (`Finisher-bone-break`) çalar.
    * Ön kan püskürme efekti (`FrontBlood`) patlar.
    * Düşman kalıcı olarak ölür (`EnemyKnocked` animasyonu ve `Corpses` katmanı).
* **Stamina Yönetimi:** İnfaz yapıldığında stamina sıfırlanır. Zamanla pasif olarak yenilenir ve düşman öldürüldükçe bonus stamina kazanılır (değerler zorluk seviyesine bağlıdır).

### 4.5. Silah Fırlatma ve Yerden Alma (Weapon Throw & Pickup)
* **Sağ Tık (Silahlıyken):** Eldeki silahı nişan alınan yöne fırlatır (`_throwForce`).
  * Fırlatılan silah fiziksel bir mermiye dönüşür (`ThrownItem` katmanı, torklu dönüş hareketi).
  * Fırlatılan silah düşmana çarparsa onu sersemletir (`Stun`) ve silahını düşürür.
* **Sağ Tık (Silahsızken):** Çevredeki (`_pickupRadius = 1.5f`) en yakın yerdeki silahı kuşanır.
* **Cephane Korunumu:** Silah fırlatıldığında içindeki kalan mermi sayısı hafızada tutulur; yerden tekrar alındığında mermi eksilmez veya sihirli şekilde yenilenmez.

### 4.6. Kontrol Haritası (Keybindings)

| Tuş | Eylem | Açıklama |
| :--- | :--- | :--- |
| **W, A, S, D** | Hareket | Oyuncunun bacaklarını hareket ettirir |
| **Fare Hareketi** | Nişan Alma | Gövdeyi ve sanal imleci (`VirtualCursor`) yönlendirir |
| **Sol Tık** | Ateş / Yumruk | Silah varsa ateş eder, yoksa sırayla sağ/sol yumruk atar |
| **Sağ Tık** | Fırlat / Al | Silah varsa fırlatır (sersemletir), yoksa yerdeki silahı alır |
| **Space (Boşluk)** | İnfaz Hazırlığı | Sersemlemiş düşmanın üstüne kilitlenir (Stamina tam ise) |
| **Sol Tık (İnfazda)**| İnfazı Gerçekleştir| Düşmanın kemiklerini kırarak anında yok eder |
| **Sol / Sağ Shift** | İleriye Bakış | Kamerayı ve imleci ileri kaydırarak keşif yapmayı sağlar |
| **R** | Hızlı Yeniden Başlat | Ölüm durumunda mevcut seviyeyi baştan başlatır |
| **ESC** | Duraklat / Menü | Pause menüsünü açar/kapatır |

---

## 5. Silahlar ve Balistik Sistemi (Arsenal & Ballistics)

| Silah Tipi | Atış Tipi | Kapasite | Atış Oranı | Saçılma / Özel Mekanik |
| :--- | :--- | :--- | :--- | :--- |
| **Pistol** | Yarı Otomatik | 15 Mermi | 0.20 sn | Tek mermi, yüksek isabet, standart tabanca sesi |
| **Shotgun** | Saçmalı Pompalı | 6 Mermi | 0.80 sn | Tek tetikte 6 saçma, -15° ile +15° arası açısal dağılım |
| **Rifle** | 3'lü Seri (Burst) | 30 Mermi | 0.40 sn | Tek tetikte 0.1 sn aralıkla 3 mermilik seri yaylım ateşi |
| **Minigun** | Tam Otomatik | 100 Mermi | 0.08 sn | Yüksek mermi debisi, kesintisiz yaylım ateşi |

### Balistik Mantığı (CCD & Raycast-Linecast)
* Mermiler havuzdan (`BulletPool`) çağrılır.
* Yüksek hızlı 2D mermilerin duvarlardan veya karakterlerden atlamasını (tunneling) önlemek için her karede `Physics2D.LinecastAll(_previousPosition, transform.position)` ile kesintisiz çarpışma tespiti uygulanır.
* Duvarla temas anında `ImpactEffect` üretilir; `IDamageable` taşıyan varlığa temas anında tek atışta hasar verilir ve mermi havuza geri döner.
* Merminin duvarın arkasında doğmasını engellemek için namlu ucundan raycast kontrolü yapılır.

---

## 6. Düşman Yapay Zekası (Enemy AI Architecture)

Düşmanlar `EnemyController` ve `PlayerAwarenessController` bileşenleriyle yönetilen iki katmanlı bir hiyerarşiye sahiptir. Navigasyon için 2D NavMesh (`NavMeshPlus`) kullanılır.

```
                  ┌──────────────┐
                  │     Idle     │
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
       ┌─────────►│    Patrol    │◄─────────┐
       │          └──────┬───────┘          │
       │                 │ Oyuncuyu Gördü   │
       │                 ▼                  │
       │          ┌──────────────┐          │
       │          │    Alert     │          │
       │          └──────┬───────┘          │
       │                 │ Tepki Süresi     │
       │                 ▼ Doldu            │
       │          ┌──────────────┐          │
       │          │    Chase     │          │
       │          └──────┬───────┘          │
       │                 │ Görüş Kayboldu   │
       │                 ▼                  │
       │          ┌──────────────┐          │
       │          │ Investigate  │──────────┘
       │          │   LastSeen   │
       │          └──────────────┘
       │
       │ (Eğer Silahsız Kaldıysa)
       ├────────────────────────────────┐
       ▼                                ▼
┌──────────────┐                 ┌──────────────┐
│ SearchWeapon │                 │     Flee     │
│ (Yerdeki     │                 │ (Oyuncudan   │
│  Silaha Koş) │                 │  Ters Yöne   │
└──────────────┘                 │   Kaçış)     │
                                 └──────────────┘
```

### Yapay Zeka Durumları (AI States):
1. **Idle:** Sabit bekleyen nöbetçiler.
2. **Patrol:** Odadaki `PatrolPoint` nesneleri arasında rastgele rotalar çizerek devriye gezer.
3. **Alert:** Oyuncuyu ilk fark ettiğinde afallama ve tepki verme süresi (Zorluğa göre 0.15s - 1.0s).
4. **Chase:** Oyuncuyu hedefe koyup son sürat kovalar; menzile girdiğinde (`_shootingRange = 7f`) silahını ateşler.
5. **InvestigateLastSeen:** Oyuncu duvar arkasına kaçıp görüş hattı (Line-of-Sight) kesildiğinde, en son görüldüğü noktaya gidip etrafı araştırır.
6. **SearchWeapon:** Düşman yumruk, fırlatılan silah veya kapı çarpmasıyla sersemletilip silahını düşürdüğünde; ayılınca derhal yerdeki en yakın silaha koşar.
7. **Flee:** Silahsız düşman yakınında silah bulamazsa ve oyuncu yaklaşırsa can havliyle ters yöne kaçar.

---

## 7. Çevre ve Etkileşim Tasarımı (Environment Interactivity)

### 7.1. Dinamik Kapılar (`DoorController`)
* Gerçekçi fizik motoru (`HingeJoint2D`) ile kontrol edilir.
* Karakterler kapıya çarptığında motor devreye girer ve kapıyı hızla açar (`_motorSpeed = 500f`).
* **Kapı Tekmesi (Door Kick Stun):** Açılan kapının kanadı arkasındaki düşmanlara çarparsa (`_hitRadius = 1.5f`), düşmanlar savrulur ve sersemletilir (`Stun`).
* Kapı belirli bir süre açık kaldıktan sonra otomatik olarak kapanır ve kilitlenir.

### 7.2. Akıllı Sensörlü Lambalar (`ProximityLamp`)
* Yalnızca çevre dekoru değil, taktiksel bir atmosfer unsurudur.
* Belirli bir yarıçaptaki (`_detectionRadius = 5f`) canlı varlıkları (Player veya Enemy) algılar.
* Duvarların arkasından ışığın tetiklenmesini önlemek için görüş hattı (Linecast) kontrolü yapar.
* Koridorda veya odada biri olduğunda lamba yanar; alan boşaldıktan sonra gecikmeyle söner.

### 7.3. Seviye Çıkışları ve İlerleme (`LevelExitTrigger` & `LevelObjectiveUI`)
* Her seviyede hedef: **Bölgedeki tüm düşmanları etkisiz hale getirmektir.**
* Düşmanların tamamı öldürülmeden çıkış kapısı/merdivenleri aktifleşmez.
* Kalan düşman sayısı ekranın sol üstünde gösterilir (`Enemies: X / Y`).
* Son düşman düştüğünde arayüzde *"STAIRS UNLOCKED / PROCEED TO EXIT"* bildirimi belirir.

---

## 8. Zorluk Matrisi ve Dengeleme (Difficulty Manager)

Oyuncuların beceri seviyelerine göre `DifficultyManager` üzerinden dinamik olarak ölçeklenen parametreler:

| Parametre | Kolay (Easy) | Normal | Zor (Hard) | Açıklama |
| :--- | :--- | :--- | :--- | :--- |
| **Düşman Reaksiyon Süresi (Alert)** | 1.00 sn | 0.50 sn | 0.15 sn | Oyuncuyu gördükten sonra ateşe başlama süresi |
| **Düşman Atış Hızı Çarpanı** | 1.60x (Yavaş) | 1.00x | 0.75x (Seri) | Düşmanların mermiler arasındaki bekleme süresi |
| **Finisher Stamina Dolum Hızı** | 15.0 / sn | 10.0 / sn | 6.67 / sn | Pasif enerji dolma hızı |
| **Öldürme Başına Stamina Ödülü** | +40 Stamina | +25 Stamina | +15 Stamina | Bir düşman öldüğünde kazanılan enerji |

---

## 9. Görsel ve İşitsel Tasarım (Audio & Visual Aesthetics)

### 9.1. Görsel Dil
* **Stil:** Low-Res Pixel Art, kontrastlı zemin ve duvar karoları, neon ve retro endüstriyel renk paleti.
* **Gore Efektleri:** Düşmanlar vurulduğunda arkalarından doğrusal kan sıçraması (`BackBlood`), infaz edildiğinde dairesel kan patlaması (`FrontBlood`) oluşur.
* **Cesetler:** Ölen tüm karakterler ve düşmanlar fizik çarpışmalarını kapatarak `Corpses` sorting layer'ına geçer ve zeminde kalıcı iz bırakır.

### 9.2. Ses Tasarımı ve Miksaj (AudioMixer)
* `MainMixer` altında iki bağımsız kanal: `MusicVolume` ve `SFXVolume`.
* Ses efektleri 2D/3D mekansallık ayarlarıyla (`spatialBlend`) konumlandırılmıştır:
  * Ateş sesleri ve patlamalar yarı-uzamsal (`0.5f`),
  * Düşman inlemeleri ve kapı gıcırtıları tam 3D uzamsal (`1.0f`),
  * Arayüz ve infaz kemik sesleri doğrudan kulaklık/ekran merkezlidir (`0.0f`).
