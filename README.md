<!-- ============================================================
     HERO / LOGO BANNER
     Önerilen görsel: assets/banner.png  (1280×400 banner;
     koyu navy zemin, sunumdaki pixel-art şehir silüetiyle uyumlu)
============================================================ -->
<p align="center">
  <img src="assets/banner.png" alt="Core 15 Architect — Yapay Zekâ Destekli Prosedürel Bölüm ve Hikâye Motoru" width="100%"/>
</p>

<h1 align="center">Core 15 Architect</h1>
<p align="center"><i>Unity için Yapay Zekâ Destekli Prosedürel Bölüm ve Hikâye Motoru</i></p>

<p align="center">
  <a href="https://opensource.org/licenses/Apache-2.0"><img src="https://img.shields.io/badge/Lisans-Apache%202.0-blue.svg" alt="Lisans"/></a>
  <img src="https://img.shields.io/badge/Unity-2022.3%20LTS%2B-black?logo=unity" alt="Unity"/>
  <img src="https://img.shields.io/badge/C%23-9.0%2B-239120?logo=csharp" alt="C#"/>
  <img src="https://img.shields.io/badge/.NET-Standard%202.1-512BD4?logo=dotnet" alt=".NET"/>
  <img src="https://img.shields.io/badge/Durum-Aktif%20Geliştirme-success" alt="Durum"/>
  <a href="https://case15.beraeren.com"><img src="https://img.shields.io/badge/Canlı%20Demo-case15.beraeren.com-1D54E2?logo=vercel" alt="Canlı Demo"/></a>
</p>

<p align="center">
  <b>🌐 Web platformu:</b> <a href="https://case15.beraeren.com"><code>case15.beraeren.com</code></a>
</p>

---

Core 15 Architect, **Unity** için tasarlanmış yapay zekâ destekli bir bölüm tasarımı ve hikâye üretim sistemidir. **Wave Function Collapse (WFC)** algoritmasıyla **Büyük Dil Modelleri (LLM)** birlikte çalışır; akıllı prosedürel ortamlar, otomatik hikâye entegrasyonu ve dinamik içerik akışı sunar. Kısacası: tasarımcı senaryoyu yazar, motor sahneyi inşa eder.

<!-- ============================================================
     HERO DEMO GIF
     Önerilen görsel: assets/demo-hero.gif
     İçerik: 8-12 sn döngü — kullanıcı GDD yapıştırır,
     "Üret" butonuna basar, harita karo karo oluşur, ardından
     ipuçları/objeler doğru odalara yerleşir.
============================================================ -->
<p align="center">
  <img src="assets/demo-hero.gif" alt="Core 15'in bir GDD'den gerçek zamanlı bölüm ürettiği demo" width="85%"/>
</p>

---

## 📋 İçindekiler

