using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UniversalInteractiveArchitect
{
    #region Persistence & Data Architecture
    public enum CameraPerspectiveType { Free_3D, TopDown_2D, SideScroller_2D }
    public enum EnvironmentType { Indoor, Forest, Dungeon, SpaceStation, City }

    [System.Serializable]
    public class InteractionCategory
    {
        public string categoryName = "New Category";
        public List<string> prefabIDs = new List<string>();
    }

    [System.Serializable]
    public class CanvasItem
    {
        public Rect rect;
        public string itemName;
        public Color itemColor;
        public bool isSelected;
        public bool isDragging;
        public Vector2 dragOffset;
    }

    // Asset Database (Persistence) model
    public class Core15Template : ScriptableObject
    {
        public TextAsset gddFile;
        public CameraPerspectiveType cameraPerspective;
        public EnvironmentType environmentType;
        public int mapWidth = 1920;
        public int mapHeight = 1080;
        public int wallThickness = 20;
        public List<InteractionCategory> categories = new List<InteractionCategory>();
    }
    #endregion

    public class Core15ArchitectWindow : EditorWindow
    {
        private const string TEMPLATE_FOLDER = "Assets/Core15_Templates";

        #region Window State
        private int currentTab = 0;
        private readonly string[] tabNames = { "1. Asset Config", "2. Design Canvas", "3. Export & Story" };

        // Active template data
        private TextAsset gddFile;
        private CameraPerspectiveType cameraPerspective;
        private EnvironmentType environmentType;
        private int mapWidth = 1000;
        private int mapHeight = 800;
        private int wallThickness = 20;
        private List<InteractionCategory> categories = new List<InteractionCategory>();

        // Template Saving
        private string currentTemplateName = "New_Template";
        private string[] availableTemplateGUIDs;
        private string[] availableTemplateNames;
        private int selectedTemplateIndex = 0;

        // UI State
        private Vector2 tab1Scroll;
        private Vector2 tab3Scroll;
        private string systemLog = "";
        
        // Canvas & Selection State
        private List<CanvasItem> canvasItems = new List<CanvasItem>();
        private Vector2 panOffset = Vector2.zero;
        private float zoomScale = 1f;
        private string manualPrefabIDInput = "";
        private Rect canvasRect;

        // Mock Story State
        private bool isStoryGenerated = false;
        private string storyMainEvent = "";
        private string storyEvidence = "";
        private string storyCulprits = "";

        // NEW 3-Stage Memory
        private char[,] generatedMap;
        private List<PropInstance> placedProps;

        // Tree UI State
        private bool storyMainEventFoldout = true;
        private bool storyEvidenceFoldout = true;
        private bool storyCulpritsFoldout = true;
        private Vector2 miniMapPanOffset = Vector2.zero;
        private float miniMapZoom = 1f;
        #endregion

        [MenuItem("Window/Universal Interactive Architect/Core 15 Architect")]
        public static void ShowWindow()
        {
            var window = GetWindow<Core15ArchitectWindow>("Core 15 Architect");
            window.minSize = new Vector2(1200, 750);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureTemplateFolderExists();
            LoadTemplateList();
            if (categories.Count == 0)
                categories.Add(new InteractionCategory { categoryName = "Evidence", prefabIDs = new List<string> { "Blood_Stain_01" } });
        }

        #region Main GUI
        private void OnGUI()
        {
            DrawHeader();
            currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(30));

            switch (currentTab)
            {
                case 0: DrawTab1_AssetConfig(); break;
                case 1: DrawTab2_DesignCanvas(); break;
                case 2: DrawTab3_ExportStory(); break;
            }

            if (GUI.changed || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.ScrollWheel)
            {
                Repaint();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Template Name:", GUILayout.Width(100));
            currentTemplateName = EditorGUILayout.TextField(currentTemplateName, GUILayout.Width(200));

            if (GUILayout.Button("Save Template", EditorStyles.toolbarButton, GUILayout.Width(120)))
                SaveCurrentTemplate();

            GUILayout.FlexibleSpace();

            GUILayout.Label("Loaded Templates:", GUILayout.Width(110));
            if (availableTemplateNames != null && availableTemplateNames.Length > 0)
            {
                int newIdx = EditorGUILayout.Popup(selectedTemplateIndex, availableTemplateNames, EditorStyles.toolbarPopup, GUILayout.Width(200));
                if (newIdx != selectedTemplateIndex)
                {
                    selectedTemplateIndex = newIdx;
                    LoadTemplateFromIndex(selectedTemplateIndex);
                }
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                LoadTemplateList();

            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region TAB 1: Asset Configuration & Lore
        private void DrawTab1_AssetConfig()
        {
            tab1Scroll = EditorGUILayout.BeginScrollView(tab1Scroll);
            GUILayout.Space(10);

            // GDD Section
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Project Lore (GDD)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            gddFile = (TextAsset)EditorGUILayout.ObjectField("GDD Document", gddFile, typeof(TextAsset), false, GUILayout.Width(350));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Optional: Helps AI understand your game's world. (.txt or .md)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Static Assets Selection
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Static Assets Scanner", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Static Prefabs Folder", GUILayout.Height(30), GUILayout.Width(250)))
            {
                AddLog("Static assets folder scanned successfully (Mock).");
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("(Auto-imports IDs for generic walls/floors)", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Dynamic Interaction Pool
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Dynamic Interaction Pool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ New Category", GUILayout.Height(30), GUILayout.Width(200)))
                categories.Add(new InteractionCategory());
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                EditorGUILayout.BeginVertical("helpbox");
                
                EditorGUILayout.BeginHorizontal();
                cat.categoryName = EditorGUILayout.TextField(cat.categoryName, GUILayout.Width(250));
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    categories.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                for (int j = 0; j < cat.prefabIDs.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    cat.prefabIDs[j] = EditorGUILayout.TextField("Prefab ID", cat.prefabIDs[j], GUILayout.Width(250));
                    
                    GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        cat.prefabIDs.RemoveAt(j);
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add Prefab ID", GUILayout.Width(150)))
                {
                    cat.prefabIDs.Add("New_ID_" + Random.Range(10, 99));
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region TAB 2: Design Canvas
        private void DrawTab2_DesignCanvas()
        {
            EditorGUILayout.BeginHorizontal();

            // LEFT PANEL
            GUILayout.BeginVertical("box", GUILayout.Width(280));
            GUILayout.Label("Visual Parameters", EditorStyles.boldLabel);
            
            cameraPerspective = (CameraPerspectiveType)EditorGUILayout.EnumPopup("Camera View", cameraPerspective);
            environmentType = (EnvironmentType)EditorGUILayout.EnumPopup("Environment", environmentType);

            GUILayout.Space(10);
            GUILayout.Label("Room Editor (W x H | Wall)", EditorStyles.miniBoldLabel);
            
            // Basic Visual Room Editor Mock
            EditorGUILayout.BeginHorizontal();
            mapWidth = EditorGUILayout.IntField(mapWidth, GUILayout.Width(60));
            GUILayout.Label("x", GUILayout.Width(15));
            mapHeight = EditorGUILayout.IntField(mapHeight, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            wallThickness = EditorGUILayout.IntField("Wall Thickness", wallThickness, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("Manual Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            manualPrefabIDInput = EditorGUILayout.TextField("Prefab ID", manualPrefabIDInput, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Object by ID", GUILayout.Width(200)))
            {
                if (!string.IsNullOrEmpty(manualPrefabIDInput))
                {
                    if (placedProps == null) placedProps = new List<PropInstance>();
                    float unscaledCenterX = (canvasRect.width/2 - panOffset.x) / (40f * zoomScale);
                    float rawY = (canvasRect.height/2 - panOffset.y) / (40f * zoomScale);
                    int mapHeight = generatedMap != null ? generatedMap.GetLength(1) : 20;
                    float unscaledCenterY = mapHeight - 1 - rawY;
                    
                    PropInstance newItem = new PropInstance {
                        propID = manualPrefabIDInput,
                        x = unscaledCenterX,
                        y = unscaledCenterY,
                        isSelected = true
                    };
                    DeselectAllItems();
                    placedProps.Add(newItem);
                    GUI.FocusControl(null);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
            if (GUILayout.Button("Delete Selected", GUILayout.Height(30), GUILayout.Width(200)))
            {
                if (placedProps != null) placedProps.RemoveAll(p => p.isSelected);
                canvasItems.RemoveAll(i => i.isSelected);
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.9f);
            if (GUILayout.Button("Generate Base Map", GUILayout.Height(40)))
            {
                GenerateBaseMap();
            }
            
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("Bind AI Scenario", GUILayout.Height(40)))
            {
                BindAIScenario();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();

            // RIGHT PANEL (Canvas)
            canvasRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.BeginGroup(canvasRect);
            
            EditorGUI.DrawRect(new Rect(0, 0, canvasRect.width, canvasRect.height), new Color(0.12f, 0.12f, 0.12f));

            Event e = Event.current;
            
            // Handle Pan & Zoom
            if (e.type == EventType.MouseDrag && (e.button == 2 || (e.button == 1 && e.alt)))
            {
                panOffset += e.delta;
                e.Use();
            }
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                float newZoom = Mathf.Clamp(zoomScale + zoomDelta, 0.2f, 3f);
                Vector2 mouseWorldBefore = (e.mousePosition - panOffset) / zoomScale;
                zoomScale = newZoom;
                Vector2 mouseWorldAfter = (e.mousePosition - panOffset) / zoomScale;
                panOffset += (mouseWorldAfter - mouseWorldBefore) * zoomScale;
                e.Use();
            }

            // Draw Grid manually
            Handles.color = new Color(1f, 1f, 1f, 0.05f);
            float step = 100f * zoomScale;
            if (step > 5f)
            {
                float offsetX = panOffset.x % step;
                float offsetY = panOffset.y % step;
                if (offsetX < 0) offsetX += step;
                if (offsetY < 0) offsetY += step;

                for (float x = offsetX; x < canvasRect.width; x += step)
                    Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvasRect.height));
                for (float y = offsetY; y < canvasRect.height; y += step)
                    Handles.DrawLine(new Vector3(0, y), new Vector3(canvasRect.width, y));
            }
            Handles.color = Color.white;

            float tSize = 40f;

            // Draw Room/Map Base
            if (generatedMap != null)
            {
                int width = generatedMap.GetLength(0);
                int height = generatedMap.GetLength(1);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        char c = generatedMap[x, y];
                        if (c == ' ') continue;

                        Color color = Color.black;
                        if (c == 'G') color = new Color(0.1f, 0.4f, 0.1f);
                        else if (c == 'W') color = Color.gray;
                        else if (c == 'D') color = new Color(0.5f, 0.25f, 0f);
                        else if (c == 'O') color = new Color(0.5f, 0.8f, 1f);

                        int mapHeight = height;
                        float drawY = (mapHeight - 1 - y) * tSize;
                        Rect tileRect = new Rect((x * tSize) * zoomScale + panOffset.x, drawY * zoomScale + panOffset.y, tSize * zoomScale, tSize * zoomScale);
                        
                        if (tileRect.Overlaps(new Rect(0, 0, canvasRect.width, canvasRect.height)))
                            EditorGUI.DrawRect(tileRect, color);
                    }
                }
            }


            // Draw and Handle Props (independent of generatedMap)
            if (placedProps != null)
            {
                int mapHeightProps = generatedMap != null ? generatedMap.GetLength(1) : 20;
                // Interaction
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    bool clickedItem = false;
                    for (int i = placedProps.Count - 1; i >= 0; i--)
                    {
                        float drawY = (mapHeightProps - 1 - placedProps[i].y) * tSize;
                        Rect pRect = new Rect((placedProps[i].x * tSize) * zoomScale + panOffset.x, drawY * zoomScale + panOffset.y, tSize * zoomScale, tSize * zoomScale);
                        if (pRect.Contains(e.mousePosition))
                        {
                            DeselectAllItems();
                            placedProps[i].isSelected = true;
                            placedProps[i].isDragging = true;
                            placedProps[i].dragOffset = e.mousePosition - new Vector2(pRect.x, pRect.y);
                            
                            var item = placedProps[i];
                            placedProps.RemoveAt(i);
                            placedProps.Add(item);
                            
                            clickedItem = true;
                            e.Use();
                            break;
                        }
                    }
                    if (!clickedItem) DeselectAllItems();
                }

                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    foreach (var prop in placedProps)
                    {
                        if (prop.isDragging)
                        {
                            Vector2 newScreenPos = e.mousePosition - prop.dragOffset;
                            prop.x = (newScreenPos.x - panOffset.x) / (tSize * zoomScale);
                            float rawY = (newScreenPos.y - panOffset.y) / (tSize * zoomScale);
                            prop.y = mapHeightProps - 1 - rawY;
                            e.Use();
                            break;
                        }
                    }
                }

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    foreach (var prop in placedProps) prop.isDragging = false;
                }

                foreach (var prop in placedProps)
                {
                    float drawY = (mapHeightProps - 1 - prop.y) * tSize;
                    Rect pRect = new Rect((prop.x * tSize) * zoomScale + panOffset.x, drawY * zoomScale + panOffset.y, tSize * zoomScale, tSize * zoomScale);
                    
                    if (prop.isSelected)
                    {
                        EditorGUI.DrawRect(new Rect(pRect.x - 4, pRect.y - 4, pRect.width + 8, pRect.height + 8), Color.yellow);
                    }

                    GUI.backgroundColor = prop.isSelected ? Color.yellow : Color.white;
                    GUI.skin.box.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.box.normal.textColor = Color.black;
                    string shortName = prop.propID.Length > 8 ? prop.propID.Substring(0, 8) : prop.propID;
                    
                    int fontSize = Mathf.RoundToInt(11 * zoomScale);
                    if (fontSize > 5) {
                        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                        boxStyle.fontSize = fontSize;
                        boxStyle.alignment = TextAnchor.MiddleCenter;
                        boxStyle.normal.textColor = Color.black;
                        GUI.Box(pRect, shortName, boxStyle);
                    } else {
                        GUI.Box(pRect, ""); 
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            GUI.EndGroup();
            
            // Info Overlay
            GUI.Label(new Rect(canvasRect.x + 10, canvasRect.y + 10, 200, 20), $"Zoom: {zoomScale:F2}x", EditorStyles.whiteMiniLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            Handles.color = new Color(1f, 1f, 1f, 0.05f);
            float step = 100f;
            for (float x = -2000; x < 4000; x += step)
                Handles.DrawLine(new Vector3(x, -2000), new Vector3(x, 4000));
            for (float y = -2000; y < 4000; y += step)
                Handles.DrawLine(new Vector3(-2000, y), new Vector3(4000, y));
            Handles.color = Color.white;
        }
        #endregion

        #region TAB 3: Export & Story
        private void DrawTab3_ExportStory()
        {
            EditorGUILayout.BeginHorizontal();

            // Mini Map Preview (Left)
            GUILayout.BeginVertical("box", GUILayout.Width(350));
            GUILayout.Label("Mini-Map Preview", EditorStyles.boldLabel);
            Rect miniMapRect = GUILayoutUtility.GetRect(330, 250);
            
            GUI.BeginGroup(miniMapRect);
            EditorGUI.DrawRect(new Rect(0, 0, miniMapRect.width, miniMapRect.height), new Color(0.1f, 0.1f, 0.1f));
            
            if (generatedMap != null)
            {
                int w = generatedMap.GetLength(0);
                int h = generatedMap.GetLength(1);
                float miniTSize = 10f * miniMapZoom;

                Event e = Event.current;
                
                // Pan
                if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2))
                {
                    if (new Rect(0, 0, miniMapRect.width, miniMapRect.height).Contains(e.mousePosition))
                    {
                        miniMapPanOffset += e.delta;
                        e.Use();
                    }
                }
                
                // Zoom
                if (e.type == EventType.ScrollWheel)
                {
                    if (new Rect(0, 0, miniMapRect.width, miniMapRect.height).Contains(e.mousePosition))
                    {
                        miniMapZoom -= e.delta.y * 0.05f;
                        miniMapZoom = Mathf.Clamp(miniMapZoom, 0.1f, 5f);
                        e.Use();
                    }
                }

                float mapPixelW = w * 10f * miniMapZoom;
                float mapPixelH = h * 10f * miniMapZoom;
                Vector2 baseOffset = new Vector2(miniMapRect.width / 2 - mapPixelW / 2 + miniMapPanOffset.x, miniMapRect.height / 2 - mapPixelH / 2 + miniMapPanOffset.y);
                
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        char c = generatedMap[x, y];
                        if (c == ' ') continue;

                        Color color = Color.black;
                        if (c == 'G') color = new Color(0.1f, 0.4f, 0.1f);
                        else if (c == 'W') color = Color.gray;
                        else if (c == 'D') color = new Color(0.5f, 0.25f, 0f);
                        else if (c == 'O') color = new Color(0.5f, 0.8f, 1f);

                        int mapHeight = h;
                        float drawY = (mapHeight - 1 - y) * miniTSize;
                        Rect tRect = new Rect(x * miniTSize + baseOffset.x, drawY + baseOffset.y, miniTSize, miniTSize);
                        EditorGUI.DrawRect(tRect, color);
                    }
                }

                if (placedProps != null)
                {
                    int mapHeight = h;
                    foreach (var prop in placedProps)
                    {
                        float drawY = (mapHeight - 1 - prop.y) * miniTSize;
                        Rect pRect = new Rect(prop.x * miniTSize + baseOffset.x, drawY + baseOffset.y, miniTSize, miniTSize);
                        GUI.backgroundColor = Color.yellow;
                        GUI.Box(pRect, "");
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            GUI.EndGroup();
            GUILayout.EndVertical();

            // Story Tree & Build (Right)
            tab3Scroll = EditorGUILayout.BeginScrollView(tab3Scroll, "box");
            GUILayout.Label("Generated AI Scenario Tree", EditorStyles.largeLabel);
            GUILayout.Space(10);

            // Mock tree structure
            storyMainEventFoldout = EditorGUILayout.Foldout(storyMainEventFoldout, "Main Event: The Midnight Incident", true, EditorStyles.foldoutHeader);
            if (storyMainEventFoldout)
            {
                EditorGUI.indentLevel++;
                storyEvidenceFoldout = EditorGUILayout.Foldout(storyEvidenceFoldout, "Evidences & Artifacts", true);
                if (storyEvidenceFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("- Broken Glass (Physical)");
                    EditorGUILayout.LabelField("- Blood Stain (Biological)");
                    EditorGUILayout.LabelField("- Footprints (Physical)");
                    EditorGUI.indentLevel--;
                }
                storyCulpritsFoldout = EditorGUILayout.Foldout(storyCulpritsFoldout, "Potential Culprits", true);
                if (storyCulpritsFoldout)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("- Unknown Intruder");
                    EditorGUILayout.LabelField("- Rogue AI Unit");
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("BUILD TO UNITY SCENE", GUILayout.Height(60)))
            {
                BuildToUnityScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        private void DeselectAllItems()
        {
            if (placedProps != null)
            {
                foreach (var p in placedProps) p.isSelected = false;
            }
            foreach (var i in canvasItems) i.isSelected = false;
        }

        #region Actions & Logic
        private void GenerateBaseMap()
        {
            canvasItems.Clear();
            isStoryGenerated = false;

            string path = Application.dataPath + "/llm-test.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                LLMResponseWrapper wrapper = JsonUtility.FromJson<LLMResponseWrapper>(json);
                generatedMap = Core15WFCGenerator.GenerateMapData(wrapper, "Assets/IndoorRules.xml", out placedProps);
                AddLog("Base map generated logically from JSON.");
            }
            else
            {
                AddLog("JSON file not found at: " + path + ". Generating with dummy data.");
                LLMResponseWrapper dummy = new LLMResponseWrapper();
                dummy.rooms = new List<LLMRoomRequest> {
                    new LLMRoomRequest { id = "Lobby", width = 10, height = 10, connectTo = "", direction = "", corridorLen = 0 },
                    new LLMRoomRequest { id = "Office", width = 6, height = 6, connectTo = "Lobby", direction = "Right", corridorLen = 3 }
                };
                dummy.roomPropPools = new List<RoomPropPool>();
                generatedMap = Core15WFCGenerator.GenerateMapData(dummy, "Assets/IndoorRules.xml", out placedProps);
            }

            // Center map in canvas
            if (generatedMap != null && canvasRect.width > 0)
            {
                int w = generatedMap.GetLength(0);
                int h = generatedMap.GetLength(1);
                float mapPixelW = w * 40f * zoomScale;
                float mapPixelH = h * 40f * zoomScale;
                panOffset = new Vector2(canvasRect.width / 2 - mapPixelW / 2, canvasRect.height / 2 - mapPixelH / 2);
            }
        }

        private void BindAIScenario()
        {
            bool hasValidCat = false;
            foreach (var cat in categories)
            {
                if (cat.prefabIDs.Count > 0)
                {
                    hasValidCat = true;
                    string selectedID = cat.prefabIDs[Random.Range(0, cat.prefabIDs.Count)];
                    Color cColor = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
                    
                    float randX = Random.Range(wallThickness, mapWidth - 150f);
                    float randY = cameraPerspective == CameraPerspectiveType.SideScroller_2D ? (mapHeight - wallThickness - 50f - Random.Range(0, 100)) : Random.Range(wallThickness, mapHeight - 80f);

                    canvasItems.Add(new CanvasItem {
                        rect = new Rect(randX, randY, 140, 55),
                        itemName = $"[{cat.categoryName}]\n{selectedID}",
                        itemColor = cColor
                    });
                }
            }

            if (hasValidCat)
            {
                isStoryGenerated = true;
                storyMainEvent = "Main Event: A complex incident occurred within the generated perimeter.";
                storyEvidence = $"Evidence Found: Linked via AI generation. Environment: {environmentType}";
                storyCulprits = $"Culprits: Actors mapped from Dynamic Interaction Pool.";
                
                currentTab = 2; // Auto-switch to Export & Story tab
            }
        }

        private void BuildToUnityScene()
        {
            if (generatedMap == null)
            {
                AddLog("No generated map in memory. Generate base map first.");
                return;
            }

            GameObject root = new GameObject("Core15_GeneratedLevel");
            float tileSize = 1f;

            int width = generatedMap.GetLength(0);
            int height = generatedMap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    char c = generatedMap[x, y];
                    if (c == ' ') continue;

                    string prefabName = "";
                    if (c == 'G') prefabName = "Ground";
                    else if (c == 'W') prefabName = "Wall";
                    else if (c == 'D') prefabName = "Door";
                    else if (c == 'O') prefabName = "Glass";

                    if (!string.IsNullOrEmpty(prefabName))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
                        GameObject instance;
                        if (prefab != null)
                        {
                            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                        }
                        else
                        {
                            instance = GameObject.CreatePrimitive(c == 'G' ? PrimitiveType.Plane : PrimitiveType.Cube);
                            instance.name = prefabName + "_Fallback";
                            instance.transform.parent = root.transform;
                        }

                        float xPos = (x * tileSize) + (tileSize * 0.5f);
                        float yPos = (y * tileSize) + (tileSize * 0.5f);

                        if (cameraPerspective == CameraPerspectiveType.Free_3D)
                        {
                            // 3D Serbest Kamera: Temel objeler XZ düzlemine dizilir (Y = 0)
                            instance.transform.localPosition = new Vector3(xPos, 0f, yPos);
                        }
                        else 
                        {
                            // 2D (Top-Down veya Side-Scroller): Temel objeler XY düzlemine dizilir (Z = 0)
                            instance.transform.localPosition = new Vector3(xPos, yPos, 0f);
                        }
                    }
                }
            }

            if (placedProps != null)
            {
                foreach (var prop in placedProps)
                {
                    string basePropName = prop.propID;
                    int num = 0;
                    int lastSpace = basePropName.LastIndexOf(' ');
                    if (lastSpace > 0)
                    {
                        string possibleNumber = basePropName.Substring(lastSpace + 1);
                        if (int.TryParse(possibleNumber, out int parsedNum))
                        {
                            num = parsedNum;
                            basePropName = basePropName.Substring(0, lastSpace);
                        }
                    }

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + basePropName + ".prefab");
                    GameObject instance;
                    if (prefab != null)
                    {
                        instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    }
                    else
                    {
                        instance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        instance.name = basePropName + "_Fallback";
                        instance.transform.parent = root.transform;
                    }

                    float xPos = (prop.x * tileSize) + (tileSize * 0.5f);
                    float yPos = (prop.y * tileSize) + (tileSize * 0.5f);

                    if (cameraPerspective == CameraPerspectiveType.Free_3D)
                    {
                        // 3D Düzlem: Eşyalar XZ düzleminde (Y ekseninde hafifçe yukarıda)
                        instance.transform.localPosition = new Vector3(xPos, 0.1f, yPos);
                        
                        // KESİN ROTASYON KURALI: 
                        // Prefab'ler varsayılan olarak XY düzlemine (ekrana) dik bakıyorsa, 
                        // onları 3D dünyada yere yatırmak için X ekseninde 90 derece döndürüyoruz.
                        // Y ekseninde ise (num * 90f) ile WFC varyant dönüşlerini uyguluyoruz.
                        // Not: İleride prefab'leri zaten 3D model olarak doğru eksenlerde (X=0) hazırlarsan, buradaki 90f'i 0f yapabilirsin.
                        instance.transform.localRotation = Quaternion.Euler(90f, num * 90f, 0f);
                    }
                    else
                    {
                        // 2D Düzlem: Eşyalar XY düzleminde (Z ekseninde hafifçe önde)
                        instance.transform.localPosition = new Vector3(xPos, yPos, -0.1f);
                        
                        // 2D rotasyon her zaman Z ekseninden döner
                        instance.transform.localRotation = Quaternion.Euler(0f, 0f, num * -90f);
                    }
                }
            }

            Selection.activeGameObject = root;
            AddLog("Successfully built to Unity Scene from Logic Map.");
        }


        #endregion

        #region Persistence & File Utility
        private void EnsureTemplateFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(TEMPLATE_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Core15_Templates");
            }
        }

        private void SaveCurrentTemplate()
        {
            EnsureTemplateFolderExists();
            string path = $"{TEMPLATE_FOLDER}/{currentTemplateName}.asset";
            
            Core15Template tmpl = AssetDatabase.LoadAssetAtPath<Core15Template>(path);
            if (tmpl == null)
            {
                tmpl = ScriptableObject.CreateInstance<Core15Template>();
                AssetDatabase.CreateAsset(tmpl, path);
            }

            tmpl.gddFile = gddFile;
            tmpl.cameraPerspective = cameraPerspective;
            tmpl.environmentType = environmentType;
            tmpl.mapWidth = mapWidth;
            tmpl.mapHeight = mapHeight;
            tmpl.wallThickness = wallThickness;
            
            tmpl.categories = new List<InteractionCategory>();
            foreach (var c in categories) {
                tmpl.categories.Add(new InteractionCategory { categoryName = c.categoryName, prefabIDs = new List<string>(c.prefabIDs) });
            }

            EditorUtility.SetDirty(tmpl);
            AssetDatabase.SaveAssets();
            LoadTemplateList();
        }

        private void LoadTemplateList()
        {
            availableTemplateGUIDs = AssetDatabase.FindAssets("t:Core15Template", new[] { TEMPLATE_FOLDER });
            if (availableTemplateGUIDs.Length > 0)
            {
                availableTemplateNames = new string[availableTemplateGUIDs.Length];
                for (int i = 0; i < availableTemplateGUIDs.Length; i++)
                    availableTemplateNames[i] = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(availableTemplateGUIDs[i]));
            }
            else availableTemplateNames = new string[0];
        }

        private void LoadTemplateFromIndex(int index)
        {
            if (availableTemplateGUIDs == null || index < 0 || index >= availableTemplateGUIDs.Length) return;
            string path = AssetDatabase.GUIDToAssetPath(availableTemplateGUIDs[index]);
            Core15Template tmpl = AssetDatabase.LoadAssetAtPath<Core15Template>(path);

            if (tmpl != null)
            {
                currentTemplateName = availableTemplateNames[index];
                gddFile = tmpl.gddFile;
                cameraPerspective = tmpl.cameraPerspective;
                environmentType = tmpl.environmentType;
                mapWidth = tmpl.mapWidth;
                mapHeight = tmpl.mapHeight;
                wallThickness = tmpl.wallThickness;

                categories.Clear();
                foreach (var c in tmpl.categories)
                    categories.Add(new InteractionCategory { categoryName = c.categoryName, prefabIDs = new List<string>(c.prefabIDs) });
            }
        }

        private void AddLog(string msg) { Debug.Log($"[Core15] {msg}"); }
        #endregion
    }
}
