using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using DeBroglie;
using DeBroglie.Models;
using DeBroglie.Topo;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeBroglieGenerator : MonoBehaviour
{
    public string tilesXmlPath = "Assets/tiles.xml";

    [Header("DFS Mode Settings")]
    public int dfsMinGroundWidth = 10;
    public int dfsMaxGroundWidth = 15;
    public int dfsMinGroundHeight = 5;
    public int dfsMaxGroundHeight = 10;
    public int dfsMaxRooms = 10;
    public int dfsMinCorridor = 2;
    public int dfsMaxCorridor = 5;

    [Header("Hub Mode Settings")]
    public int hubMinMainWidth = 8;
    public int hubMaxMainWidth = 14;
    public int hubMinMainHeight = 8;
    public int hubMaxMainHeight = 14;
    public int hubMaxRooms = 10;
    public int hubMinWidth = 4;
    public int hubMaxWidth = 8;
    public int hubMinHeight = 4;
    public int hubMaxHeight = 8;
    public int hubMinCorridor = 2;
    public int hubMaxCorridor = 5;

    [Header("Universal Structural Rules")]
    public int minTopWallThickness = 2;
    public int maxTopWallThickness = 4;

    [Header("Generative Settings")]
    public float tileSize = 1f;

    [Header("2D System Settings")]
    public bool is2D = true;
    public bool variantIsFlipX = false;

    public enum RoomStyle { DFSMultiRoom, HubMultiRoom }

    public class RoomData {
        public RectInt groundRect;
        public RectInt fullRect;
        public int topWallThickness;
        public List<Vector2Int> allowedExits = new List<Vector2Int>();
    }
    public class CorridorData {
        public Vector2Int start;
        public Vector2Int end;
        public bool isVerticalFirst;
    }

    private int SafeRandomRange(int a, int b) {
        return UnityEngine.Random.Range(Mathf.Min(a, b), Mathf.Max(a, b) + 1);
    }

    private List<Vector2Int> GetRandomDirections(int minDirs, int maxDirs) {
        List<Vector2Int> allDirs = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        List<Vector2Int> result = new List<Vector2Int>();
        int count = UnityEngine.Random.Range(minDirs, maxDirs + 1);
        int maxAllowed = Mathf.Min(count, allDirs.Count);
        for (int i = 0; i < maxAllowed; i++) {
            int idx = UnityEngine.Random.Range(0, allDirs.Count);
            result.Add(allDirs[idx]);
            allDirs.RemoveAt(idx);
        }
        return result;
    }
    [Header("Room Setup")]
    public RoomStyle roomStyle = RoomStyle.DFSMultiRoom;

    [ContextMenu("Generate Scene")]
    public void Generate()
    {
        Debug.Log("Starting DeBroglie Generation...");

        int topWallThickness = UnityEngine.Random.Range(minTopWallThickness, maxTopWallThickness + 1);

        char[,] map = null;
        List<RoomData> roomsList = null;
        int minX = 0, minY = 0;
        int width = 0, height = 0;

        if (roomStyle == RoomStyle.DFSMultiRoom)
        {
             int groundWidth = UnityEngine.Random.Range(dfsMinGroundWidth, dfsMaxGroundWidth + 1);
             int groundHeight = UnityEngine.Random.Range(dfsMinGroundHeight, dfsMaxGroundHeight + 1);

             roomsList = new List<RoomData>();
             List<CorridorData> corridors = new List<CorridorData>();
             
             RoomData mainRoom = new RoomData();
             mainRoom.topWallThickness = topWallThickness;
             mainRoom.groundRect = new RectInt(50, 50, groundWidth, groundHeight);
             mainRoom.fullRect = new RectInt(49, 49, groundWidth + 2, groundHeight + topWallThickness + 1);
             mainRoom.allowedExits = GetRandomDirections(2, 4);
             roomsList.Add(mainRoom);

             int attempts = 0; int currentRooms = 0;

             while (currentRooms < dfsMaxRooms && attempts < 2000)
             {
                 attempts++;
                 RoomData parent = roomsList[UnityEngine.Random.Range(0, roomsList.Count)];
                 if (parent.allowedExits.Count == 0) continue;
                 
                 List<Vector2Int> validDirs = parent.allowedExits;
                 Vector2Int dir = validDirs[UnityEngine.Random.Range(0, validDirs.Count)];

                 int nw = UnityEngine.Random.Range(dfsMinGroundWidth, dfsMaxGroundWidth + 1);
                 int nh = UnityEngine.Random.Range(dfsMinGroundHeight, dfsMaxGroundHeight + 1);
                 int nTop = UnityEngine.Random.Range(minTopWallThickness, maxTopWallThickness + 1);
                 int corridorLen = UnityEngine.Random.Range(dfsMinCorridor, dfsMaxCorridor + 1);
                 int nx = 0, ny = 0;

                 if (dir == Vector2Int.up)
                 {
                     nx = SafeRandomRange(parent.groundRect.xMin - nw + 2, parent.groundRect.xMax - 2);
                     ny = parent.fullRect.yMax + corridorLen + 1;
                 }
                 else if (dir == Vector2Int.down)
                 {
                     nx = SafeRandomRange(parent.groundRect.xMin - nw + 2, parent.groundRect.xMax - 2);
                     ny = parent.fullRect.yMin - corridorLen - nh - nTop;
                 }
                 else if (dir == Vector2Int.right)
                 {
                     nx = parent.fullRect.xMax + corridorLen + 1;
                     ny = SafeRandomRange(parent.groundRect.yMin - nh + 2, parent.groundRect.yMax - 2);
                 }
                 else if (dir == Vector2Int.left)
                 {
                     nx = parent.fullRect.xMin - corridorLen - nw - 1;
                     ny = SafeRandomRange(parent.groundRect.yMin - nh + 2, parent.groundRect.yMax - 2);
                 }

                 RectInt newGround = new RectInt(nx, ny, nw, nh);
                 RectInt newFull = new RectInt(nx - 1, ny - 1, nw + 2, nh + nTop + 1);

                 CorridorData c = new CorridorData();
                 int overlapXMin = Mathf.Max(parent.groundRect.xMin, newGround.xMin);
                 int overlapXMax = Mathf.Min(parent.groundRect.xMax - 1, newGround.xMax - 1);
                 int overlapYMin = Mathf.Max(parent.groundRect.yMin, newGround.yMin);
                 int overlapYMax = Mathf.Min(parent.groundRect.yMax - 1, newGround.yMax - 1);
                 
                 if (dir == Vector2Int.up || dir == Vector2Int.down) {
                     int sharedX = overlapXMin <= overlapXMax ? UnityEngine.Random.Range(overlapXMin, overlapXMax + 1) : Mathf.RoundToInt(newGround.center.x);
                     c.start = new Vector2Int(sharedX, Mathf.RoundToInt(newGround.center.y));
                     c.end = new Vector2Int(sharedX, Mathf.RoundToInt(parent.groundRect.center.y));
                 } else {
                     int sharedY = overlapYMin <= overlapYMax ? UnityEngine.Random.Range(overlapYMin, overlapYMax + 1) : Mathf.RoundToInt(newGround.center.y);
                     c.start = new Vector2Int(Mathf.RoundToInt(newGround.center.x), sharedY);
                     c.end = new Vector2Int(Mathf.RoundToInt(parent.groundRect.center.x), sharedY);
                 }
                 c.isVerticalFirst = (dir == Vector2Int.up || dir == Vector2Int.down);

                 RectInt cRect = new RectInt(
                     Mathf.Min(c.start.x, c.end.x),
                     Mathf.Min(c.start.y, c.end.y),
                     Mathf.Abs(c.start.x - c.end.x) + 1,
                     Mathf.Abs(c.start.y - c.end.y) + 1
                 );

                 bool overlap = false;
                 foreach (var r in roomsList)
                 {
                     RectInt rExp = new RectInt(r.fullRect.x - 1, r.fullRect.y - 1, r.fullRect.width + 2, r.fullRect.height + 2);
                     if (rExp.Overlaps(newFull)) { overlap = true; break; }
                     if (r != parent && rExp.Overlaps(cRect)) { overlap = true; break; }
                 }

                 if (!overlap)
                 {
                     RoomData newRoom = new RoomData { groundRect = newGround, fullRect = newFull, topWallThickness = nTop, allowedExits = GetRandomDirections(1, 3) };
                     roomsList.Add(newRoom);
                     corridors.Add(c);
                     currentRooms++;
                 }
             }

             minX = 9999; minY = 9999; int maxX = -9999, maxY = -9999;
             foreach(var r in roomsList) {
                 if (r.fullRect.xMin < minX) minX = r.fullRect.xMin;
                 if (r.fullRect.yMin < minY) minY = r.fullRect.yMin;
                 if (r.fullRect.xMax > maxX) maxX = r.fullRect.xMax;
                 if (r.fullRect.yMax > maxY) maxY = r.fullRect.yMax;
             }
             minX -= 2; minY -= 2; maxX += 2; maxY += 2;
             width = maxX - minX;
             height = maxY - minY;

             map = new char[width, height];
             for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) map[x,y] = ' ';

             foreach(var r in roomsList) {
                 for (int x = r.fullRect.xMin; x < r.fullRect.xMax; x++) {
                     for (int y = r.fullRect.yMin; y < r.fullRect.yMax; y++) {
                         map[x - minX, y - minY] = 'W';
                     }
                 }
             }
             foreach(var r in roomsList) {
                 for (int x = r.groundRect.xMin; x < r.groundRect.xMax; x++) {
                     for (int y = r.groundRect.yMin; y < r.groundRect.yMax; y++) {
                         map[x - minX, y - minY] = 'G';
                     }
                 }
             }
             foreach(var c in corridors) {
                 int sx = c.start.x - minX, sy = c.start.y - minY;
                 int ex = c.end.x - minX, ey = c.end.y - minY;
                 
                 if (c.isVerticalFirst) {
                     int stepY = sy < ey ? 1 : -1;
                     for (int y = sy; y != ey; y += stepY) map[sx, y] = 'C';
                     map[sx, ey] = 'C';
                     int stepX = sx < ex ? 1 : -1;
                     for (int x = sx; x != ex; x += stepX) map[x, ey] = 'C';
                 } else {
                     int stepX = sx < ex ? 1 : -1;
                     for (int x = sx; x != ex; x += stepX) map[x, sy] = 'C';
                     map[ex, sy] = 'C';
                     int stepY = sy < ey ? 1 : -1;
                     for (int y = sy; y != ey; y += stepY) map[ex, y] = 'C';
                 }
             }
        }
        else if (roomStyle == RoomStyle.HubMultiRoom)
        {
             int mainW = UnityEngine.Random.Range(hubMinMainWidth, hubMaxMainWidth + 1);
             int mainH = UnityEngine.Random.Range(hubMinMainHeight, hubMaxMainHeight + 1);

             roomsList = new List<RoomData>();
             List<CorridorData> corridors = new List<CorridorData>();
             
             RoomData mainRoom = new RoomData();
             mainRoom.topWallThickness = topWallThickness;
             mainRoom.groundRect = new RectInt(50, 50, mainW, mainH);
             mainRoom.fullRect = new RectInt(49, 49, mainW + 2, mainH + topWallThickness + 1);
             mainRoom.allowedExits = GetRandomDirections(2, 4); // Hub dynamically restricts its exits for organic shapes
             roomsList.Add(mainRoom);

             int attempts = 0; int currentRooms = 0;

             while (currentRooms < hubMaxRooms && attempts < 2000)
             {
                 attempts++;
                 if (mainRoom.allowedExits.Count == 0) break; // Should never happen but safe

                 List<Vector2Int> validDirs = mainRoom.allowedExits;
                 Vector2Int dir = validDirs[UnityEngine.Random.Range(0, validDirs.Count)];

                 int nw = UnityEngine.Random.Range(hubMinWidth, hubMaxWidth + 1);
                 int nh = UnityEngine.Random.Range(hubMinHeight, hubMaxHeight + 1);
                 int nTop = UnityEngine.Random.Range(minTopWallThickness, maxTopWallThickness + 1);
                 int corridorLen = UnityEngine.Random.Range(hubMinCorridor, hubMaxCorridor + 1);

                 int nx = 0, ny = 0;

                 if (dir == Vector2Int.up)
                 {
                     nx = SafeRandomRange(mainRoom.groundRect.xMin - nw + 2, mainRoom.groundRect.xMax - 2);
                     ny = mainRoom.fullRect.yMax + corridorLen + 1;
                 }
                 else if (dir == Vector2Int.down)
                 {
                     nx = SafeRandomRange(mainRoom.groundRect.xMin - nw + 2, mainRoom.groundRect.xMax - 2);
                     ny = mainRoom.fullRect.yMin - corridorLen - nh - nTop;
                 }
                 else if (dir == Vector2Int.right)
                 {
                     nx = mainRoom.fullRect.xMax + corridorLen + 1;
                     ny = SafeRandomRange(mainRoom.groundRect.yMin - nh + 2, mainRoom.groundRect.yMax - 2);
                 }
                 else if (dir == Vector2Int.left)
                 {
                     nx = mainRoom.fullRect.xMin - corridorLen - nw - 1;
                     ny = SafeRandomRange(mainRoom.groundRect.yMin - nh + 2, mainRoom.groundRect.yMax - 2);
                 }

                 RectInt newGround = new RectInt(nx, ny, nw, nh);
                 RectInt newFull = new RectInt(nx - 1, ny - 1, nw + 2, nh + nTop + 1);

                 CorridorData c = new CorridorData();
                 int overlapXMin = Mathf.Max(mainRoom.groundRect.xMin, newGround.xMin);
                 int overlapXMax = Mathf.Min(mainRoom.groundRect.xMax - 1, newGround.xMax - 1);
                 int overlapYMin = Mathf.Max(mainRoom.groundRect.yMin, newGround.yMin);
                 int overlapYMax = Mathf.Min(mainRoom.groundRect.yMax - 1, newGround.yMax - 1);
                 
                 if (dir == Vector2Int.up || dir == Vector2Int.down) {
                     int sharedX = overlapXMin <= overlapXMax ? UnityEngine.Random.Range(overlapXMin, overlapXMax + 1) : Mathf.RoundToInt(newGround.center.x);
                     c.start = new Vector2Int(sharedX, Mathf.RoundToInt(newGround.center.y));
                     c.end = new Vector2Int(sharedX, Mathf.RoundToInt(mainRoom.groundRect.center.y));
                 } else {
                     int sharedY = overlapYMin <= overlapYMax ? UnityEngine.Random.Range(overlapYMin, overlapYMax + 1) : Mathf.RoundToInt(newGround.center.y);
                     c.start = new Vector2Int(Mathf.RoundToInt(newGround.center.x), sharedY);
                     c.end = new Vector2Int(Mathf.RoundToInt(mainRoom.groundRect.center.x), sharedY);
                 }
                 c.isVerticalFirst = (dir == Vector2Int.up || dir == Vector2Int.down);

                 RectInt cRect = new RectInt(
                     Mathf.Min(c.start.x, c.end.x),
                     Mathf.Min(c.start.y, c.end.y),
                     Mathf.Abs(c.start.x - c.end.x) + 1,
                     Mathf.Abs(c.start.y - c.end.y) + 1
                 );

                 bool overlap = false;
                 foreach (var r in roomsList)
                 {
                     RectInt rExp = new RectInt(r.fullRect.x - 1, r.fullRect.y - 1, r.fullRect.width + 2, r.fullRect.height + 2);
                     if (rExp.Overlaps(newFull)) { overlap = true; break; }
                     if (r != mainRoom && rExp.Overlaps(cRect)) { overlap = true; break; }
                 }

                 if (!overlap)
                 {
                     RoomData newRoom = new RoomData { groundRect = newGround, fullRect = newFull, topWallThickness = nTop };
                     roomsList.Add(newRoom);
                     corridors.Add(c);
                     currentRooms++;
                 }
             }

             minX = 9999; minY = 9999; int maxX = -9999, maxY = -9999;
             foreach(var r in roomsList) {
                 if (r.fullRect.xMin < minX) minX = r.fullRect.xMin;
                 if (r.fullRect.yMin < minY) minY = r.fullRect.yMin;
                 if (r.fullRect.xMax > maxX) maxX = r.fullRect.xMax;
                 if (r.fullRect.yMax > maxY) maxY = r.fullRect.yMax;
             }
             minX -= 2; minY -= 2; maxX += 2; maxY += 2;
             width = maxX - minX;
             height = maxY - minY;

             map = new char[width, height];
             for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) map[x,y] = ' ';

             foreach(var r in roomsList) {
                 for (int x = r.fullRect.xMin; x < r.fullRect.xMax; x++) {
                     for (int y = r.fullRect.yMin; y < r.fullRect.yMax; y++) {
                         map[x - minX, y - minY] = 'W';
                     }
                 }
             }
             foreach(var r in roomsList) {
                 for (int x = r.groundRect.xMin; x < r.groundRect.xMax; x++) {
                     for (int y = r.groundRect.yMin; y < r.groundRect.yMax; y++) {
                         map[x - minX, y - minY] = 'G';
                     }
                 }
             }
             foreach(var c in corridors) {
                 int sx = c.start.x - minX, sy = c.start.y - minY;
                 int ex = c.end.x - minX, ey = c.end.y - minY;
                 
                 int stepX = sx < ex ? 1 : -1;
                 for (int x = sx; x != ex; x += stepX) map[x, sy] = 'C';

                 int stepY = sy < ey ? 1 : -1;
                 for (int y = sy; y != ey; y += stepY) map[ex, y] = 'C';
                 map[ex, ey] = 'C'; // Ensures the final point is set
             }
        }

        if (map != null && roomsList != null) 
        {
             // Determine glass
             for (int i = 0; i < roomsList.Count; i++) {
                 if (i == 0) continue; // ana odada cam olmasın
                 var r = roomsList[i];
                 int glassY = r.groundRect.yMax + r.topWallThickness / 2;
                 for (int x = r.groundRect.xMin + 1; x < r.groundRect.xMax - 1; x++) {
                     if (map[x - minX, glassY - minY] == 'W') { 
                         if (UnityEngine.Random.value > 0.5f && map[x - minX - 1, glassY - minY] != 'O') 
                             map[x - minX, glassY - minY] = 'O'; 
                     }
                 }
             }

             // Determine doors!
             foreach(var r in roomsList) {
                 List<Vector2Int> borders = new List<Vector2Int>();
                 for (int y = r.groundRect.yMin; y < r.groundRect.yMax; y++) {
                     borders.Add(new Vector2Int(r.groundRect.xMin - 1, y)); // Left
                     borders.Add(new Vector2Int(r.groundRect.xMax, y)); // Right
                 }
                 for (int x = r.groundRect.xMin; x < r.groundRect.xMax; x++) {
                     borders.Add(new Vector2Int(x, r.groundRect.yMin - 1)); // Bottom
                     borders.Add(new Vector2Int(x, r.groundRect.yMax)); // Top Inner
                 }

                 foreach(var b in borders) {
                     int bx = b.x - minX;
                     int by = b.y - minY;
                     if (map[bx, by] == 'C') {
                         bool isBetweenWalls = (bx > 0 && bx < width - 1 && map[bx - 1, by] == 'W' && map[bx + 1, by] == 'W') ||
                                               (by > 0 && by < height - 1 && map[bx, by - 1] == 'W' && map[bx, by + 1] == 'W');

                         if (isBetweenWalls) {
                             bool hasAdjD = false;
                             if (bx > 0 && map[bx - 1, by] == 'D') hasAdjD = true;
                             if (by > 0 && map[bx, by - 1] == 'D') hasAdjD = true;
                             if (bx < width - 1 && map[bx + 1, by] == 'D') hasAdjD = true;
                             if (by < height - 1 && map[bx, by + 1] == 'D') hasAdjD = true;

                             if (hasAdjD) {
                                 map[bx, by] = 'G'; // Convert extra parallel door to open passage
                             } else {
                                 map[bx, by] = 'D'; // Standard Door
                             }
                         } else {
                             map[bx, by] = 'G'; // Convert invalid door position to open passage
                         }
                     }
                 }
             }

             // -- ADD MAIN ENTRANCE DOOR --
             List<System.Tuple<Vector2Int, Vector2Int>> entranceCandidates = new List<System.Tuple<Vector2Int, Vector2Int>>();
             RoomData mRoom = roomsList[0];
             
             for (int y = mRoom.groundRect.yMin + 2; y < mRoom.groundRect.yMax - 2; y++) {
                 entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(mRoom.groundRect.xMin - 1, y), Vector2Int.left));
                 entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(mRoom.groundRect.xMax, y), Vector2Int.right));
             }
             for (int x = mRoom.groundRect.xMin + 2; x < mRoom.groundRect.xMax - 2; x++) {
                 entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(x, mRoom.groundRect.yMin - 1), Vector2Int.down));
                 entranceCandidates.Add(new System.Tuple<Vector2Int, Vector2Int>(new Vector2Int(x, mRoom.groundRect.yMax), Vector2Int.up));
             }

             // Shuffle
             for (int i = 0; i < entranceCandidates.Count; i++) {
                 int rIdx = UnityEngine.Random.Range(i, entranceCandidates.Count);
                 var temp = entranceCandidates[i];
                 entranceCandidates[i] = entranceCandidates[rIdx];
                 entranceCandidates[rIdx] = temp;
             }

             bool entrancePlaced = false;
             foreach (var candidate in entranceCandidates) {
                 int bx = candidate.Item1.x - minX;
                 int by = candidate.Item1.y - minY;
                 Vector2Int dir = candidate.Item2;

                 if (map[bx, by] == 'W') {
                     bool hasCloseDoor = false;
                     // Prevent placement too close to other connections
                     for (int dx = -4; dx <= 4; dx++) {
                         for (int dy = -4; dy <= 4; dy++) {
                             int cx = bx + dx, cy = by + dy;
                             if (cx >= 0 && cx < width && cy >= 0 && cy < height) {
                                 if (map[cx, cy] == 'D') {
                                     hasCloseDoor = true;
                                     break;
                                 }
                             }
                         }
                         if (hasCloseDoor) break;
                     }

                     if (!hasCloseDoor) {
                         map[bx, by] = 'D';

                         // Pierce thick wall on top so player can walk out cleanly
                         if (dir == Vector2Int.up) {
                             int thick = mRoom.topWallThickness;
                             for (int t = 1; t <= thick; t++) {
                                 int py = by + t;
                                 if (py < height) {
                                     map[bx, py] = 'G';
                                 }
                             }
                         }

                         entrancePlaced = true;
                         break;
                     }
                 }
             }
             // -- END MAIN ENTRANCE DOOR --

             // Explicitly wrap corridors and doors with walls so WFC doesn't expand them into huge ground patches
             for (int x = 0; x < width; x++) {
                 for (int y = 0; y < height; y++) {
                     if (map[x,y] == 'C' || map[x,y] == 'D') {
                         for (int dx = -1; dx <= 1; dx++) {
                             for (int dy = -1; dy <= 1; dy++) {
                                 int wX = x + dx;
                                 int wY = y + dy;
                                 if (wX >= 0 && wX < width && wY >= 0 && wY < height) {
                                     if (map[wX, wY] == ' ') {
                                         map[wX, wY] = 'W';
                                     }
                                 }
                             }
                         }
                     }
                 }
             }

             // Corridors to Ground
             for (int x = 0; x < width; x++) {
                 for (int y = 0; y < height; y++) {
                     if (map[x,y] == 'C') map[x,y] = 'G';
                 }
             }
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
            model.AddAdjacency(topTile, bottomTile, 0, -1, 0); // using x, y, z overload if possible, else we use 2/3
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
                        }
                }
            }
        }

        DeBroglie.Resolution status = propagator.Run();

        Debug.Log("DeBroglie Run Status: " + status.ToString());

        if (status == DeBroglie.Resolution.Contradiction)
        {
            Debug.LogError("Wave Function Collapse failed with a contradiction.");
            return;
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
    }

    private string GetBaseName(string variant)
    {
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
                instance.transform.localPosition = new Vector3(x * tileSize, y * tileSize, 0);
            }
            else
            {
                instance.transform.localPosition = new Vector3(x * tileSize, 0, y * tileSize);
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
}
