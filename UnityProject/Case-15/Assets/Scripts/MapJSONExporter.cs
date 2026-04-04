using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class TileData
{
    public int x;
    public int y;
    public string type;
}

[System.Serializable]
public class MapExportData
{
    public string description = "Top-down 2D grid representation of the generated building. (0,0) is bottom-left.";
    public int width;
    public int height;
    public List<string> asciiMap = new List<string>();
    public List<TileData> doors = new List<TileData>();
    public List<TileData> windows = new List<TileData>();
    public List<TileData> floors = new List<TileData>();
}

[RequireComponent(typeof(DeBroglieGenerator))]
public class MapJSONExporter : MonoBehaviour
{
    public string exportPath = "Assets/MapExport.json";

    [ContextMenu("Generate JSON")]
    public void GenerateJSON()
    {
        DeBroglieGenerator generator = GetComponent<DeBroglieGenerator>();
        if (generator == null)
        {
            Debug.LogError("DeBroglieGenerator not found on this GameObject!");
            return;
        }

        float tileSize = generator.tileSize;
        bool is2D = generator.is2D;

        if (tileSize <= 0) tileSize = 1f; // Fallback to avoid division by zero

        List<TileData> allTiles = new List<TileData>();

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        // Extract raw coordinates based on transform positions
        foreach (Transform child in transform)
        {
            int gridX = 0;
            int gridY = 0;

            if (is2D)
            {
                gridX = Mathf.RoundToInt(child.localPosition.x / tileSize);
                gridY = Mathf.RoundToInt(child.localPosition.y / tileSize);
            }
            else
            {
                gridX = Mathf.RoundToInt(child.localPosition.x / tileSize);
                gridY = Mathf.RoundToInt(child.localPosition.z / tileSize);
            }

            string tileName = child.name.ToLower();
            string abstractType = "";

            if (tileName.Contains("door")) abstractType = "Door";
            else if (tileName.Contains("glass") || tileName.Contains("window")) abstractType = "Window";
            else if (tileName.Contains("ground") || tileName.Contains("floor")) abstractType = "Ground";
            else abstractType = "Wall"; // Or just ignore generic walls to save space, but let's keep them out of standard lists if not needed, we'll just track bounds

            if (gridX < minX) minX = gridX;
            if (gridX > maxX) maxX = gridX;
            if (gridY < minY) minY = gridY;
            if (gridY > maxY) maxY = gridY;

            if (abstractType == "Door" || abstractType == "Window" || abstractType == "Ground")
            {
                allTiles.Add(new TileData { x = gridX, y = gridY, type = abstractType });
            }
        }

        if (allTiles.Count == 0)
        {
            Debug.LogWarning("No relevant tiles found to export. Make sure you generated the map first.");
            return;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        MapExportData exportData = new MapExportData();
        exportData.width = width;
        exportData.height = height;

        // Normalize coordinates to start from (0,0) instead of negatives for LLM clarity
        char[,] gridMap = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridMap[x, y] = ' '; // Empty space / Unwalkable
            }
        }

        foreach (var t in allTiles)
        {
            int nx = t.x - minX;
            int ny = t.y - minY;

            TileData normalizedTile = new TileData { x = nx, y = ny, type = t.type };

            if (t.type == "Ground") 
            {
                gridMap[nx, ny] = '.';
                exportData.floors.Add(normalizedTile);
            }
            else if (t.type == "Door") 
            {
                gridMap[nx, ny] = 'D';
                exportData.doors.Add(normalizedTile);
            }
            else if (t.type == "Window") 
            {
                gridMap[nx, ny] = 'W';
                exportData.windows.Add(normalizedTile);
            }
        }

        // Build ASCII Map Representation
        // For array visuals, we typically print from top to bottom (Y max to Y min)
        for (int y = height - 1; y >= 0; y--)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++)
            {
                row[x] = gridMap[x, y];
            }
            exportData.asciiMap.Add(new string(row));
        }

        string jsonOutput = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(exportPath, jsonOutput);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log($"Map JSON successfully exported to {exportPath}! Included LLM-friendly ASCII layout.");
    }
}