- [🏗️ Mimari Genel Bakış](#️-mimari-genel-bakış)
  - [Mantıksal Üretim Akışı](#-mantıksal-üretim-akışı-ai--wfc)
  - [Fiziksel Sahne İnşa Akışı](#-fiziksel-sahne-i̇nşa-akışı-scene-builder)
- [🚀 Öne Çıkan Özellikler](#-öne-çıkan-özellikler)
- [🌟 Teknoloji Yığını](#-teknoloji-yığını)
- [📋 Sistem Gereksinimleri](#-sistem-gereksinimleri)
- [🛠️ Kurulum](#️-kurulum)
- [🗄️ Çekirdek Kurulum ve Lore Entegrasyonu](#️-çekirdek-kurulum-ve-lore-entegrasyonu)
- [🚀 Sistemi Çalıştırma](#-sistemi-çalıştırma)
- [💬 Örnek Üretim Akışları](#-örnek-üretim-akışları)
- [🌐 Web Platformu](#-web-platformu)
- [📂 Proje Yapısı](#-proje-yapısı)
- [📖 Çekirdek API Dokümantasyonu](#-çekirdek-api-dokümantasyonu)
- [🤝 Ekip](#-ekip)

---

## 🏗️ Mimari Genel Bakış

<!-- ============================================================
     MİMARİ DİYAGRAM
     Önerilen görsel: assets/core15-architecture.png
     İçerik: GDD → LLM → WFC → SceneBuilder akışını gösteren diyagram
============================================================ -->
<p align="center">
  <img src="assets/core15-architecture.png" alt="Core 15 Mimari Diyagramı" width="90%"/>
</p>

Sistem, temiz bir mimari sürdürmek ve Unity sahne hiyerarşisinin gereksiz dolmasını engellemek için **iki ayrı iş akışı** üzerine kuruludur:

---

### 🔹 Mantıksal Üretim Akışı (AI + WFC)

- **LLM Motoru** — Game Design Document (GDD) dosyasını okur; hangi odaların, ipuçlarının ve etkileşimli objelerin gerektiğine karar verir.
- **Wave Function Collapse (WFC)** — Unity hiyerarşisine hiç dokunmadan, matematiksel olarak geçerli bir 2D/3D iskelet ızgarası (`char[,]`) üretir.
- Tüm karar süreci saf mantıksal düzeyde işler; bu sayede performans ve esneklik en üst seviyede tutulur.

<!-- ============================================================
     MANTIKSAL AKIŞ DİYAGRAMI
     Önerilen görsel: assets/flow-logical.png
     İçerik: GDD ayrıştırma + WFC ızgara çıktısının yan yana diyagramı
============================================================ -->
<p align="center">
  <img src="assets/flow-logical.png" alt="Mantıksal Üretim Akışı" width="80%"/>
</p>

---

### 🔹 Fiziksel Sahne İnşa Akışı (Scene Builder)

- Mantıksal ızgarayı Unity Prefab'larına çevirir.
- Doğru rotasyonları otomatik uygular:
  - `-90f` → 2D Top-Down
  - `90f` → 3D
- Merkez ofsetlerini ve hiyerarşi düzenini (`Core15_GeneratedLevel`) sahne içinde temiz tutar.

<!-- ============================================================
     SCENE BUILDER EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/scene-builder.png
     İçerik: Unity Editor'de Core15_GeneratedLevel hiyerarşisi ve
              viewport'ta üretilen 3D sahne
============================================================ -->
<p align="center">
  <img src="assets/scene-builder.png" alt="Mantıksal ızgaranın Unity prefab'larına dönüştürülmesi" width="85%"/>
</p>

> ⚠️ **Not:** Canvas Preview her zaman mantıksal akışta çalışır; sahne inşa edilmeden önce manuel düzenleme yapılabilir.

---

## 🚀 Öne Çıkan Özellikler

| Özellik | Açıklama |
|---|---|
| 🔄 **Patch'siz İçerik Güncelleme** | Bölümler JSON üzerinden güncellenir; oyuna yeni patch çıkarmaya gerek kalmaz. |
| 🧠 **Akıllı Hikâye Bağlama** | İpuçları ve objeler, AI Senaryo Ağacı'na göre mantıklı yerlere yerleştirilir. |
| 🎲 **Dinamik Etkileşim Havuzu** | Çoklu kategoriye göre prefab yönlendirmesi (Hikâye, Bulmaca, Çevre). |
| 🎨 **AAA Editör Canvas** | 3 sekmeli iş akışı: **Asset Config** · **Design Canvas** · **Export & Story**. |
| 📐 **Perspektif Bağımsız** | Aynı motordan Top-Down 2D, Side-Scroller 2D ve serbest 3D çıktısı. |

<!-- ============================================================
     EDİTÖR CANVAS — 3 SEKME GENEL GÖRÜNÜM
     Önerilen görsel: assets/editor-canvas-tabs.png
     İçerik: 3 sekmenin yan yana birleştirilmiş ekran görüntüsü
              (Asset Config | Design Canvas | Export & Story)
============================================================ -->
<p align="center">
  <img src="assets/editor-canvas-tabs.png" alt="Üç sekmeli AAA Editör Canvas" width="100%"/>
</p>

---

## 🌟 Teknoloji Yığını

### 🎮 Çekirdek Framework

- Unity **2022.3 LTS+**
- C# **9.0+**

### 🧠 Yapay Zekâ ve Algoritmalar

- LLM Entegrasyonu (API'den bağımsız)
- Wave Function Collapse (WFC)

### 📄 Veri Yönetimi

- JSON Parser
- GDD Engine (`.txt` / `.md` ayrıştırma)

---

## 📋 Sistem Gereksinimleri

### Yazılım

- Unity Editor: `2022.3.x+`
- .NET Standard: `2.1`

### Performans

- RAM: **en az 8 GB (16 GB önerilir)**

> ⚠️ **500×500** karoyu aşan haritalarda kare kayıpları yaşanabilir.

---

## 🛠️ Kurulum

### 1. Repoyu Klonlayın

```bash
git clone https://github.com/kullaniciadi/Core15Architect.git
cd Core15Architect
```

### 2. Unity Projesine Ekleme

`Core15/` klasörünü şuraya kopyalayın:

```
Assets/
```

Klasör yapısının aşağıdaki gibi olduğundan emin olun:

```
Assets/Prefabs/
Assets/Core15_Templates/
```

<!-- ============================================================
     KURULUM / KLASÖR YAPISI EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/install-structure.png
     İçerik: Unity Project penceresinde içe aktarılmış klasörler
============================================================ -->
<p align="center">
  <img src="assets/install-structure.png" alt="İçe aktarma sonrası Unity Project penceresi" width="55%"/>
</p>

### 3. Ortam Değişkenleri

Yapılandırma dosyasını oluşturun:

```env
LLM_API_KEY=anahtariniz_buraya
LLM_ENDPOINT=https://api.saglayicim.com/v1/completions
DEBUG_LOGS=True
```

---

## 🗄️ Çekirdek Kurulum ve Lore Entegrasyonu

### 📖 GDD Bağlama

- **Asset Config** sekmesine gidin
- Bir `.txt` veya `.md` dosyası atayın
- LLM bu dosyayı bağlam olarak kullanacaktır

<!-- ============================================================
     ASSET CONFIG SEKMESİ EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/tab-asset-config.png
     İçerik: GDD yüklenmiş ve prefab havuzu görünür durumdaki sekme
============================================================ -->
<p align="center">
  <img src="assets/tab-asset-config.png" alt="Asset Config sekmesi — GDD bağlama ve prefab havuzu" width="80%"/>
</p>

---

### 🧱 Asset Havuzu Yapılandırması

- Statik prefab'ları tarayın
- Dinamik objeleri kategorilere ayırın

Örnek:

```yaml
Kategori: Evidence
Prefab:   Blood_Stain_01
```

---

## 🚀 Sistemi Çalıştırma

### 1. Görsel Parametreleri Ayarlayın

**Design Canvas** sekmesini açın ve şunları belirleyin:

- Kamera açısı (Camera View)
- Oda boyutu (Genişlik × Yükseklik)
- Duvar kalınlığı

<!-- ============================================================
     DESIGN CANVAS EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/tab-design-canvas.png
     İçerik: Düzenlenebilir WFC ızgara önizlemesi
============================================================ -->
<p align="center">
  <img src="assets/tab-design-canvas.png" alt="Design Canvas sekmesi — interaktif WFC önizlemesi" width="80%"/>
</p>

---

### 2. Temel Haritayı Üretin

- **Generate Base Map** butonuna tıklayın
- WFC ızgarayı oluşturur
- Gerekirse manuel olarak düzenleyin

<!-- ============================================================
     WFC ÜRETİM GIF
     Önerilen görsel: assets/wfc-generation.gif
     İçerik: 4-6 sn döngü — WFC algoritmasının karoları çökertmesi
============================================================ -->
<p align="center">
  <img src="assets/wfc-generation.gif" alt="Wave Function Collapse temel harita üretiyor" width="70%"/>
</p>

---

### 3. Sahneyi İnşa Edin

**Export & Story** sekmesine geçin ve şunları gözden geçirin:

- Mini-harita
- Senaryo Ağacı

Ardından şu butona tıklayın:

```
BUILD TO UNITY SCENE
```

<!-- ============================================================
     EXPORT & STORY SEKMESİ EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/tab-export-story.png
     İçerik: Senaryo ağacı + mini-harita + öne çıkmış BUILD butonu
============================================================ -->
<p align="center">
  <img src="assets/tab-export-story.png" alt="Export & Story sekmesi — senaryo ağacı ve sahne inşa tetikleyicisi" width="80%"/>
</p>

---

## 💬 Örnek Üretim Akışları

### 🌙 Gece Yarısı Olayı

**Girdi (Lore):**

```
Zengin bir tüccarın malikânesi. Yatak odasında bir hırsızlık olayı yaşandı.
```

**Sistem Çıktısı:**

1. WFC şu odaları üretir:
   - Koridor
   - Salon
   - Ana Yatak Odası
2. LLM senaryoyu kurar:
   - Kırık Cam → Yatak Odası Penceresi
   - Çamurlu Ayak İzi → Koridor

<!-- ============================================================
     ÖRNEK 1 ÇIKTI
     Önerilen görsel: assets/example-midnight.png
     İçerik: Üretilmiş malikânenin top-down görüntüsü;
              ipuçları (cam, ayak izi) işaretlenmiş halde
============================================================ -->
<p align="center">
  <img src="assets/example-midnight.png" alt="'Gece Yarısı Olayı' örneği — ipucu yerleşimi ile birlikte" width="80%"/>
</p>

---

### 🧪 Terk Edilmiş Laboratuvar

**Girdi (Lore):**

```
Yer altı araştırma tesisi. Elektrik kesik, güvenlik bölgesi delinmiş.
```

**Sistem Çıktısı:**

1. Dar koridorlar ve karantina odaları
2. Senaryo:
   - Devre Dışı Bırakılmış Konsol → Güvenlik Odası
   - Pençe İzleri → Karantina Kapısı

<!-- ============================================================
     ÖRNEK 2 ÇIKTI
     Önerilen görsel: assets/example-lab.png
     İçerik: Karanlık laboratuvarın 3D oyun-içi ekran görüntüsü
============================================================ -->
<p align="center">
  <img src="assets/example-lab.png" alt="'Terk Edilmiş Laboratuvar' örneği — 3D modda" width="80%"/>
</p>

---

## 🌐 Web Platformu

Tarayıcı tabanlı eşlikçi platform: **[case15.beraeren.com](https://case15.beraeren.com)**.
Tasarımcılar Unity'yi açmadan önce senaryolarını web üzerinde prototipleyebilir; ardından tek tıkla Unity sahnesine aktarabilir.

**Web platformu özellikleri:**

- 🧾 Canlı LLM önizlemeli, tarayıcıdan GDD editörü
- 🗺️ Unity kurulumu gerektirmeyen interaktif harita prototipleme
- 📤 Unity eklentisine hazır JSON paketi olarak dışa aktarma
- 👥 Ekip içi inceleme için paylaşılabilir senaryo bağlantıları

<!-- ============================================================
     WEB PLATFORMU EKRAN GÖRÜNTÜSÜ
     Önerilen görsel: assets/web-platform.png
     İçerik: case15.beraeren.com'un tam ekran görüntüsü;
              solda GDD editörü, sağda canlı harita önizlemesi
============================================================ -->
<p align="center">
  <a href="https://case15.beraeren.com">
    <img src="assets/web-platform.png" alt="Case 15 web platformu — case15.beraeren.com" width="90%"/>
  </a>
</p>

> 💡 Web uygulaması ve Unity eklentisi aynı JSON şemasını paylaşır; tarayıcıda kurduğunuz herhangi bir senaryo, tek tık ötede oynanabilir bir Unity sahnesidir.

---

## 📂 Proje Yapısı

```
Assets/
├── Core15/
│   ├── Editor/
│   │   ├── Core15ArchitectWindow.cs
│   │   ├── WFCGenerator.cs
│   │   └── LLMConnector.cs
│   ├── Models/
│   │   ├── PropInstance.cs
│   │   └── ScenarioTree.cs
│   └── Resources/
├── Prefabs/
└── Core15_Templates/
```

---

## 📖 Çekirdek API Dokümantasyonu

### `GenerateBaseMap`

```csharp
GenerateBaseMap(int width, int height, EnvironmentType type)
```

- **Döner değer:** `char[,]`
- Wave Function Collapse kullanarak mantıksal ızgara iskeleti üretir.

---

### `BuildToUnityScene`

Prefab'ları örnekler ve şu işlemleri yapar:

- Koordinat dönüşümü
- Rotasyon (`-90f` → 2D, `90f` → 3D)

---

### `BindAIScenario`

```csharp
BindAIScenario(string gddContext)
```

- GDD bağlamını LLM'e gönderir
- `ScenarioTree` döndürür

---

## 🤝 Ekip

<!-- ============================================================
     EKİP FOTOĞRAFLARI (opsiyonel)
     Önerilen görsel: assets/team.png  VEYA bireysel avatarlar
     İçerik: Sunum "Ekibimiz" slaytındaki vesikalık fotoğraflar
============================================================ -->

| Avatar | İsim | Rol |
|---|---|---|
| <img src="assets/avatar-emre.png" width="64"/> | **Emre Özen** | Kurucu / Proje Lideri — Oyun yönetimi ve proje koordinasyonu |
| <img src="assets/avatar-bera.png" width="64"/> | **Bera Eren Tutkun** | Kurucu / LLM ve AI Uzmanı — Hikâye kurgusu, vaka tasarımı, LLM entegrasyon mimarisi |
| <img src="assets/avatar-samet.png" width="64"/> | **Samet Yılmaz** | Kurucu / Teknik Geliştirme — Unity, C# OOP mimarisi, PCG ve tilemap sistemleri |

---

<p align="center">
  <sub>☕ ve Wave Function Collapse ile geliştirildi · <a href="https://case15.beraeren.com">case15.beraeren.com</a></sub>
</p>
