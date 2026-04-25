using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using DeBroglie;
using DeBroglie.Models;
using DeBroglie.Topo;
using System.Linq;
using Unity.Mathematics.Geometry;

#if UNITY_EDITOR
using UnityEditor;
#endif

// LLM'den gelecek talimat seti için seri ilan edilebilir sınıflar
[System.Serializable]
public class LLMLevelPlan
{
    public List<LLMRoomRequest> rooms = new List<LLMRoomRequest>();
}

[System.Serializable]
public class LLMRoomRequest
{
    public string id;           // Odanın adı (Mutfak, Koridor vb.)
    public int width;           // Odanın genişliği
    public int height;          // Odanın yüksekliği
    public string connectTo;    // Hangi odaya bağlanacağı
    public string direction;    // Bağlantı yönü: "Up", "Down", "Left", "Right"
    public int corridorLen;     // Bağlantı yolu (koridor) uzunluğu
}

[System.Serializable]
public enum PropPlacementType
{
    WallEdge,   // Duvar kenarına yaslanmalı (TV, Kitaplık, Mutfak Tezgahı)
    Center,     // Odanın ortalarında serbest (Sehpa, Masa)
    Corner      // Sadece köşelere (Saksı, Lambader)
}

[System.Serializable]
public class PropDefinition
{
    public string propName; // Prefab'ın adı (örn: "TV", "Sofa")
    public bool isDirectional;
    public PropPlacementType placementType;
    public bool faceInward; // İçeriye doğru mu bakmalı? (TV ve Koltuk için true)
}

[System.Serializable]
public class RoomPropPool
{
    public string roomID; // "Lobby", "Office" vb.
    public List<PropDefinition> propsToSpawn;
}


public class DeBroglieGenerator : MonoBehaviour
{
    public string tilesXmlPath = "Assets/tiles.xml";


    [Header("Prop Settings")]
    public List<RoomPropPool> roomPropPools;

    [Header("LLM Stability Settings")]
    // LLM'den gelen planı inspector üzerinden de görebilmen/test edebilmen için
    public LLMLevelPlan currentPlan;
    public bool useManualPlanForTest = true;

    [Header("Universal Structural Rules")]
    public int minTopWallThickness = 2;
    public int maxTopWallThickness = 4;

    [Header("Generative Settings")]
    public float tileSize = 1f;

    [Header("2D System Settings")]
    public bool is2D = true;
    public bool variantIsFlipX = false;

    private int retryCount = 0;
    private const int MAX_RETRIES = 10;

    // İç mantık için gerekli veri yapıları
    public class RoomData
    {
        public string roomID;
        public RectInt groundRect;
        public RectInt fullRect;
        public int topWallThickness;
        // LLM planında spesifik kapı koordinatları yoksa algoritma bunları hesaplayacak
        public List<Vector2Int> allowedExits = new List<Vector2Int>();
    }

    public class CorridorData
    {
        public Vector2Int start;
        public Vector2Int end;
        public bool isVerticalFirst;
    }

    // Yardımcı Metotlar
    private int SafeRandomRange(int a, int b)
    {
        return UnityEngine.Random.Range(Mathf.Min(a, b), Mathf.Max(a, b) + 1);
    }

