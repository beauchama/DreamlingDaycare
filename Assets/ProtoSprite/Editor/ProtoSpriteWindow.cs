using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEditor.SceneManagement;
using System.Reflection;
using System.Linq;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace ProtoSprite.Editor
{
    public class ProtoSpriteWindow : EditorWindow, IHasCustomMenu
    {
        public const int kMaxTextureSize = 2048;
        public const string kToolShortcutsTag = "ProtoSprite.ToolShortcuts";
        readonly InternalEngineBridge.ShortcutContext m_ShortcutContext = new InternalEngineBridge.ShortcutContext();

        [SerializeField] private string m_LastSaveFolder = "";

        Type[] m_DefaultToolTypes =
            {
                typeof(PaintTool),
                typeof(EraseTool),
                typeof(FillTool),
                typeof(ResizeTool),
                typeof(PivotTool),
                typeof(SelectTool),
                typeof(CreationTool)
            };

        [NonSerialized] List<Type> m_AllToolTypes = new List<Type>();

        [NonSerialized] List<ProtoSpriteTool> m_AllToolInstances = new List<ProtoSpriteTool>();

        ColorPalette m_ColorPalette = null;

        bool m_DebugRender = false;

        static ProtoSpriteWindow s_Instance = null;

        public string LastSaveFolder { get => m_LastSaveFolder; set => m_LastSaveFolder = value; }
		public List<ProtoSpriteTool> AllToolInstances { get
            {
                ValidateTools();

                return m_AllToolInstances;
            }
        }

		public ColorPalette ColorPalette { get => m_ColorPalette; }

        public bool DebugRender { get => m_DebugRender;
            set
            {
                m_DebugRender = value;
                EditorPrefs.SetInt("ProtoSprite.Editor.ProtoSpriteWindow.DebugRender", m_DebugRender ? 1 : 0);
            }
        }

		public static ProtoSpriteWindow GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = GetWindow<ProtoSpriteWindow>("ProtoSprite", false);
                s_Instance.wantsMouseMove = true;
            }
            return s_Instance;
        }

        void OnEnable()
        {
            m_ColorPalette = new ColorPalette();
            m_ColorPalette.OnEnable();

            m_DebugRender = EditorPrefs.GetInt("ProtoSprite.Editor.ProtoSpriteWindow.DebugRender", 0) == 0 ? false : true;

            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            bool isValid = IsSelectionValidProtoSprite(out string invalidReason, out bool isAutoFixable);

            if (Event.current.type == EventType.Repaint && isValid && DebugRender && ToolManager.activeToolType != null && ToolManager.activeToolType.IsSubclassOf(typeof(ProtoSpriteTool)))
            {
                PaintTool.DrawDebugSprite(Selection.activeGameObject.GetComponent<SpriteRenderer>(), sceneView);
            }
        }

        public static void ToggleTool<T>() where T : ProtoSpriteTool
        {
            if (ToolManager.activeToolType == typeof(T))
            {
                ToolManager.RestorePreviousPersistentTool();
            }
            else
            {
                ToolManager.SetActiveTool(GetInstance().GetToolInstance(typeof(T)));
            }
        }

        public EditorTool GetToolInstance(Type toolType)
        {
            if (!toolType.IsSubclassOf(typeof(ProtoSpriteTool)))
                return null;

            foreach (var instance in AllToolInstances)
            {
                if (instance.GetType() == toolType)
                {
                    return instance;
                }
            }

            throw new System.Exception("Couldn't find tool instance for type: " + toolType);
        }

        public T GetToolInstance<T>() where T : ProtoSpriteTool
        {
            if (!typeof(T).IsSubclassOf(typeof(ProtoSpriteTool)))
                return null;

            return (T)GetToolInstance(typeof(T));
        }

        [MenuItem("Window/2D/ProtoSprite")]
        public static void OpenWindow()
        {
            GetInstance();
        }

        private void Update()
        {
            Repaint();
        }

		void ValidateTools()
        {
            bool shouldRecreate = false;

            for (int i = 0; i < m_AllToolInstances.Count; i++)
            {
                if (m_AllToolInstances[i] == null)
                {
                    shouldRecreate = true;
                    m_AllToolInstances.RemoveAt(i);
                    i--;
                }
            }

            if (m_AllToolInstances.Count == 0)
                shouldRecreate = true;

            if (!shouldRecreate)
                return;

            for (int i = 0; i < m_AllToolInstances.Count; i++)
            {
                DestroyImmediate(m_AllToolInstances[i]);
            }

            m_AllToolInstances.Clear();

            m_AllToolTypes.Clear();

            foreach (var defaultToolType in m_DefaultToolTypes)
            {
                m_AllToolTypes.Add(defaultToolType);
            }

            foreach (var foundToolType in TypeCache.GetTypesDerivedFrom(typeof(ProtoSpriteTool)))
            {
                if (foundToolType.IsAbstract)
                    continue;

                if (m_AllToolTypes.Contains(foundToolType))
                    continue;

                m_AllToolTypes.Add(foundToolType);
            }

            foreach (var toolType in m_AllToolTypes)
            {
                var toolInstance = (ProtoSpriteTool)CreateInstance(toolType);
                m_AllToolInstances.Add(toolInstance);
            }
        }

        private void OnGUI()
        {
            Event e = Event.current;

            bool isValid = IsSelectionValidProtoSprite(out string invalidReason, out bool isAutoFixable);

            // Top menu bar
            float menuBarHeight = 20;
            Rect menuBar = new Rect(0, 0, position.width, menuBarHeight);

            GUILayout.BeginArea(menuBar, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            // Populate add/+ menu
            {
                var guiContent = new GUIContent(EditorGUIUtility.IconContent("CreateAddNew"));
                guiContent.tooltip = "Create a new GameObject and associated sprite texture file.";
                if (GUILayout.Button(guiContent, EditorStyles.toolbarDropDown))
                {
                    GenericMenu toolsMenu = new GenericMenu();

                    toolsMenu.AddItem(new GUIContent("Default"), false, delegate
                    {
                        var pswin = GetInstance();
                        NewProtoSprite(null, DefaultSettingsWindow.DefaultPPU, DefaultSettingsWindow.DefaultPivot, DefaultSettingsWindow.DefaultTextureSize);
                    });

                    // Custom new presets
                    var methods = TypeCache.GetMethodsWithAttribute<ProtoSpriteAddMenuItemAttribute>();
                    if (methods.Count > 0)
                    {
                        foreach (var method in methods)
                        {
                            if (method.GetParameters().Length != 0)
                                continue;

                            if (!method.IsStatic)
                                continue;

                            if (method.IsAbstract)
                                continue;

                            var attribute = method.GetCustomAttribute<ProtoSpriteAddMenuItemAttribute>();

                            toolsMenu.AddItem(new GUIContent(attribute.menuName), false, delegate
                            {
                                method.Invoke(null, null);
                            });
                        }
                    }

                    toolsMenu.AddSeparator("");

                    toolsMenu.AddItem(new GUIContent("Default Settings"), false, DefaultSettingsWindow.Open);

                    // Offset menu from right of editor window
                    toolsMenu.DropDown(new Rect(0, 5, 0, 16));
                    EditorGUIUtility.ExitGUI();
                }
            }

            // Duplicate sprite
            {
                GUI.enabled = isValid;

                var guiContent = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate"));
                guiContent.tooltip = "Create a duplicate sprite texture file and set the currently selected SpriteRenderer to reference it. Useful for creating a new frame when animating or for quickly creating a variant.";

                if (GUILayout.Button(guiContent, EditorStyles.toolbarButton))
                {
                    bool isMultisprite = ((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite.texture))).spriteImportMode == SpriteImportMode.Multiple;

                    if (isMultisprite)
                    {
                        int option = EditorUtility.DisplayDialogComplex("Sprite is multisprite",
                        "Extract or duplicate whole texture?",
                        "Extract sprite",
                        "Cancel",
                        "Whole Texture");

                        switch (option)
                        {
                            // Extract
                            case 0:
                                DuplicateExtractMultisprite();
                                break;

                            // Cancel.
                            case 1:
                                break;

                            // Whole texture
                            case 2:
                                DuplicateSprite();
                                break;

                            default:
                                Debug.LogError("Unrecognized option.");
                                break;
                        }
                    }
                    else
                    {
                        DuplicateSprite();
                    }

                    //DuplicateSprite();
                }

                GUI.enabled = true;
            }



            // Error
            if (!isValid)
            {
                
                
                // If user is selecting assets then show reason why assets aren't compatible
                bool isValidAssets = AreSelectedTextureAssetsValidProtoSprites(out string invalidAssetReason, out bool isAssetAutoFixable);

                if (!isValidAssets)
                {
                    //EditorGUILayout.BeginVertical();

                    string fullInvalidReason = invalidReason;

                    if (Selection.activeGameObject == null)
                    {
                        fullInvalidReason = invalidAssetReason;
                    }



                    //EditorGUILayout.EndVertical();

                    var guiContent = new GUIContent(EditorGUIUtility.IconContent("error"));
                    guiContent.tooltip = fullInvalidReason;

                    if (!isValid && (isAutoFixable || isAssetAutoFixable))
                    {
                        guiContent.text = " Fix Now";
                        if (GUILayout.Button(guiContent, EditorStyles.toolbarButton) && (isAutoFixable || isAssetAutoFixable))
                        {
                            AutoFix();
                        }
                        //AutoFix();
                    }
                    else
                    {
                        GUIStyle style = new GUIStyle();// GUIStyle(EditorStyles.toolbarButton);
                        style.normal = EditorStyles.toolbarButton.normal;
                        style.contentOffset = EditorStyles.toolbarButton.contentOffset;
                        style.imagePosition = EditorStyles.toolbarButton.imagePosition;
                        style.wordWrap = EditorStyles.toolbarButton.wordWrap;
                        style.margin = EditorStyles.toolbarButton.margin;
                        style.padding = EditorStyles.toolbarButton.padding;
                        style.border = EditorStyles.toolbarButton.border;
                        style.alignment = EditorStyles.toolbarButton.alignment;
                        //style.onHover.textColor = Color.red;
                        //style.hover.textColor = Color.red;
                        //style.hover.scaledBackgrounds = new Texture2D[0];
                        //style.hover.background = null;
                        //style.normal.textColor = Color.green;
                        //style.hover.background = null;
                        //style.hover = style.normal;
                        //style.onHover = style.onNormal;
                        //style.focused = style.normal;
                        //style.hover.background = style.normal.background;
                        //style.hover = new GUIStyleState();
                        GUILayout.Label(guiContent, style);
                        GUI.enabled = true;
                    }

                    //GUI.enabled = (isAutoFixable || isAssetAutoFixable);

                    

                    //GUI.enabled = true;
                }
            }

            // Transform operations WIP
            /*{
                GUI.enabled = isValid;

                var guiContent = new GUIContent("Transform");// new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate"));
                guiContent.tooltip = "Transform operations.";

                if (GUILayout.Button(guiContent, EditorStyles.toolbarButton))
                {
                    Debug.Log("Rotate");
                    RotateSprite();
                }

                GUI.enabled = true;
            }*/

            GUILayout.FlexibleSpace();

            // Debug Draw Toggle
            {
                //GUI.enabled = isValid;

                var guiContent = new GUIContent(EditorGUIUtility.IconContent("d_ViewToolOrbit" + (DebugRender? " On" : "")));
                guiContent.tooltip = "When toggled on, an overlay of the selected sprite is rendered while any ProtoSpriteTool is active.";

                //guiContent.
                bool newDebugRender = GUILayout.Toggle(DebugRender, guiContent, EditorStyles.toolbarButton);

                if (DebugRender != newDebugRender)
                {
                    DebugRender = newDebugRender;
                }
                //GUI.enabled = true;
            }


            // Save
            {
                GUI.enabled = ProtoSpriteData.HasSaveData();

                var guiContent = new GUIContent("Save");
                guiContent.tooltip = "Save all modified textures.";

                if (GUILayout.Button(guiContent, EditorStyles.toolbarButton))
                {
                    ProtoSpriteData.Saving.SaveAll();
                }

                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            EditorGUILayout.Space(20);

            if (ProtoSpriteData.HasSaveData())
            {
                titleContent = new GUIContent("ProtoSprite*");
            }
            else
            {
                titleContent = new GUIContent("ProtoSprite");
            }

            int selectedTool = Array.IndexOf(m_AllToolTypes.ToArray(), ToolManager.activeToolType);

            List<GUIContent> guiContents2 = new List<GUIContent>();
            for (int i = 0; i < AllToolInstances.Count; i++)
            {
                guiContents2.Add(AllToolInstances[i].toolbarIcon);
            }

            Rect toolbarRect = EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();

            int selected = GUILayout.Toolbar(selectedTool, guiContents2.ToArray(), GUILayout.Height(25));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck() && selected >= 0 && selected < m_AllToolTypes.Count)
            {
                Type selectedToolType = m_AllToolTypes[selected];

                if (selected == selectedTool)
                {
                    ToolManager.RestorePreviousPersistentTool();
                }
                else if (ToolManager.activeToolType != selectedToolType)
                {
                    var toolInstance = GetToolInstance(selectedToolType);
                    ToolManager.SetActiveTool(toolInstance);
                }
            }


            EditorGUILayout.Space(5);

            // Tool specific UI
            var activeToolType  = ToolManager.activeToolType;

            bool isToolCompatible = true;
            string invalidToolReason = "";

            if (activeToolType != null && activeToolType.IsSubclassOf(typeof(ProtoSpriteTool)))
            {
                var toolInstance = (ProtoSpriteTool)GetToolInstance(activeToolType);
                toolInstance.ProtoSpriteWindowGUI();
                isToolCompatible = toolInstance.IsToolCompatible(out invalidToolReason);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            /*if (!isValid)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleCenter;

                // If user is selecting assets then show reason why assets aren't compatible
                bool isValidAssets = AreSelectedTextureAssetsValidProtoSprites(out string invalidAssetReason, out bool isAssetAutoFixable);

                EditorGUILayout.BeginVertical();

                if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.LabelField(invalidAssetReason, style, GUILayout.Width(position.width - 20));
                }
                else
                {
                    EditorGUILayout.LabelField(invalidReason, style, GUILayout.Width(position.width - 20));
                }

                using (var h = new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (!isValid && (isAutoFixable || isAssetAutoFixable) && GUILayout.Button("Fix Now", GUILayout.Width(100)))
                    {
                        AutoFix();
                    }
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndVertical();
            }*/
            
            if (isValid && !isToolCompatible)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.red;
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleCenter;

                EditorGUILayout.LabelField(invalidToolReason, style, GUILayout.Width(position.width - 20));
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        void AutoFix()
        {
            var selectedObjects = Selection.objects;

            for (int i = 0; i < selectedObjects.Length; i++)
            {
                Texture2D texture = selectedObjects[i] as Texture2D;
                if (texture != null)
                {
                    Fix(texture);
                }

                GameObject gameObject = selectedObjects[i] as GameObject;
                if (gameObject != null)
                {
                    SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null)
                    {
                        Fix(spriteRenderer.sprite.texture);
                    }
                }
            }
        }

        void Fix(Texture2D texture)
        {
            bool shouldReimport = false;

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            var defaultPlatformSettings = importer.GetDefaultPlatformTextureSettings();

            var activeBuildTargetPlatformSettings = importer.GetPlatformTextureSettings(EditorUserBuildSettings.activeBuildTarget.ToString());

            if (activeBuildTargetPlatformSettings.overridden)
            {
                shouldReimport = true;
                activeBuildTargetPlatformSettings.format = TextureImporterFormat.RGBA32;
                importer.SetPlatformTextureSettings(activeBuildTargetPlatformSettings);
            }

            if (defaultPlatformSettings.format != TextureImporterFormat.RGBA32)
            {
                shouldReimport = true;
                defaultPlatformSettings.format = TextureImporterFormat.RGBA32;
            }

            if (!importer.isReadable)
            {
                shouldReimport = true;
                importer.isReadable = true;
            }

            importer.SetPlatformTextureSettings(defaultPlatformSettings);

            if (shouldReimport)
            {
                var tempSelection = Selection.objects;
                Selection.objects = null;
                importer.SaveAndReimport();
                Selection.objects = tempSelection;
            }
        }

        // We register shortcuts using window visibility callbacks so that users can quickly hide the window to suppress shortcuts
        void OnBecameVisible()
        {
            try { ShortcutManager.RegisterTag(kToolShortcutsTag); } catch { }
            try { InternalEngineBridge.RegisterShortcutContext(m_ShortcutContext); } catch { }
        }

        void OnBecameInvisible()
        {
            try { ShortcutManager.UnregisterTag(kToolShortcutsTag); } catch { }
            try { InternalEngineBridge.UnregisterShortcutContext(m_ShortcutContext); } catch { }
        }

        void OnDisable()
        {
            if (ToolManager.activeToolType != null && ToolManager.activeToolType.IsSubclassOf(typeof(ProtoSpriteTool)))
            {
                ToolManager.RestorePreviousPersistentTool();
            }

            for (int i = 0; i < m_AllToolInstances.Count; i++)
            {
                if (m_AllToolInstances[i] != null)
                {
                    DestroyImmediate(m_AllToolInstances[i]);
                }
            }
            m_AllToolInstances.Clear();

            m_ColorPalette.OnDisable();

            SceneView.duringSceneGui -= OnSceneGUI;
        }

        [MenuItem("GameObject/2D Object/ProtoSprite", priority = 10)]
        static void NewProtoSprite(MenuCommand command)
        {
            NewProtoSprite(command, DefaultSettingsWindow.DefaultPPU, DefaultSettingsWindow.DefaultPivot, DefaultSettingsWindow.DefaultTextureSize);
        }

        public static GameObject CreateProtoSprite(float ppu, Vector2 pivot, Vector2Int textureSize)
        {
            return NewProtoSprite(null, ppu, pivot, textureSize);
        }

        public static GameObject CreateProtoSprite(float ppu, Vector2 pivot, Vector2Int textureSize, Vector3 spawnPosition, Color color)
        {
            return NewProtoSprite(null, ppu, pivot, textureSize, spawnPosition, color);
        }

        static GameObject NewProtoSprite(MenuCommand command, float ppu, Vector2 pivot, Vector2Int textureSize)
        {
            Vector3 spawnPosition = SceneView.lastActiveSceneView.pivot;
            spawnPosition.z = 0.0f;

            return NewProtoSprite(command, ppu, pivot, textureSize, spawnPosition, Color.black);
        }

        static GameObject NewProtoSprite(MenuCommand command, float ppu, Vector2 pivot, Vector2Int textureSize, Vector3 spawnPosition, Color color)
        {
            var instance = GetInstance();
            if (string.IsNullOrEmpty(instance.m_LastSaveFolder) || !Directory.Exists(instance.m_LastSaveFolder))
            {
                instance.m_LastSaveFolder = "Assets";
            }

            string combinedPath = Path.Combine(instance.m_LastSaveFolder, "New ProtoSprite.png");

            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(combinedPath);

            string defaultName = Path.GetFileNameWithoutExtension(uniquePath);

            string newPath = EditorUtility.SaveFilePanelInProject("Create ProtoSprite Texture", defaultName, "png", "Enter file name", instance.m_LastSaveFolder);

            if (newPath.Length == 0)
                return null;

            instance.m_LastSaveFolder = Path.GetDirectoryName(newPath);

            GameObject parentGameObject = null;

            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (command != null)
            {
                GameObject contextGO = command.context as GameObject;
                if (contextGO != null)
                {
                    parentGameObject = contextGO;
                }
            }
            else if (currentPrefabStage != null)
            {
                parentGameObject = currentPrefabStage.prefabContentsRoot;
            }

            // Create sprite renderer
            GameObject newGO = new GameObject(Path.GetFileNameWithoutExtension(newPath));

            newGO.transform.position = spawnPosition;

            SpriteRenderer spriteRenderer = newGO.AddComponent<SpriteRenderer>();

            if (parentGameObject != null)
            {
                GameObjectUtility.SetParentAndAlign(newGO, parentGameObject);
            }

            // Create new texture
            Texture2D texture = new Texture2D(textureSize.x, textureSize.y);

            // Set pixels to black
            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < (texture.width * texture.height); i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            // Write to file
            var bytes = texture.EncodeToPNG();

            DestroyImmediate(texture);

            File.WriteAllBytes(newPath, bytes);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(newPath);
            AssetDatabase.Refresh();

            string path = newPath;
            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath, StringComparison.InvariantCultureIgnoreCase))
            {
                path = "Assets" + path.Substring(dataPath.Length);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(defaultSettings);

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importerSettings.spritePixelsPerUnit = ppu;
            importerSettings.spritePivot = pivot;
            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(importerSettings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // We want to ensure the new file has a new GUID
            // If there was a file called ProtoSprite.png and it was deleted and another one with the same name is created it will have the same GUID
            // if this is done while Unity is open. We want to prevent this as it's confusing
            GenerateNewGUID(AssetDatabase.GUIDFromAssetPath(newPath));

            // Link sprite renderer to texture
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            spriteRenderer.sprite = sprite;

            Selection.activeGameObject = newGO;
            Undo.RegisterCreatedObjectUndo(newGO, "Created ProtoSprite");

            EditorGUIUtility.PingObject(sprite.texture);

            return newGO;
        }

        static void GenerateNewGUID(GUID guid)
        {
            try
            {
                if (EditorSettings.serializationMode == SerializationMode.ForceText)
                {
                    GUID currentGUID = guid;
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string newGUID = GUID.Generate().ToString();
                    string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
                    string metaFileContents = File.ReadAllText(metaFilePath);
                    metaFileContents = metaFileContents.Replace(currentGUID.ToString(), newGUID);
                    File.WriteAllText(metaFilePath, metaFileContents);
                    AssetDatabase.ImportAsset(assetPath);
                    //Debug.Log("Previous guid: " + currentGUID.ToString() + " new: " + newGUID.ToString());
                }
            }
            catch { }
        }

        static void DuplicateSprite()
        {
            GameObject gameObject = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            Sprite selectedSprite = spriteRenderer.sprite;
            Texture2D selectedTexture = SpriteUtility.GetSpriteTexture(selectedSprite, false);
            string selectedAssetPath = AssetDatabase.GetAssetPath(selectedTexture);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(selectedAssetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string folderPath = Path.GetDirectoryName(assetPath);

            string newPath = EditorUtility.SaveFilePanelInProject("Create ProtoSprite Texture", assetName, "png", "Enter file name", folderPath);

            if (newPath.Length != 0)
            {
                ProtoSpriteData.Saving.SaveTextureIfDirty(selectedTexture);

                // Create new texture
                AssetDatabase.CopyAsset(selectedAssetPath, newPath);
                AssetDatabase.ImportAsset(newPath);

                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(newPath).OfType<Sprite>().ToArray();

                Sprite sprite = sprites[0];

                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i].name == selectedSprite.name)
                    {
                        sprite = sprites[i];
                        break;
                    }
                }

                Undo.RecordObject(spriteRenderer, "ProtoSprite Duplicate Sprite");

                spriteRenderer.sprite = sprite;
                EditorUtility.SetDirty(spriteRenderer);

                EditorGUIUtility.PingObject(sprite.texture);
            }
        }

        static void DuplicateExtractMultisprite()
        {
            GameObject gameObject = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            Sprite selectedSprite = spriteRenderer.sprite;
            Texture2D selectedTexture = SpriteUtility.GetSpriteTexture(selectedSprite, false);
            string selectedAssetPath = AssetDatabase.GetAssetPath(selectedTexture);


            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(Path.GetDirectoryName(selectedAssetPath), selectedSprite.name + ".png"));

            //string assetPath = AssetDatabase.GenerateUniqueAssetPath(selectedAssetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string folderPath = Path.GetDirectoryName(assetPath);

            // Use sprite name instead of texture name

            //Debug.Log("asset path: " + assetPath);

            string newPath = EditorUtility.SaveFilePanelInProject("Create ProtoSprite Texture", assetName, "png", "Enter file name", folderPath);

            if (newPath.Length == 0)
                return;

            ProtoSpriteData.Saving.SaveTextureIfDirty(selectedTexture);

            // Create new texture
            Texture2D texture = new Texture2D((int)selectedSprite.rect.size.x, (int)selectedSprite.rect.size.y);

            var dst = texture.GetPixelData<Color32>(0);
            var src = selectedTexture.GetPixelData<Color32>(0);

            // Copy pixels from sprite texture within sprite rect bounds to new texture
            var job = new BlitJob()
            {
                srcPixelData = src,
                dstPixelData = dst,
                srcTextureSize = new int2(selectedTexture.width, selectedTexture.height),
                dstTextureSize = new int2(texture.width, texture.height),
                srcPos = new int2((int)selectedSprite.rect.position.x, (int)selectedSprite.rect.position.y),
                spriteRect = selectedSprite.rect,
            };

            job.Schedule(texture.width * texture.height, 32).Complete();

            texture.Apply(false, false);

            // Write to file
            var bytes = texture.EncodeToPNG();

            DestroyImmediate(texture);

            File.WriteAllBytes(newPath, bytes);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(newPath);
            AssetDatabase.Refresh();

            string path = newPath;
            string dataPath = Application.dataPath;
            if (path.StartsWith(dataPath, StringComparison.InvariantCultureIgnoreCase))
            {
                path = "Assets" + path.Substring(dataPath.Length);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            defaultSettings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(defaultSettings);

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importerSettings.spritePixelsPerUnit = selectedSprite.pixelsPerUnit;
            importerSettings.spritePivot = selectedSprite.pivot / selectedSprite.rect.size;
            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(importerSettings);

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // We want to ensure the new file has a new GUID
            // If there was a file called ProtoSprite.png and it was deleted and another one with the same name is created it will have the same GUID
            // if this is done while Unity is open. We want to prevent this as it's confusing
            GenerateNewGUID(AssetDatabase.GUIDFromAssetPath(newPath));

            Undo.RecordObject(spriteRenderer, "ProtoSprite Duplicate Sprite");

            // Link sprite renderer to texture
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            spriteRenderer.sprite = sprite;
            EditorUtility.SetDirty(spriteRenderer);

            EditorGUIUtility.PingObject(sprite.texture);
        }

        [BurstCompile]
        struct BlitJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Color32> srcPixelData;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> dstPixelData;

            [ReadOnly] public int2 srcTextureSize;
            [ReadOnly] public int2 dstTextureSize;

            [ReadOnly] public int2 srcPos;
            [ReadOnly] public Rect spriteRect;

            public void Execute(int i)
            {
                int2 dstPixelCoord = new int2(i % dstTextureSize.x, i / dstTextureSize.x);

                int2 srcPixelCoord = srcPos + dstPixelCoord;

                if (!spriteRect.Contains(new Vector2(srcPixelCoord.x + 0.5f, srcPixelCoord.y + 0.5f)))
                {
                    dstPixelData[i] = Color.clear;
                    return;
                }

                if (srcPixelCoord.x < 0 || srcPixelCoord.y < 0 || srcPixelCoord.x >= srcTextureSize.x || srcPixelCoord.y >= srcTextureSize.y)
                {
                    dstPixelData[i] = Color.clear;
                }
                else
                {
                    int srcIndex = srcPixelCoord.x + srcPixelCoord.y * srcTextureSize.x;

                    dstPixelData[i] = srcPixelData[srcIndex];
                }
            }
        }

        static void RotateSprite()
        {
            GameObject gameObject = Selection.activeGameObject;
            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            Sprite selectedSprite = spriteRenderer.sprite;
            Texture2D selectedTexture = SpriteUtility.GetSpriteTexture(selectedSprite, false);

            // texture width and height swap

            // Copy the color32 pixel data
            var srcPixelData = selectedTexture.GetPixelData<Color32>(0);
            var dstPixelData = new NativeArray<Color32>(srcPixelData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int height = selectedTexture.height;
            int width = selectedTexture.width;

            // Pivot
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(selectedTexture));

            int count = 3;

            for (int i = 0; i < 3; i++)
            {
                RotatePixelDataJob job;
                job.dst = dstPixelData;
                job.src = srcPixelData;
                job.height = height;
                job.width = width;
                job.Schedule(width * height, 32).Complete();
                importer.spritePivot = new Vector2(1.0f - importer.spritePivot.y, importer.spritePivot.x);

                if (i < count - 1)
                {
                    srcPixelData.CopyFrom(dstPixelData);
                    height = job.width;
                    width = job.height;
                }
            }

            /*for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate the new coordinates for clockwise rotation
                    int newX = height - y - 1;
                    int newY = x;

                    // Map the original pixel to the rotated position
                    int rotatedIndex = newX + newY * height;
                    int originalIndex = x + y * width;

                    // Store the pixel in the rotated array
                    dstPixelData[rotatedIndex] = srcPixelData[originalIndex];
                }
            }*/

            // Submit undo data
            // Submit save data

            // Rearrange all the nativearray color32 data

            // Reinitialize the texture width and height swap
            selectedTexture.Reinitialize(selectedTexture.height, selectedTexture.width);

            selectedTexture.SetPixelData<Color32>(dstPixelData, 0);
            selectedTexture.Apply(true, false);

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(selectedTexture);
            ProtoSpriteData.SubmitSaveData(saveData);
            ProtoSpriteData.Saving.SaveTextureIfDirty(selectedTexture, false);



            dstPixelData.Dispose();


            importer.SaveAndReimport();
        }

        [BurstCompile]
        struct RotatePixelDataJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> src;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> dst;

            public int height;
            public int width;

            public void Execute(int i)
            {
                int x = i % (int)width;
                int y = i / (int)width;

                int newX = height - y - 1;
                int newY = x;

                // Map the original pixel to the rotated position
                int rotatedIndex = newX + newY * height;
                int originalIndex = x + y * width;

                // Store the pixel in the rotated array
                dst[rotatedIndex] = src[originalIndex];
            }
        }

        public static bool AreSelectedTextureAssetsValidProtoSprites(out string invalidReason, out bool isAutoFixable)
        {
            isAutoFixable = false;

            invalidReason = "No selection.";
            if (Selection.objects == null || Selection.objects.Length == 0)
                return false;

            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var texture = Selection.objects[i] as Texture2D;

                invalidReason = "Selection contains objects that aren't textures.";
                if (texture == null)
                {
                    isAutoFixable = false;
                    return false;
                }

                invalidReason = "Couldn't find asset file for the texture.";
                string textureAssetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(textureAssetPath))
                {
                    isAutoFixable = false;
                    return false;
                }

                invalidReason = "Texture asset file can't be from a read-only package.";
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(textureAssetPath);
                if (packageInfo != null && (packageInfo.source != UnityEditor.PackageManager.PackageSource.Embedded || packageInfo.source != UnityEditor.PackageManager.PackageSource.Local))
                {
                    isAutoFixable = false;
                    return false;
                }

                invalidReason = "Texture asset file must be a PNG.";
                string fileExtension = Path.GetExtension(textureAssetPath).ToLower();
                if (string.Compare(fileExtension, ".png") != 0)
                {
                    isAutoFixable = false;
                    return false;
                }

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

                importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
                invalidReason = "Texture imported size (" + texture.width + "x" + texture.height + ") must match the source size (" + sourceWidth + "x" + sourceHeight + ")";
                if (texture.width != sourceWidth || texture.height != sourceHeight)
                {
                    isAutoFixable = false;
                    return false;
                }

                invalidReason = "Texture too large.";
                if (texture.width > kMaxTextureSize || texture.height > kMaxTextureSize)
                {
                    isAutoFixable = false;
                    return false;
                }

                if (importer.spriteImportMode == SpriteImportMode.Polygon)
                {
                    isAutoFixable = false;
                    invalidReason = "Texture's Importer has Sprite Mode set to Polygon.";
                    return false;
                }

                invalidReason = "Texture is not set as Read/Write enabled in its Importer.";
                if (!texture.isReadable)
                {
                    isAutoFixable = true;
                    return false;
                }

                invalidReason = "Texture's Importer must have Format set to RGBA32.";
                if (texture.format != TextureFormat.RGBA32)
                {
                    isAutoFixable = true;
                    return false;
                }
            }

            invalidReason = "";
            isAutoFixable = false;
            return true;
        }

        public static bool IsSelectionValidProtoSprite(out string invalidReason)
        {
            return IsSelectionValidProtoSprite(out invalidReason, out bool isAutoFixable);
        }

        public static bool IsSelectionValidProtoSprite(out string invalidReason, out bool isAutoFixable)
        {
            isAutoFixable = false;

            if (Selection.activeTransform == null)
            {
                invalidReason = "No GameObject Selected.";
                return false;
            }

            if (Selection.transforms.Length > 1)
            {
                invalidReason = "Can't edit more than one GameObject at a time.";
                return false;
            }

            Transform transform = Selection.activeTransform;

            SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                invalidReason = "GameObject doesn't have a SpriteRenderer.";
                return false;
            }

            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null)
            {
                invalidReason = "SpriteRenderer doesn't have a sprite referenced.";
                return false;
            }

            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);
            if (texture == null)
            {
                invalidReason = "SpriteRenderer.sprite doesn't have a texture reference.";
                return false;
            }

            string textureAssetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(textureAssetPath))
            {
                invalidReason = "Couldn't find asset file for the texture.";
                return false;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(textureAssetPath);
            if (packageInfo != null && (packageInfo.source != UnityEditor.PackageManager.PackageSource.Embedded || packageInfo.source != UnityEditor.PackageManager.PackageSource.Local))
            {
                invalidReason = "Texture asset file can't be from a read-only package.";
                return false;
            }

            string fileExtension = Path.GetExtension(textureAssetPath).ToLower();
            if (string.Compare(fileExtension, ".png") != 0)
            {
                invalidReason = "Texture asset file must be a PNG.";
                return false;
            }

            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                invalidReason = "SpriteRenderer Draw Mode must be Simple.";
                return false;
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (texture.width != sourceWidth || texture.height != sourceHeight)
            {
                invalidReason = "Texture imported size (" + texture.width + "x" + texture.height + ") must match the source size (" + sourceWidth + "x" + sourceHeight + ")";
                return false;
            }

            if (texture.width > kMaxTextureSize || texture.height > kMaxTextureSize)
            {
                invalidReason = "Texture too large.";
                return false;
            }

            if (sprite.packed)
            {
                invalidReason = "Sprite must not be packed in a sprite atlas.";
                return false;
            }

            if (importer.spriteImportMode == SpriteImportMode.Polygon)
            {
                invalidReason = "Texture's Importer has Sprite Mode set to Polygon.";
                return false;
            }

            if (!texture.isReadable)
            {
                invalidReason = "Texture is not set as Read/Write enabled in its Importer.";
                isAutoFixable = true;
                return false;
            }

            if (texture.format != TextureFormat.RGBA32)
            {
                invalidReason = "Texture's Importer must have Format set to RGBA32.";
                isAutoFixable = true;
                return false;
            }

            invalidReason = string.Empty;
            return true;
        }

		public void AddItemsToMenu(GenericMenu menu)
		{
            if (ProtoSpriteData.HasSaveData())
            {
                menu.AddItem(new GUIContent("Save"), false, ProtoSpriteData.Saving.SaveAll);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Save"), false);
            }
        }
    }
}