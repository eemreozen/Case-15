using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DFSRoomGen : MonoBehaviour
{
    [Header("Ana Oda Ayarları")]
    public int minMainWidth = 8, maxMainWidth = 14;
    public int minMainHeight = 8, maxMainHeight = 14;

    [Header("Yan Oda Ayarları")]
    public int maxRooms = 10; // Ana oda hariç eklenecek yan oda sayısı
    public int minWidth = 4, maxWidth = 8;
    public int minHeight = 4, maxHeight = 8;

    [Header("Koridor Ayarları")]
    public int minCorridor = 2; // En kısa koridor
    public int maxCorridor = 5; // En uzun koridor

    [Header("Çizim (Tilemap)")]
    public Tilemap floorTilemap;
    public TileBase floorTile;

    private List<RectInt> rooms = new List<RectInt>();

    void Start()
    {
        // 1. Ana Odayı Rastgele Boyutta Oluştur (Merkez)
        int mainW = Random.Range(minMainWidth, maxMainWidth + 1);
        int mainH = Random.Range(minMainHeight, maxMainHeight + 1);
        RectInt mainRoom = new RectInt(0, 0, mainW, mainH);
        
        rooms.Add(mainRoom);
        DrawRoom(mainRoom);

        // 2. Yan Odaları Ana Odadan Dallandır
        GenerateHubRooms(mainRoom);
    }

    void GenerateHubRooms(RectInt mainRoom)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        int attempts = 0; 
        int currentRooms = 0;

        // Yer bulma şansını artırmak için deneme sınırını 500 yaptık
        while (currentRooms < maxRooms && attempts < 500)
        {
            attempts++;

            Vector2Int dir = dirs[Random.Range(0, dirs.Length)];

            int nw = Random.Range(minWidth, maxWidth + 1);
            int nh = Random.Range(minHeight, maxHeight + 1);
            int corridorLen = Random.Range(minCorridor, maxCorridor + 1);

            int nx = 0, ny = 0;

            // Odaları her duvarın tam ortasına DEĞİL, duvar boyunca rastgele bir yere hizala
            if (dir == Vector2Int.up)
            {
                nx = Random.Range(mainRoom.xMin - nw + 2, mainRoom.xMax - 2);
                ny = mainRoom.yMax + corridorLen;
            }
            else if (dir == Vector2Int.down)
            {
                nx = Random.Range(mainRoom.xMin - nw + 2, mainRoom.xMax - 2);
                ny = mainRoom.yMin - corridorLen - nh;
            }
            else if (dir == Vector2Int.right)
            {
                nx = mainRoom.xMax + corridorLen;
                ny = Random.Range(mainRoom.yMin - nh + 2, mainRoom.yMax - 2);
            }
            else if (dir == Vector2Int.left)
            {
                nx = mainRoom.xMin - corridorLen - nw;
                ny = Random.Range(mainRoom.yMin - nh + 2, mainRoom.yMax - 2);
            }

            RectInt newRoom = new RectInt(nx, ny, nw, nh);

            // Çakışma kontrolü
            bool overlap = false;
            foreach (var r in rooms)
            {
                RectInt expanded = new RectInt(r.x - 1, r.y - 1, r.width + 2, r.height + 2);
                if (expanded.Overlaps(newRoom)) { overlap = true; break; }
            }

            // Çakışma yoksa odayı çiz ve koridoru ana odaya dik bir şekilde bağla
            if (!overlap)
            {
                rooms.Add(newRoom);
                DrawRoom(newRoom);

                // Koridorların L çizmemesi ve düz girmesi için ufak bir hile:
                if (dir == Vector2Int.up || dir == Vector2Int.down)
                    DrawCorridor(newRoom.center, new Vector2(newRoom.center.x, mainRoom.center.y));
                else
                    DrawCorridor(newRoom.center, new Vector2(mainRoom.center.x, newRoom.center.y));

                currentRooms++;
            }
        }
    }

    void DrawRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }
    }

    void DrawCorridor(Vector2 pos1, Vector2 pos2)
    {
        Vector3Int start = new Vector3Int(Mathf.RoundToInt(pos1.x), Mathf.RoundToInt(pos1.y), 0);
        Vector3Int end = new Vector3Int(Mathf.RoundToInt(pos2.x), Mathf.RoundToInt(pos2.y), 0);

        int stepX = start.x < end.x ? 1 : -1;
        for (int x = start.x; x != end.x; x += stepX) floorTilemap.SetTile(new Vector3Int(x, start.y, 0), floorTile);

        int stepY = start.y < end.y ? 1 : -1;
        for (int y = start.y; y != end.y; y += stepY) floorTilemap.SetTile(new Vector3Int(end.x, y, 0), floorTile);
    }
}