    // Yönleri LLM stringinden Vector2Int'e çeviren yardımcı metot
    private Vector2Int GetDirectionVector(string dir)
    {
        switch (dir.ToLower())
        {
            case "up": return Vector2Int.up;
            case "down": return Vector2Int.down;
            case "left": return Vector2Int.left;
            case "right": return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }

    [ContextMenu("Generate Scene")]
    public void Generate()
    {
        Debug.Log("Starting DeBroglie Generation...");

        int topWallThickness = UnityEngine.Random.Range(minTopWallThickness, maxTopWallThickness + 1);

        char[,] map = null;
        int minX = 0, minY = 0;
        int width = 0, height = 0;
        List<RoomData> roomsList = new List<RoomData>();
        List<CorridorData> corridors = new List<CorridorData>();
        Dictionary<string, RoomData> roomLookup = new Dictionary<string, RoomData>();

        // 1. TEST VERİSİ (Plan boşsa doldurur, doluysa dokunmaz)
        if (useManualPlanForTest && (currentPlan == null || currentPlan.rooms.Count == 0))
        {
            currentPlan = new LLMLevelPlan();
            currentPlan.rooms.Add(new LLMRoomRequest { id = "Lobby", width = 10, height = 10, connectTo = "", direction = "" });
            currentPlan.rooms.Add(new LLMRoomRequest { id = "Office", width = 6, height = 6, connectTo = "Lobby", direction = "Right", corridorLen = 3 });
            currentPlan.rooms.Add(new LLMRoomRequest { id = "Storage", width = 5, height = 7, connectTo = "Lobby", direction = "Up", corridorLen = 2 });
        }

        // 2. ADIM: ODALARI YERLEŞTİRME VE KORİDOR HESAPLAMA
        foreach (var req in currentPlan.rooms)
        {
            RoomData newRoom = new RoomData { roomID = req.id };
            newRoom.topWallThickness = SafeRandomRange(minTopWallThickness, maxTopWallThickness);

            int nx = 50, ny = 50;

            if (!string.IsNullOrEmpty(req.connectTo) && roomLookup.ContainsKey(req.connectTo))
            {
                RoomData parent = roomLookup[req.connectTo];
                int xOffset = UnityEngine.Random.Range(0, Mathf.Max(1, parent.groundRect.width - req.width));
                int yOffset = UnityEngine.Random.Range(0, Mathf.Max(1, parent.groundRect.height - req.height));

                // Koridor Başlangıç ve Bitiş Noktaları (Dinamik Hizalama İçin)
                int startX = 0, startY = 0;
                int endX = 0, endY = 0;

                switch (req.direction.ToLower())
                {
                    case "up":
                        nx = parent.groundRect.xMin + xOffset;
                        ny = parent.fullRect.yMax + req.corridorLen + 1;
                        // Dikey Hizalama: X'i yeni odanın ortasına sabitle
                        startX = nx + (req.width / 2);
                        startY = parent.groundRect.yMax;
                        endX = startX;
                        endY = ny - 1;
                        break;

                    case "down":
                        nx = parent.groundRect.xMin + xOffset;
                        ny = parent.fullRect.yMin - req.corridorLen - req.height - newRoom.topWallThickness;
                        // Dikey Hizalama: X'i yeni odanın ortasına sabitle
                        startX = nx + (req.width / 2);
                        startY = parent.groundRect.yMin - 1;
                        endX = startX;
                        endY = ny + req.height + newRoom.topWallThickness;
                        break;

                    case "right":
                        nx = parent.fullRect.xMax + req.corridorLen + 1;
                        ny = parent.groundRect.yMin + yOffset;
                        // Yatay Hizalama: Y'yi yeni odanın ortasına sabitle
                        startX = parent.groundRect.xMax;
                        startY = ny + (req.height / 2);
                        endX = nx - 1;
                        endY = startY;
                        break;

                    case "left":
                        nx = parent.fullRect.xMin - req.corridorLen - req.width - 1;
                        ny = parent.groundRect.yMin + yOffset;
                        // Yatay Hizalama: Y'yi yeni odanın ortasına sabitle
                        startX = parent.groundRect.xMin - 1;
                        startY = ny + (req.height / 2);
                        endX = nx + req.width;
                        endY = startY;
                        break;
                }

                // Yeni Dinamik Koridor Verisi
                CorridorData c = new CorridorData();
                c.start = new Vector2Int(startX, startY);
                c.end = new Vector2Int(endX, endY);
                c.isVerticalFirst = (req.direction.ToLower() == "up" || req.direction.ToLower() == "down");
                corridors.Add(c);
            }

            newRoom.groundRect = new RectInt(nx, ny, req.width, req.height);
            newRoom.fullRect = new RectInt(nx - 1, ny - 1, req.width + 2, req.height + newRoom.topWallThickness + 1);

            roomsList.Add(newRoom);
            roomLookup[req.id] = newRoom;
        }

        // 2. ADIM'ın hemen sonuna ekle:
        int actualMinX = int.MaxValue, actualMaxX = int.MinValue;
        int actualMinY = int.MaxValue, actualMaxY = int.MinValue;

        foreach (var r in roomsList)
        {
            actualMinX = Mathf.Min(actualMinX, r.fullRect.xMin);
            actualMaxX = Mathf.Max(actualMaxX, r.fullRect.xMax);
            actualMinY = Mathf.Min(actualMinY, r.fullRect.yMin);
            actualMaxY = Mathf.Max(actualMaxY, r.fullRect.yMax);
        }

        // Global değişkenlere ata (kenarlarda 2 birim boşluk bırakmak WFC için iyidir)
        minX = actualMinX - 2;
        minY = actualMinY - 2;
        width = (actualMaxX - actualMinX) + 5;
        height = (actualMaxY - actualMinY) + 5;

        // Haritayı initialize et (Bunu yapmazsan null hatası alırsın)
        map = new char[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = ' ';

        // Harita boyutlarını hesapladıktan sonra:

        foreach (var r in roomsList)
        {
            // Zeminleri çiz
            for (int x = r.groundRect.xMin; x < r.groundRect.xMax; x++)
                for (int y = r.groundRect.yMin; y < r.groundRect.yMax; y++)
                    map[x - minX, y - minY] = 'G';

            // Duvarları çiz
            for (int x = r.fullRect.xMin; x < r.fullRect.xMax; x++)
            {
                for (int y = r.fullRect.yMin; y < r.fullRect.yMax; y++)
                {
                    if (map[x - minX, y - minY] != 'G')
                        map[x - minX, y - minY] = 'W';
                }
            }
        }

        // 5. ADIM: KORİDORLARI ÇİZ (Artık düz çizecek)
        foreach (var c in corridors)
        {
            Vector2Int cur = c.start;
            int stepX = (c.end.x > c.start.x) ? 1 : (c.end.x < c.start.x ? -1 : 0);
            int stepY = (c.end.y > c.start.y) ? 1 : (c.end.y < c.start.y ? -1 : 0);
            bool doorPlaced = false;

            void DrawCell(int x, int y)
            {
                int mx = x - minX; int my = y - minY;
                if (mx < 0 || mx >= width || my < 0 || my >= height) return;

                if (map[mx, my] == 'W' && !doorPlaced) { map[mx, my] = 'D'; doorPlaced = true; }
                else if (map[mx, my] != 'G' && map[mx, my] != 'D') { map[mx, my] = 'G'; }

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int wx = mx + dx; int wy = my + dy;
                        if (wx >= 0 && wx < width && wy >= 0 && wy < height && map[wx, wy] == ' ') map[wx, wy] = 'W';
                    }
            }

            // Çizim döngüsü (startX/Y artık endX/Y ile aynı eksende olduğu için düz gider)
            while (cur != c.end)
            {
                DrawCell(cur.x, cur.y);
                if (cur.x != c.end.x) cur.x += stepX;
                else if (cur.y != c.end.y) cur.y += stepY;
            }
            DrawCell(c.end.x, c.end.y);
        }

        // 6. ADIM: CAMLARI YERLEŞTİR (Sadece duvar olan yerlere)
        for (int i = 1; i < roomsList.Count; i++)
        {
            var r = roomsList[i];
            int glassY = r.groundRect.yMax + r.topWallThickness / 2;
            for (int x = r.groundRect.xMin + 1; x < r.groundRect.xMax - 1; x++)
            {
                int mx = x - minX; int my = glassY - minY;
                if (map[mx, my] == 'W' && map[mx - 1, my] == 'W' && map[mx + 1, my] == 'W')
                {
                    if (UnityEngine.Random.value > 0.5f) map[mx, my] = 'O';
                }
            }
        }

        // --- ANA GİRİŞ KAPISI ALGORİTMASI ---
        List<System.Tuple<Vector2Int, Vector2Int>> entranceCandidates = new List<System.Tuple<Vector2Int, Vector2Int>>();
        RoomData mRoom = roomsList[0];

        int minOccupiedX = int.MaxValue, maxOccupiedX = int.MinValue;
        int minOccupiedY = int.MaxValue, maxOccupiedY = int.MinValue;
        foreach (var r in roomsList)
        {
            minOccupiedX = Mathf.Min(minOccupiedX, r.fullRect.xMin);
            maxOccupiedX = Mathf.Max(maxOccupiedX, r.fullRect.xMax);
            minOccupiedY = Mathf.Min(minOccupiedY, r.fullRect.yMin);
            maxOccupiedY = Mathf.Max(maxOccupiedY, r.fullRect.yMax);
        }

        bool canGoUp = mRoom.fullRect.yMax >= maxOccupiedY;
        bool canGoDown = mRoom.fullRect.yMin <= minOccupiedY;
        bool canGoLeft = mRoom.fullRect.xMin <= minOccupiedX;
        bool canGoRight = mRoom.fullRect.xMax >= maxOccupiedX;

        if (canGoLeft)
        {
            for (int y = mRoom.groundRect.yMin + 1; y < mRoom.groundRect.yMax - 1; y++)
                entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(mRoom.groundRect.xMin - 1, y), Vector2Int.left));
        }
        if (canGoRight)
        {
            for (int y = mRoom.groundRect.yMin + 1; y < mRoom.groundRect.yMax - 1; y++)
                entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(mRoom.groundRect.xMax, y), Vector2Int.right));
        }
        if (canGoDown)
        {
            for (int x = mRoom.groundRect.xMin + 1; x < mRoom.groundRect.xMax - 1; x++)
                entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(x, mRoom.groundRect.yMin - 1), Vector2Int.down));
        }
        if (canGoUp)
        {
            for (int x = mRoom.groundRect.xMin + 1; x < mRoom.groundRect.xMax - 1; x++)
                entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(x, mRoom.groundRect.yMax), Vector2Int.up));
        }

        if (entranceCandidates.Count > 0)
        {
            var choice = entranceCandidates[UnityEngine.Random.Range(0, entranceCandidates.Count)];
            int ex = choice.Item1.x - minX;
            int ey = choice.Item1.y - minY;
            map[ex, ey] = 'D';

            int outX = ex + choice.Item2.x;
            int outY = ey + choice.Item2.y;
            if (outX >= 0 && outX < width && outY >= 0 && outY < height) map[outX, outY] = 'G';
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(tilesXmlPath);

        Dictionary<string, double> tileWeights = new Dictionary<string, double>();
        foreach (XmlNode node in doc.SelectNodes("set/tiles/tile"))
        {
            string name = node.Attributes["name"].Value;
            double weight = node.Attributes["weight"] != null ? double.Parse(node.Attributes["weight"].Value, System.Globalization.CultureInfo.InvariantCulture) : 1.0;
            tileWeights[name] = weight;
        }

        List<System.Tuple<string, string>> leftRightNeighbors = new List<System.Tuple<string, string>>();
        List<System.Tuple<string, string>> topBottomNeighbors = new List<System.Tuple<string, string>>();
        HashSet<string> allTileVariantsSet = new HashSet<string>();

        foreach (XmlNode node in doc.SelectNodes("set/neighbors/neighbor"))
        {
            if (node.Attributes["left"] != null && node.Attributes["right"] != null)
            {
                string left = node.Attributes["left"].Value;
                string right = node.Attributes["right"].Value;
                leftRightNeighbors.Add(new System.Tuple<string, string>(left, right));
                allTileVariantsSet.Add(left);
                allTileVariantsSet.Add(right);
            }
            if (node.Attributes["top"] != null && node.Attributes["bottom"] != null)
            {
                string top = node.Attributes["top"].Value;
                string bottom = node.Attributes["bottom"].Value;
                topBottomNeighbors.Add(new System.Tuple<string, string>(top, bottom));
                allTileVariantsSet.Add(top);
                allTileVariantsSet.Add(bottom);
            }
        }

        List<string> allTileVariants = new List<string>(allTileVariantsSet);

        // DeBroglie Initialization
        // We use 2D topology
        GridTopology topology = new GridTopology(width, height, false);
        AdjacentModel model = new AdjacentModel(topology.Directions);

        foreach (string variant in allTileVariants)
        {
            Tile tile = new Tile(variant);
            string baseName = GetBaseName(variant);
            double w = tileWeights.ContainsKey(baseName) ? tileWeights[baseName] : 1.0;
            model.SetFrequency(tile, w);
        }

        // Add adjacencies
        // In GridTopology, XRight is direction 0, XLeft is 1, YUp is 2, YDown is 3 (depending on Cartesian2d vs custom)
        // Let's assume standard GridTopology:
        // direction 0 = (1, 0)
        // direction 1 = (-1, 0)
        // direction 2 = (0, 1)
        // direction 3 = (0, -1)
        foreach (var pair in leftRightNeighbors)
        {
            Tile leftTile = new Tile(pair.Item1);
            Tile rightTile = new Tile(pair.Item2);
            // leftTile is to the left of rightTile. Moving right from leftTile yields rightTile.
            model.AddAdjacency(leftTile, rightTile, 1, 0, 0); // 1,0,0 corresponds to +X
        }
        foreach (var pair in topBottomNeighbors)
        {
            Tile topTile = new Tile(pair.Item1);
            Tile bottomTile = new Tile(pair.Item2);
            model.AddAdjacency(topTile, bottomTile, 0, -1, 0);
            model.AddAdjacency(bottomTile, topTile, 0, 1, 0);// using x, y, z overload if possible, else we use 2/3
            // Actually, best to use assumed direction 2 and 3
        }
        
        // If there are no vertical rules, allow all vertical placements
        if (topBottomNeighbors.Count == 0 && height > 1)
        {
            Debug.LogWarning("No top/bottom neighbors defined. Allowing all vertical adjacencies.");
            foreach (var t1 in allTileVariants)
            {
                foreach (var t2 in allTileVariants)
                {
                    // Direction 2 is YPlus
                    model.AddAdjacency(new Tile(t1), new Tile(t2), 0, 1, 0);
                }
            }
        }

        TilePropagator propagator = new TilePropagator(model, topology);




        if (map != null)
        {
            string groundVariant = allTileVariants.FirstOrDefault(v => v.Contains("Ground"));
            string wallVariant = allTileVariants.FirstOrDefault(v => v.Contains("Wall"));
            string glassVariant = allTileVariants.FirstOrDefault(v => v.Contains("Glass"));
            string doorVariant = allTileVariants.FirstOrDefault(v => v.Contains("Door"));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = map[x, y];
                        
                        if (c == ' ') { 
                            if (groundVariant != null) propagator.Ban(x, y, 0, new Tile(groundVariant));
                            if (doorVariant != null) propagator.Ban(x, y, 0, new Tile(doorVariant));
                            if (glassVariant != null) propagator.Ban(x, y, 0, new Tile(glassVariant));
                            //if (wallVariant != null) propagator.Ban(x, y, 0, new Tile(wallVariant));
                        }
                        else if (c == 'W') {
                            if (groundVariant != null) propagator.Ban(x, y, 0, new Tile(groundVariant));
                            if (doorVariant != null) propagator.Ban(x, y, 0, new Tile(doorVariant));
                            if (glassVariant != null) propagator.Ban(x, y, 0, new Tile(glassVariant));
                        }
                        else if (c == 'G') {
                            if (wallVariant != null) propagator.Ban(x, y, 0, new Tile(wallVariant));
                            if (doorVariant != null) propagator.Ban(x, y, 0, new Tile(doorVariant));
                            if (glassVariant != null) propagator.Ban(x, y, 0, new Tile(glassVariant));
                        }
                        else if (c == 'D') {
                            if (doorVariant != null) propagator.Select(x, y, 0, new Tile(doorVariant));
                        }
                        else if (c == 'O') {
                            if (groundVariant != null) propagator.Ban(x, y, 0, new Tile(groundVariant));
                            if (doorVariant != null) propagator.Ban(x, y, 0, new Tile(doorVariant));
                        // Allowing Wall or Glass prevents WFC contradictions if Glass is illegal
                            if (glassVariant != null) propagator.Select(x, y, 0, new Tile(glassVariant));
                    }
                }
            }
        }

        DeBroglie.Resolution status = propagator.Run();

        Debug.Log("DeBroglie Run Status: " + status.ToString());



        if (status == DeBroglie.Resolution.Contradiction)
        {
            if (retryCount < MAX_RETRIES)
            {
                retryCount++;
                Debug.LogWarning($"Çelişki oluştu. Deneme {retryCount}/{MAX_RETRIES}...");

                // Sahneyi temizlediğinden emin ol (Eğer önceden bir şeyler spawn ediyorsan)
                // ClearLevel(); 

                Generate(); // Tekrar dene
                return;
            }
            else
            {
                Debug.LogError("Maksimum deneme sayısına ulaşıldı! XML kurallarını veya harita taslağını kontrol et.");
                retryCount = 0; // Bir sonraki manuel basış için sıfırla
                return;
            }
        }

        Debug.Log("Generation successful, instantiating prefabs...");

        // Clean up children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Instantiate
        ITopoArray<string> result = propagator.ToValueArray<string>();
        int instantCount = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                string variant = result.Get(x, y);
                if (variant != null)
                {
                    if (map != null && map[x, y] == ' ') 
                        continue; // Skip rendering the Void!

                    InstantiateTile(variant, x, y);
                    instantCount++;
                }
                else
                {
                    Debug.LogWarning("Result variant is null at " + x + ", " + y);
                }
            }
        }
        Debug.Log("Total prefabs instantiated: " + instantCount);

        PlaceProps(map, minX, minY, roomsList);
    }

    private string GetBaseName(string variant)
    {
        // 1. ADIM: Void kontrolü (XML'de Assets/Prefabs/ kullanmadığın için)
        if (variant == "Void") return "Void";

        // 2. ADIM: Mevcut variant temizleme mantığın
        // Variant is like "Assets/Prefabs/Wall 0" or "Assets/Prefabs/Wall"
        int lastSpace = variant.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            string possibleNumber = variant.Substring(lastSpace + 1);
            if (int.TryParse(possibleNumber, out _))
            {
                return variant.Substring(0, lastSpace);
            }
        }
        return variant;
    }

    private void InstantiateTile(string variant, int x, int y)
    {
        string baseName = GetBaseName(variant);

#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseName + ".prefab");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);

            if (is2D)
            {
                // tileSize ile çarparak her zaman tile'ın tam ortasına denk gelmesini sağlıyoruz
                float xPos = (x * tileSize) + (tileSize * 0.5f);
                float yPos = (y * tileSize) + (tileSize * 0.5f);

                instance.transform.localPosition = new Vector3(xPos, yPos, 0);
            }
            else
            {
                // 3D için de aynı mantık (X ve Z düzleminde)
                float xPos = (x * tileSize) + (tileSize * 0.5f);
                float zPos = (y * tileSize) + (tileSize * 0.5f);

                instance.transform.localPosition = new Vector3(xPos, 0, zPos);
            }

            // Handle Rotations based on variant number
            int lastSpace = variant.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                string possibleNumber = variant.Substring(lastSpace + 1);
                if (int.TryParse(possibleNumber, out int num))
                {
                    if (is2D)
                    {
                        if (variantIsFlipX)
                        {
                            instance.transform.localScale = new Vector3(num % 2 != 0 ? -1 : 1, 1, 1);
                        }
                        else
                        {
                            instance.transform.localRotation = Quaternion.Euler(0, 0, num * -90f);
                        }
                    }
                    else
                    {
                        instance.transform.localRotation = Quaternion.Euler(0, num * 90f, 0);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Prefab not found at: " + baseName + ".prefab");
        }
#endif
    }

    private void InstantiateProp(string propName, int x, int y, float rotationAngle)
    {
#if UNITY_EDITOR
        // Proplarını Prefabs/Props gibi bir klasörde tuttuğunu varsayıyorum, yolunu kendine göre ayarla.
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + propName + ".prefab");
        if (prefab != null)
        {

            Debug.LogWarning("AAAAAAAAAAAAAAAAAAAA " + propName);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);

            float xPos = (x * tileSize) + (tileSize * 0.5f);

            if (is2D)
            {
                float yPos = (y * tileSize) + (tileSize * 0.5f);
                instance.transform.localPosition = new Vector3(xPos, yPos, -0.1f);
                instance.transform.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            }
            else
            {
                float zPos = (y * tileSize) + (tileSize * 0.5f);
                instance.transform.localPosition = new Vector3(xPos, 0.05f, zPos);
                instance.transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
            }
        }
        else
        {
            Debug.LogWarning("Prop Prefab bulunamadı: " + propName);
        }
