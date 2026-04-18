# Case-15 Unity Projesi Genel Özeti

Bu doküman, çalışma alanında (workspace) yer alan **Case-15** isimli Unity projesinin mimarisini, ana bileşenlerini ve özelliklerini detaylı bir şekilde özetlemektedir.

## 1. Proje Hakkında Genel Bilgiler
- **Oyun Motoru:** Unity 3D (Proje 2D yapıda tasarlanmıştır)
- **Tür:** 2D Top-Down (Kuşbakışı)
- **Ana Mekanik:** **Wave Function Collapse (WFC)** algoritması kullanılarak "DeBroglie" kütüphanesi tabanlı rastgele ve prosedürel harita / kat planı oluşturma.
- **Dizin Yapısı:** Temel olarak proje scriptleri `Assets/Scripts` klasörüne, tasarımlar ise `Docs/` dizinine ayrılmıştır.

## 2. Prosedürel Harita Üretimi Mekanikleri (Dungeon & Room Generation)

Projenin en temel modülü harita ve odaların baştan inşa edilmesini sağlayan üretim sistemleridir.

### **DeBroglieGenerator.cs**
- Projenin en büyük ve en detaylı üretim betiğidir (yaklaşık 750 satır, WFC entegrasyonu).
- **Zemin Kuralları (DFS vs Hub):** 
  - **DFS Mode:** Derinlik öncelikli arama (Depth First Search) kurallarına dayanan, dallanan odalara sahip yapı üretir.
  - **Hub Mode:** Merkezi büyük bir odanın etrafında genişleyen, merkez tabanlı bir mimari inşa eder.
- **Tile (Kare) Adjacency:** XML üzerinden (`tiles.xml`) kare tiplerinin (Tile - duvar, zemin, vb.) yan yana gelme kuralları yüklenir ve ağırlıklarına göre DeBroglie nesne çözücüsüne verilir.
- **Glass, Doors ve Çıkışlar:** Kod, büyük zeminleri hesapladıktan sonra camların (Glass), kilitli kapıların (Doors) ve asıl geçiş yollarının (Passage) yerleştirimini de rastgele hesaplamalarla yapmaktadır.

### **ProceduralRoomGenerator2D.cs**
- Daha sade bir yapıda inşa edilmiş, Tilemap (Döşeme Haritası) üzerinde direkt olarak 2D döşeme boyayan alternatif bir Hub ve DFS harita üretecidir. Odaları ve koridorları direkt `Tilemap.SetTile` fonksiyonlarıyla doldurur.

## 3. Oynanış (Gameplay) ve Diğer Özellikler

### **PlayerTopDownMovement.cs**
- Karakterin serbestçe 8 yöne hareket edebildiği (`x` ve `y` vektörü) standart bir Rigidbody2D tabanlı hareket (movement) motorudur.
- Yeni **Input System** mimarisi kullanılarak hem Keyboard (WASD/Yön Tuşları) hem de Gamepad ile kontrol desteğine sahiptir.
- Local ve External hareket imkanı destekleyerek hareketleri devralabilmeyi sağlar (`InputMode`).

### **ComputerInteract.cs**
- Karakterin odalar içerisinde bulunan bilgisayar objeleri ile etkileşime girebilmesini sağlar.
- Karakter yaklaştığında ve `E` tuşuna bastığında bilgisayar arayüzü (UI) açılır ve oyuncunun hareketini kitlemek adına eventler (UnityEvent) tetikler.
- *Not:* Skript içerisinde "İleride buraya: `if(other.GetComponent<NetworkIdentity>().isLocalPlayer)` eklenecek" şeklinde yorumlar mevcuttur. Bu da projenin ileride **Multiplayer (Çok Oyunculu)** bir mimariye kavuşabileceği ihtimalini göstermektedir.

### **MapJSONExporter.cs** 
- Üretilen haritanın zemin, duvar, kapı ve diğer koordinatlarını veri analizine uygun olarak JSON formatında (`MapExport.json`) bilgisayara dışarı aktarmaktadır. Yapay zeka eğitimi (AIModels klasörü ile) ya da harita metadatalarının kaydedilmesinde kullanılmak amaçlı olabilir.

## 4. Kullanılan Kütüphaneler ve Bağımlılıklar
- **DeBroglie (WFC):** Tile bazlı dalga fonksiyonu çökmesi (Wave Function Collapse) kütüphanesidir. Prosedürel seviye üretiminin motorudur.
- **Unity Input System:** Yeni girdi yönetim paketidir.
- **TextMeshPro:** UI ve metinler için.

## 5. Detaylı Dizin Yapısı (Directory Tree)

Genel çalışma alanının en tepesinden (root) Unity çekirdeğine kadar olan dizin ağacının ve dosyaların amacı aşağıda listelenmiştir:

