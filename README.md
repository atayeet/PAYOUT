# PAYOUT 💥

**Türkçe** · [English](README.en.md)

[![Unity 6](https://img.shields.io/badge/Unity-6000.4.11f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%202D-orange.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![C#](https://img.shields.io/badge/Language-C%23%2010.0-purple.svg?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-PC%20%2F%20Windows-lightgrey.svg?logo=windows)](https://microsoft.com/windows)
[![Genre](https://img.shields.io/badge/Genre-2D%20Top--Down%20Shooter-red.svg)](#)

> **PAYOUT**, yüksek tempolu, tek hatada ölüm cezası kesen, *Hotline Miami* ve klasik neo-noir aksiyon oyunlarından ilham alan **2D Low-Res Pixelated Top-Down Shooter** oyunudur.

---

## 📖 İçindekiler
- [Oyun Hakkında](#-oyun-hakkında)
- [Temel Oynanış Mekanikleri](#-temel-oynanış-mekanikleri)
- [Kontroller](#-kontroller)
- [Cephanelik](#-cephanelik-weapons)
- [Düşman Yapay Zekası](#-düşman-yapay-zekası-reactive-ai)
- [Zorluk Seviyeleri](#-zorluk-seviyeleri-difficulty-settings)
- [Dokümantasyon](#-dokümantasyon-proje-belgeleri)
- [Teknik Mimari ve Dosya Yapısı](#-teknik-mimari-ve-dosya-yapısı)
- [Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma-getting-started)
- [Lisans ve Telif Hakları](#-lisans--telif-hakları)

---

## 🎮 Oyun Hakkında

PAYOUT; hız, refleks, taktiksel doğaçlama ve acımasız şiddet üzerine inşa edilmiş bir oda temizleme simülasyonudur. Her oda bir bulmaca, her düşman saniyeler içinde çözülmesi gereken ölümcül bir tehdittir.

Tek bir mermiyle ölebilir, merminiz bittiğinde düşmana yumruk atabilir, elinizdeki boş tabancayı diğer düşmanın kafasına fırlatarak yere serebilir ve yerdeki pompalıyı kaparak odayı kan gölüne çevirebilirsiniz.

---

## ⚡ Temel Oynanış Mekanikleri

### 1. Bağımsız Gövde ve Bacak Kontrolleri (Decoupled Movement)
Oyuncunun bacakları dünya ekseninde `WASD` yönünde koşarken, üst gövde fare imlecine (`VirtualCursor`) kilitlenir. Geriye koşarken ileriye ateş edebilir veya köşe başlarında geri çekilerek koridorları tarayabilirsiniz.

### 2. İleriye Bakış (Shift Mekaniği - Look-Ahead)
`Shift` tuşuna basılı tutulduğunda:
* Sanal nişangahın oyuncudan uzaklaşma sınırı genişler (`7m` $\rightarrow$ `12m`).
* Cinemachine 2D kamerası oyuncu ile imleç arasındaki orta noktaya yumuşakça kayar.
* Ekran sınırı sınırlaması geçici olarak gevşetilerek bir sonraki odadaki tehditler önceden keşfedilebilir.

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

## 🕹️ Kontroller

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

## 📚 Dokümantasyon (Proje Belgeleri)

Projenin tüm detaylı teknik, tasarımsal ve mimari dokümanları repository içerisindeki [`Docs/`](Docs/) klasörü altında tutulmaktadır:

* 📄 **[GDD.md](Docs/GDD.md) — Oyun Tasarım Dokümanı (Game Design Document):**
  Oyunun vizyonu, felsefesi, çekirdek döngüsü, tüm silah balistikleri, yapay zeka durum makinesi şeması, seviye mekanikleri ve zorluk matrisini içerir.
* 📄 **[CONTEXT.md](Docs/CONTEXT.md) — Proje Bağlamı ve Teknik Mimari:**
  Unity 6 URP 2D ayarları, Additive sahne mimarisi (`CoreScene` + `Level_X`), nesne havuzları (`BulletPool`, `EffectPool`), katman matriksleri (13 Physics, 13 Sorting Layer) ve script envanteri.
* 📄 **[CODE_STYLE.md](Docs/CODE_STYLE.md) — Kodlama Standartları ve Prensipleri:**
  C# ve Unity clean architecture ilkeleri, isimlendirme kuralları, serileştirme prensipleri, yaşam döngüsü kuralları ve performans yönergeleri.
* 📄 **[GEMINI.md](Docs/GEMINI.md) — AI Asistan & LLM Yönergesi:**
  Proje üzerinde çalışacak yapay zeka modelleri için sistem talimatları, Unity MCP kullanım rehberi ve yeni silah/düşman/bölüm ekleme kontrol listeleri.

---

## 🏗️ Teknik Mimari ve Dosya Yapısı

* **Additive Sahne Düzeni:** `CoreScene` kalıcıdır (Kamera, Player, UI, Singleton yöneticileri). `Level_1`, `Level_2` ve `Level_3` additive olarak yüklenir ve seviye bitince bellekten atılır.
* **Nesne Havuzları (Object Pools):** `BulletPool` (Mermiler) ve `EffectPool` (Kan parçacıkları ve darbe efektleri) ile sıfır GC tahsisi.
* **Kesintisiz Çarpışma (CCD):** Hızlı mermiler için `Physics2D.LinecastAll` kontrolü.

```
PAYOUT/
├── Assets/
│   ├── Animations/           # Oyuncu, düşman ve silah animasyon kontrolcüleri
│   ├── Audio/                # MainMixer, ortam müzikleri, ateş ve infaz sesleri
│   ├── Prefabs/              # Silahlar, düşmanlar, kapılar, kan efektleri
│   ├── Scenes/               # CoreScene, MainMenu, Level_1, Level_2, Level_3
│   ├── Scripts/              # C# mantık ve mimari sınıfları
│   └── Sprites/              # Retro pixel-art karakter, çevre ve silah kaplamaları
└── Docs/                     # Proje tasarım ve mimari belgeleri
    ├── CODE_STYLE.md         # Kodlama standartları
    ├── CONTEXT.md            # Teknik altyapı ve mimari bağlam
    ├── GDD.md                # Kapsamlı oyun tasarım dokümanı
    └── GEMINI.md             # AI asistan ve geliştirici yönergesi
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

## 📜 Lisans & Telif Hakları
Bu proje [atayeet](https://github.com/atayeet) tarafından geliştirilmektedir. Tüm hakları saklıdır.
