# PAYOUT 💥

[![Unity 6](https://img.shields.io/badge/Unity-6000.4.11f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%202D-orange.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![C#](https://img.shields.io/badge/Language-C%23%2010.0-purple.svg?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-PC%20%2F%20Windows-lightgrey.svg?logo=windows)](https://microsoft.com/windows)
[![Genre](https://img.shields.io/badge/Genre-2D%20Top--Down%20Shooter-red.svg)](#)

> **PAYOUT**, yüksek tempolu, tek hatada ölüm cezası kesen, *Hotline Miami* ve klasik neo-noir aksiyon oyunlarından ilham alan **2D Low-Res Pixelated Top-Down Shooter** oyunudur.

---

## 🌐 English Summary (Executive Overview)

**PAYOUT** is an adrenaline-fueled, high-lethality 2D top-down shooter built on Unity 6 URP. Every bullet is deadly, reaction times are tested, and quick restarts keep the tension unbroken.

### Key Features (English)
* **Decoupled Combat Controls:** Legs navigate smoothly via `WASD` while the torso and aiming angle lock independently to a custom clamped virtual crosshair (`VirtualCursor`).
* **Tactical Look-Ahead (`Shift`):** Holding Shift extends aiming range and dynamically pans the Cinemachine camera ahead, enabling room scouting without getting trapped in viewport bounds.
* **Punch, Stun & Brutal Finishers:** Unarmed melee punches alternate between hands to knock down and stun foes. Trigger cinematic execution finishers (`Space` + `Left Click`) to snap bones, spray blood, and regenerate finisher stamina.
* **Throw & Scavenge Weapons:** Chuck your empty or loaded weapon with `Right Click` to knock enemies out cold, pick up any dropped gun from the floor, and retain exact ammo counts.
* **Reactive AI Architecture:** Enemies utilize 2D NavMesh navigation with multi-state awareness (`Patrol`, `Alert`, `Chase`, `InvestigateLastSeen`, `SearchWeapon`, `Flee`). If disarmed, they sprint toward the nearest dropped weapon or flee in panic.
* **Interactive Environment:** Kick doors open via physics motors (`HingeJoint2D`) to stun lurking guards, and utilize smart proximity lamps that react to living entities and line-of-sight.
* **Modular Additive Scene Architecture:** A persistent master `CoreScene` manages audio, cameras, HUD, and object pools, while individual levels (`Level_1`, `Level_2`, `Level_3`) load additively with seamless fade transitions.

### Default Keybindings (Quick Reference)
| Key | Action |
| :--- | :--- |
| **W, A, S, D** | Move Player Legs |
| **Mouse Aim** | Rotate Torso / Direct Virtual Crosshair |
| **Left Click** | Fire Equipped Weapon / Punch (if unarmed) / Execute Finisher |
| **Right Click** | Throw Held Weapon (stuns foes) / Pickup Weapon from Floor |
| **Space** | Initiate Execution Finisher on Stunned Enemy |
| **Left / Right Shift** | Look-Ahead (Extends aim & offsets camera forward) |
| **R** | Instant Quick Restart (after death) |
| **ESC** | Pause Menu / Settings |

---

## 🎮 Oyun Hakkında (Turkish Overview)

PAYOUT; hız, refleks, taktiksel doğaçlama ve acımasız şiddet üzerine inşa edilmiş bir oda temizleme simülasyonudur. Her oda bir bulmaca, her düşman saniyeler içinde çözülmesi gereken ölümcül bir tehdittir.

Tek bir mermiyle ölebilir, merminiz bittiğinde düşmana yumruk atabilir, elinizdeki boş tabancayı diğer düşmanın kafasına fırlatarak yere serebilir ve yerdeki pompalıyı kaparak odayı kan gölüne çevirebilirsiniz.

---

## ⚡ Temel Oynanış Mekanikleri

### 1. Bağımsız Gövde ve Bacak Kontrolleri
Oyuncunun bacakları dünya ekseninde `WASD` yönünde koşarken, üst gövde fare imlecine (`VirtualCursor`) kilitlenir. Geriye koşarken ileriye ateş edebilir veya köşe başlarında geri çekilerek koridorları tarayabilirsiniz.

### 2. İleriye Bakış (Shift Mekaniği - Look-Ahead)
`Shift` tuşuna basılı tutulduğunda:
* Sanal nişangahın oyuncudan uzaklaşma sınırı genişler (`7m` $\rightarrow$ `12m`).
* Cinemachine 2D kamerası oyuncu ile imleç arasındaki orta noktaya yumuşakça kayar.
* Ekran sınır sınırlaması geçici olarak gevşetilerek bir sonraki odadaki tehditler önceden keşfedilebilir.

### 3. Yakın Dövüş, Sersemletme ve İnfaz (Finisher)
* **Yumruk:** Silahsızken sol tık ile sırayla sağ ve sol yumruk atılır. İsabet alan düşman savrulur, silahını yere düşürür ve `1.5` saniye sersem kalır.
* **İnfaz (Finisher):** Sersemlemiş düşmana yaklaşılıp `Space` basıldığında infaz moduna geçilir. Sol tık yapıldığında kemik kırılma sesi (`Finisher-bone-break`) ve kan patlamasıyla düşman tek hamlede yok edilir.
* **Stamina:** İnfaz harcanınca stamina sıfırlanır; zamanla ve düşman öldürüldükçe yeniden dolar.

### 4. Silah Fırlatma ve Cephane Korunumu
* **Sağ Tık (Silahlı):** Eldeki silahı nişan alınan yöne fırlatır. Fırlatılan silah düşmana çarparsa onu sersemletir.
* **Sağ Tık (Silahsız):** Yerdeki silahı alır. Fırlatılan veya yerden alınan silahların mermi sayıları eksiksiz korunur.

### 5. Akıllı Çevre ve Etkileşim
* **Fiziksel Kapılar:** Kapıya çarparak açtığınızda motor hızıyla savrulan kapı kanadı arkasındaki düşmanları ezer ve sersemletir.
* **Sensörlü Lambalar:** Görüş hattı ve canlı varlık algılayan akıllı lambalar, odaya biri girdiğinde yanar, çıktığında gecikmeli olarak söner.

---

## 🔫 Cephanelik (Weapons)

| Silah | Tür | Mermi | Atış Hızı | Karakteristik |
| :--- | :--- | :---: | :---: | :--- |
| **Pistol** | Yarı Otomatik | 15 | Orta | Yüksek isabetli dengeli servis tabancası |
| **Shotgun** | Pompalı | 6 | Yavaş | Tek tetikte 6 saçma, yakın mesafede ölümcül yayılım |
| **Rifle** | Burst | 30 | Seri | Tek tetikte 0.1 sn arayla 3 mermilik yıkıcı yaylım |
| **Minigun** | Otomatik | 100 | Çok Seri | Yüksek atış hızlı mermi yağmuru |

---

## 🤖 Düşman Yapay Zekası (Reactive AI)

Düşmanlar 2D NavMesh (`NavMeshPlus`) üzerinden haritayı gezer ve çok durumlu bir yapay zeka makinesine sahiptir:
* **Patrol:** Odadaki devriye noktaları arasında gezinir.
* **Alert:** Oyuncuyu ilk fark ettiğinde tepki verme gecikmesi yaşar (Zorluk seviyesine göre `0.15s` - `1.0s`).
* **Chase:** Oyuncuyu kovalar ve menzile girdiğinde ateş açar.
* **InvestigateLastSeen:** Görüş hattı kaybolduğunda oyuncunun son görüldüğü noktayı teftiş eder.
* **SearchWeapon / Flee:** Silahı elinden alınan düşman, yakındaki yerdeki bir silaha koşar; bulamazsa can havliyle kaçar.

---

## ⚖️ Zorluk Seviyeleri (Difficulty Settings)

Oyun içi `DifficultyManager` ile dengelenen üç farklı zorluk modu:
* **Easy (Kolay):** Düşman tepki süresi `1.0s`, düşman atış hızı `%60` daha yavaş, stamina hızlı dolar.
* **Normal:** Standart dengeli aksiyon deneyimi.
* **Hard (Zor):** Düşman tepki süresi yalnızca `0.15s`, düşman atış hızı seri, stamina kazanımı kısıtlı.

---

## 🏗️ Teknik Mimari ve Dosya Yapısı

* **Additive Sahne Düzeni:** `CoreScene` kalıcıdır (Kamera, Player, UI, Singleton yöneticileri). `Level_1`, `Level_2` ve `Level_3` additive olarak yüklenir ve seviye bitince bellekten atılır.
* **Nesne Havuzları (Object Pools):** `BulletPool` (Mermiler) ve `EffectPool` (Kan parçacıkları ve darbe efektleri) ile sıfır GC tahsisi.
* **Kesintisiz Çarpışma (CCD):** Hızlı mermiler için `Physics2D.LinecastAll` kontrolü.

```
Assets/
├── Animations/           # Oyuncu, düşman ve silah animasyon kontrolcüleri
├── Audio/                # MainMixer, ortam müzikleri, ateş ve infaz sesleri
├── Prefabs/              # Silahlar, düşmanlar, kapılar, kan efektleri
├── Scenes/               # CoreScene, MainMenu, Level_1, Level_2, Level_3
├── Scripts/              # C# mantık ve mimari sınıfları
│   ├── Enemy/            # EnemyController, PlayerAwarenessController
│   ├── Environment/      # DoorController, DropPistol, ProximityLamp
│   ├── Menus/            # MainMenu, PauseMenu, Settings, Difficulty
│   ├── Player/           # PlayerController, Weapon
│   ├── Pools/            # BulletPool, EffectPool
│   └── UI/               # AmmoUI, FinisherUI, LevelObjectiveUI
└── Sprites/              # Retro pixel-art karakter, çevre ve silah kaplamaları
```

---

## 🚀 Kurulum ve Çalıştırma (Getting Started)

1. **Gereksinimler:**
   * **Unity Hub** ve **Unity 6000.4.11f1 (Unity 6 LTS)**.
   * Git (LFS desteği önerilir).
2. **Projeyi Klonlayın:**
   ```bash
   git clone https://github.com/atayeet/PAYOUT.git
   ```
3. **Unity ile Açın:**
   * Unity Hub üzerinden projeyi seçip `Unity 6000.4.11f1` sürümüyle projeyi açın.
4. **Oyunu Başlatın:**
   * `Assets/Scenes/MainMenu.unity` sahnesini açıp **Play** butonuna basın.
   * Doğrudan seviye test etmek için `Assets/Scenes/CoreScene.unity` sahnesinden başlatabilirsiniz.

---

## 📌 GitHub Repository Bilgileri (About & Topics)

GitHub sayfasındaki repository ayarlarını düzenlemek için önerilen hazır metinler:

* **Description (About Kutusu):**
  > Fast-paced, high-lethality 2D pixelated top-down shooter built with Unity 6 URP. Features Hotline Miami-inspired combat, decoupled movement, weapon throwing, brutal finishers, and reactive 2D NavMesh AI.

* **Topics / Etiketler:**
  `unity` `unity6` `pixel-art` `top-down-shooter` `hotline-miami-inspired` `csharp` `urp-2d` `game-development` `navmesh-2d` `indie-game` `action-game`

---

## 📜 Lisans & Telif Hakları
Bu proje [atayeet](https://github.com/atayeet) tarafından geliştirilmektedir. Tüm hakları saklıdır.