- **Root (Case-15 Ana Dizini)**
  - `.git/` & `.gitignore`: Versiyon kontrol (Git) dosyalarıdır. Unity için özel gereksiz (Library, Temp vb.) dosyaları yoksaymak üzere ayarlanmıştır.
  - `README.md`: Projenin başlangıç bilgisi veya kullanım kılavuzu için ayrılmış temel dokümantasyon dosyasıdır.
  - **AIModels/**: (Şu an boş). Projenin DeBroglie ile oluşturduğu haritalarda eğitilecek olası yapay zeka ajanlarının model ağırlıklarını barındırmak için düşünülmüş altyapı klasörüdür.
  - **Docs/**: Proje dokümanlarının saklandığı yerdir. `Proje_Ozeti.md` (şu an okuduğunuz) gibi dosyalar burada yaşar.
    - `AI_Docs/`: Yapay Zeka bot kurguları hakkında dizayn kuralları / notlar için kullanılır.
    - `DesignDocs/`: Oyun ve bölüm tasarım notlarını barındıran alt klasördür.
  - **Tests/**: Oyun içi kodlama testlerinin (Unit Tests vb.) bulunduğu klasördür.
  - **Tools/**: Geliştirme sürecini kolaylaştıracak dış (external) araçların / betiklerin barındırıldığı yerdir.
  - **UnityProject/Case-15/**: Projenin kalbi! Unity Editor tarafından yönetilen asıl proje yapısıdır.
    - `Case-15.sln` & `Assembly-CSharp.csproj`: IDE'ler (Ör: Visual Studio) için C# mimari çözüm ve proje dosyalarıdır.
    - `Library/`, `Logs/`, `Temp/`: Unity'nin kendi otomatik oluşturduğu ara (cache) önbellek dosyalarıdır. Manuel müdahale edilmez.
    - **Packages/**: Unity'nin dahil ettiği resmi eklenti ve paketlerin referans listesidir.
    - **ProjectSettings/**: TagManager, Physics, Input ayarları gibi oyun motorunun temel/global proje ayarlarını tutar.
    - **Assets/**: Bütün asıl oyun içerikleri, kodları, görselleri ve sahneleri burada konumlanır.
      - `Audio/`: Ses efekti ve müzik (BGM, SFX) dosyalarının tutulduğu klasördür.
      - `Materials/`: Sahnedeki nesnelere ait materyal ve renk/görünüm (Shader) dosyaları için kullanılır.
      - `Plugins/`: Projeye dışarıdan entegre edilmiş 3. parti kütüphaneler (Ör: WFC için dış modüller) burada saklanır.
      - `Prefabs/`: Önceden ayarlanmış obje (GameObject) şablonlarıdır. `Characters/`, `Props/`, `Scenes/` gibi alt klasörleri ve harita üretimi için kritik olan kaynakları (`Door.prefab`, `Glass.prefab`, `Ground.prefab`, `Wall.prefab`) içerir.
      - `Scenes/`: Unity oyun "bölüm" veya "arayüz" dosyalarıdır (`.unity` formatı). `Case.unity` (aktif oyun/test sahnesi) ve `SampleScene.unity` içerir.
      - `Sprites/`: 2D görseller ve Pixel Art çizimleri (Texture) için kullanılır. `16x16/` boyutlarında ve retro izometrik/top-down temalı zemin ve duvar çizimlerini barındırır.
      - `TextMesh Pro/`: Oyundaki gelişmiş arayüz yazıları (UI Text) için sistemin kendi dahil ettiği font/ayar klasörüdür.
      - `InputSystem_Actions.inputactions`: Unity'nin yeni Input System kontrol (Klavye, Gamepad) eşleme haritasıdır.
      - **Scripts/**: C# ile yazılmış olan, oyunun tüm davranışsal fonksiyonlarıdır.
        - `AI/`: Oyun içindeki düşman veya NPC (Bot) zekalarının davranış komut klasörüdür.
        - `Gameplay/`: Asıl oynanış ve etkileşim mekanikleridir. Oyun bilgisayarlarıyla iletişim kuran `ComputerInteract.cs`, oyuncuyu kontrol eden `PlayerTopDownMovement.cs` ve Tilemap üzerinde DFS mantığıyla seviye üreten alternatif `ProceduralRoomGenerator2D.cs` burada yer alır.
        - `UI/`: Kullanıcı arayüzleriyle (Ana menü, sağlık çubuğu vb.) ilgilenen kod kısımlarıdır.
        - `Utilities/`: Matematiksel işlemler veya merkezi fonksiyonsuz genel araç / yardımcı (helper) betikleridir.
        - `DeBroglieGenerator.cs`: Oyunun ana WFC (Kat Katlama) harita üretecidir. Hızlı erişim için Scripts ana dizininde durur.
        - `MapJSONExporter.cs`: Oluşturulan rastgele haritaları analiz için formata döken JSON çıktı betiğidir.
      - `tiles.xml`: Prosedürel harita planlamasında (DeBroglie algoritmasında) birbirine bağlanacak 2D blokların hangi kural ve kombinasyonlarla (Örn: Duvar kapıya bitişebilir) yan yana geleceğini anlatan kritik öneme sahip parametre dosyasıdır.
      - `test_config.json`: Çeşitli üretim değerlerini (Oda sayısı, duvar kalınlığı vb.) test ortamında IDE kullanmadan değiştirebilmek için hazırlanmış bir test konfigürasyonu dosyası.

## Özet
"Case-15" oyun içi prosedürel odalar ve alan kombinasyonlarını dinamik olarak DeBroglie kütüphanesini baz alarak hesaplayan ve oyuncuyu bu rastgele inşa edilmiş binaların içerisine yerleştiren; alt yapısında muhtemel bir çok oyunculu (multiplayer) yapının temelleri bulunan detaylı bir **2D Top-Down prosedürel zindan/merkez simülasyonu** taslağıdır.