#endif
    }
    private void PlaceProps(char[,] map, int minX, int minY, List<RoomData> roomsList)
    {
        // Hangi gridlerin prop ile dolu olduğunu takip edelim ki üst üste binmesinler
        bool[,] isOccupied = new bool[map.GetLength(0), map.GetLength(1)];

        foreach (var room in roomsList)
        {
            // Bu odaya ait prop listesini bul
            RoomPropPool pool = roomPropPools.FirstOrDefault(p => p.roomID == room.roomID);
            if (pool == null) continue;

            foreach (var prop in pool.propsToSpawn)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 50) // Sonsuz döngüyü önlemek için limit
                {
                    attempts++;

                    // Odanın zemin sınırları içinde rastgele bir nokta seç
                    int rx = UnityEngine.Random.Range(room.groundRect.xMin, room.groundRect.xMax);
                    int ry = UnityEngine.Random.Range(room.groundRect.yMin, room.groundRect.yMax);

                    int mx = rx - minX;
                    int my = ry - minY;

                    // Eğer burası zemin değilse, kapıysa veya doluysa atla
                    if (map[mx, my] != 'G' || isOccupied[mx, my]) continue;

                    if (prop.placementType == PropPlacementType.WallEdge)
                    {
                        string suffix = "";
                        bool isValidWallSpot = false;

                        // Duvar yönlerini kontrol et
                        if (map[mx, my + 1] == 'W' && map[mx, my - 1] != 'D')
                        {
                            if (prop.isDirectional) suffix = "_Back";
                            isValidWallSpot = true;
                        }
                        else if (map[mx, my - 1] == 'W' && map[mx, my + 1] != 'D')
                        {
                            if (prop.isDirectional) suffix = "_Front";
                            isValidWallSpot = true;
                        }
                        else if (map[mx - 1, my] == 'W' && map[mx + 1, my] != 'D')
                        {
                            if (prop.isDirectional) suffix = "_Side_R";
                            isValidWallSpot = true;
                        }
                        else if (map[mx + 1, my] == 'W' && map[mx - 1, my] != 'D')
                        {
                            if (prop.isDirectional) suffix = "_Side_L";
                            isValidWallSpot = true;
                        }

                        if (isValidWallSpot)
                        {
                            // Eğer isDirectional false ise suffix zaten boş ("") kalacak
                            string finalPropName = prop.propName + suffix;

                            // faceInward logic'ini yönlü olmayan basit objeler için hala kullanabilirsin
                            float rotation = (prop.isDirectional) ? 0f : (prop.faceInward ? GetSimpleRotation(mx, my, map) : 0f);

                            InstantiateProp(finalPropName, mx, my, rotation);

                            isOccupied[mx, my] = true;
                            placed = true;
                        }
                    }
                    else if (prop.placementType == PropPlacementType.Center)
                    {
                        // Merkeze konacaksa duvarlara değmediğinden emin olalım (1 birim içeride olsun)
                        if (map[mx + 1, my] == 'G' && map[mx - 1, my] == 'G' &&
                            map[mx, my + 1] == 'G' && map[mx, my - 1] == 'G')
                        {
                            // Rastgele 90 derecelik bir açı ver
                            float randomRot = UnityEngine.Random.Range(0, 4) * 90f;
                            InstantiateProp(prop.propName, mx, my, randomRot);
                            isOccupied[mx, my] = true;
                            placed = true;
                        }
                    }
                }
            }
        }
    }
    private float GetSimpleRotation(int mx, int my, char[,] map)
    {
        // Üst duvar kontrolü (Prop aşağı bakmalı)
        if (mx >= 0 && mx < map.GetLength(0) && my + 1 < map.GetLength(1))
        {
            if (map[mx, my + 1] == 'W') return 180f;
        }

        // Alt duvar kontrolü (Prop yukarı bakmalı)
        if (mx >= 0 && mx < map.GetLength(0) && my - 1 >= 0)
        {
            if (map[mx, my - 1] == 'W') return 0f;
        }

        // Sol duvar kontrolü (Prop sağa bakmalı)
        if (mx - 1 >= 0 && mx < map.GetLength(0) && my >= 0 && my < map.GetLength(1))
        {
            if (map[mx - 1, my] == 'W') return 90f;
        }

        // Sağ duvar kontrolü (Prop sola bakmalı)
        if (mx + 1 < map.GetLength(0) && my >= 0 && my < map.GetLength(1))
        {
            if (map[mx + 1, my] == 'W') return -90f;
        }

        return 0f; // Hiçbir duvar bulunamazsa varsayılan rotasyon
    }
}
