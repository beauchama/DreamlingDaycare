using UnityEditor;
using UnityEngine;
using UnityEditor.Sprites;
using Unity.Mathematics;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

namespace ProtoSprite.Editor
{
    public class SelectTool : ProtoSpriteTool
    {
        Texture2D m_TargetTexture = null;

        Sprite m_TargetSprite = null;

        bool m_SceneSelectionOutlineGizmoEnabled = false;

        bool m_IsPreviewDrawn = false;
        [SerializeField] bool m_IsDirty = false;

        UndoDataPaint m_UndoData = null;

        [SerializeField] Rect m_SelectionRect = new Rect(0, 0, 0, 0);

        Material m_DrawHoverImageMaterial = null;

        Texture2D m_SomeTex = null;

        int2 m_HoverImageDragRectStart = int2.zero;
        int2 m_HoverImageDragRectEnd = int2.zero;

        Vector2 m_MouseDownPos = Vector2.zero;

        int m_UndoGroupAtTimeOfDirty = -1;

        float m_SelectThreshold = 2.0f;

        bool m_IsSelecting = false;

        // Local clipboard
        byte[] m_ClipboardData = null;

        Material DrawHoverImageMaterial
        {
            get
            {
                if (m_DrawHoverImageMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/ProtoSprite/DrawHoverImage");
                    m_DrawHoverImageMaterial = new Material(shader);
                }
                return m_DrawHoverImageMaterial;
            }
        }

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("Grid.Default"));
                content.tooltip = "Select (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Select Tool") + ")";
                return content;
            }
        }

        void UndoRedoEvent(in UndoRedoInfo undoRedoInfo)
        {
            if (!undoRedoInfo.undoName.StartsWith("ProtoSprite Select Tool"))
                return;

            m_IsDirty = false;

            if (m_SomeTex != null)
            {
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
            }
        }

        public void OnEnable()
        {
            ProtoSpriteData.Saving.onWillSave += OnWillSaveData;
            Undo.undoRedoEvent += UndoRedoEvent;
        }

        void OnWillSaveData(ProtoSpriteData.SaveData saveData)
        {
            if (m_TargetSprite == null)
                return;

            if (saveData.textureGUID.ToString() == AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_TargetSprite.texture)))
            {
                DrawHoverImageFinal(m_TargetSprite);

                saveData.pixelData = m_TargetSprite.texture.GetPixelData<Color32>(0).ToArray();

                m_IsDirty = false;
            }
        }


		public void OnDisable()
		{
            ProtoSpriteData.Saving.onWillSave -= OnWillSaveData;
            Undo.undoRedoEvent -= UndoRedoEvent;

            // If the tool is active and we exit play mode then OnDisable is called but OnWillBeDeactivated isn't so we force call it here
            if (ToolManager.activeToolType == GetType())
                OnWillBeDeactivated();

            if (m_SomeTex != null)
            {
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
            }
        }

        [Shortcut("ProtoSprite/Select Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.M)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<SelectTool>();
        }

		public override void ProtoSpriteWindowGUI()
        {
            GUI.enabled = m_SelectionRect.size != Vector2.zero;

            /*float menuBarHeight = 20;
            Rect menuBar = new Rect(0, 0, position.width, menuBarHeight);

            GUILayout.BeginArea(menuBar, EditorStyles.toolbar);*/

            GUILayout.BeginHorizontal();
            {
                GUIContent guiContent = new GUIContent(EditorGUIUtility.IconContent("RotateTool On"));
                guiContent.tooltip = "Rotate 90° CCW";
                if (GUILayout.Button(guiContent))
                {
                    RotateCCW();
                }
            }

            {
                Texture2D icon = EditorGUIUtility.Load(ProtoSpriteData.ProtoSpriteEditorFolderPath + "/Icons/FlipV.png") as Texture2D;
                GUIContent guiContent = new GUIContent(icon);
                guiContent.tooltip = "Flip Vertically";
                if (GUILayout.Button(guiContent))
                {
                    FlipV();
                }
            }

            {
                Texture2D icon = EditorGUIUtility.Load(ProtoSpriteData.ProtoSpriteEditorFolderPath + "/Icons/FlipH.png") as Texture2D;
                GUIContent guiContent = new GUIContent(icon);
                guiContent.tooltip = "Flip Horizontally";
                if (GUILayout.Button(guiContent))
                {
                    FlipH();
                }
            }

            /*{
                Texture2D icon = EditorGUIUtility.Load(ProtoSpriteData.ProtoSpriteEditorFolderPath + "/Icons/FlipH.png") as Texture2D;
                GUIContent guiContent = new GUIContent(icon);
                guiContent.tooltip = "Test";
                if (GUILayout.Button(guiContent))
                {
                    test();
                }
            }*/

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //GUILayout.EndArea();

            /*if (GUILayout.Button("Copy"))
            {
                Copy();
            }

            GUI.enabled = m_ClipboardData != null && m_ClipboardData.Length > 0;

            if (GUILayout.Button("Paste"))
            {
                Paste();
            }*/

            GUI.enabled = true;
        }

        void test()
        {
            Debug.Log("test");
        }

        public override void OnActivated()
        {
            m_SceneSelectionOutlineGizmoEnabled = ProtoSpriteData.SceneSelectionGizmo;
            ProtoSpriteData.SceneSelectionGizmo = false;
        }

        public override void OnWillBeDeactivated()
        {
            ProtoSpriteData.SceneSelectionGizmo = m_SceneSelectionOutlineGizmoEnabled;

            Finish();

            m_TargetTexture = null;

            m_IsPreviewDrawn = false;
            m_IsDirty = false;
        }


        void Finish()
        {
            m_IsSelecting = false;

            DrawHoverImageFinal(m_TargetSprite);

            m_HoverImageDragRectStart = int2.zero;
            m_HoverImageDragRectEnd = int2.zero;

            if (m_TargetTexture == null)
                return;

            if (m_IsPreviewDrawn)
            {
                m_TargetTexture.Apply(true, false);
                m_IsPreviewDrawn = false;
            }

            if (m_IsDirty)
            {
                m_IsDirty = false;

                m_UndoData.pixelDataAfter = m_TargetTexture.GetPixels32(0);
                m_UndoData = null;

                ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                ProtoSpriteData.SubmitSaveData(saveData);

                m_TargetTexture = null;
            }
        }

        void UpdateTarget()
        {
            bool validSelection = ProtoSpriteWindow.IsSelectionValidProtoSprite(out string reason);
            bool changedTarget = false;

            Sprite selectedSprite = null;

            if (m_TargetSprite != null && !validSelection)
            {
                changedTarget = true;
            }

            if (m_TargetSprite != null && validSelection)
            {
                Transform t = Selection.activeTransform;
                SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
                selectedSprite = spriteRenderer.sprite;

                changedTarget = m_TargetSprite != selectedSprite;
            }

            if (changedTarget)
            {
                Finish();

                ClearSelection();
            }

            m_TargetSprite = selectedSprite;
        }

        void DrawHandles(Transform t)
        {
            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;

            // Draw rect outline
            {
                Rect spriteRect = sprite.rect;
                Vector2 scale = (new Vector2(spriteRect.width, spriteRect.height) / sprite.pixelsPerUnit);

                Matrix4x4 tempMatrix = Handles.matrix;
                Handles.matrix = t.localToWorldMatrix;

                Vector2 spritePivot = sprite.pivot;
                if (spriteRenderer.flipX)
                    spritePivot.x = spriteRect.width - sprite.pivot.x;
                if (spriteRenderer.flipY)
                    spritePivot.y = spriteRect.height - sprite.pivot.y;
                Handles.DrawWireCube(scale * 0.5f - spritePivot / sprite.pixelsPerUnit, scale);
                Handles.matrix = tempMatrix;
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            Event e = Event.current;

            UpdateTarget();

            if (!(window is SceneView))
                return;

            ProtoSpriteData.RepaintSceneViewsIfUnityFocused();
            ProtoSpriteData.RepaintSpriteEditorWindow();

            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string invalidReason))
            {
                ProtoSpriteData.DrawInvalidHandles();
                return;
            }

            if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "Copy")
                {
                    //Debug.Log("Copy");
                    Copy();
                    e.Use();
                }
                else if (e.commandName == "Cut")
                {
                    //Debug.Log("Copy");
                    Cut();
                    e.Use();
                }
                else if (e.commandName == "Paste")
                {
                    //Debug.Log("Paste");
                    Paste();
                    e.Use();
                }
            }

            Transform t = Selection.activeTransform;

            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);
            m_TargetTexture = texture;
            int2 pixelCoord = ProtoSpriteData.GetPixelCoord();

            m_TargetSprite = sprite;

            bool isColorPickerOpened = ProtoSpriteData.IsColorPickerOpen();

            DrawHandles(t);


            if (!m_IsSelecting)
                DrawHandles(spriteRenderer);

            int passiveControl = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveControl);

            if (Event.current.GetTypeForControl(passiveControl) == EventType.MouseDown && e.button == 0)
            {
                GUIUtility.hotControl = passiveControl;
            }

            // Painting
            if (e.rawType == EventType.MouseDown && e.button == 0 && !isColorPickerOpened)
            {
                m_IsDirty = false;

                m_IsSelecting = true;

                m_MouseDownPos = e.mousePosition;

                m_HoverImageDragRectStart = pixelCoord;
                m_HoverImageDragRectEnd = pixelCoord;
            }

            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                m_IsSelecting = false;

                m_HoverImageDragRectEnd = pixelCoord;

                if (Vector2.Distance(e.mousePosition, m_MouseDownPos) < m_SelectThreshold)
                {
                    ClearSelection();
                }
                else
                {
                    Rect rect = new Rect();
                    rect.min = math.min((float2)m_HoverImageDragRectStart, (float2)m_HoverImageDragRectEnd);
                    rect.max = math.max((float2)m_HoverImageDragRectStart, (float2)m_HoverImageDragRectEnd);
                    rect.max += Vector2.one;

                    if (sprite.rect.Overlaps(rect))
                    {
                        rect.xMin = Mathf.Clamp(rect.xMin, sprite.rect.xMin, sprite.rect.xMax);
                        rect.xMax = Mathf.Clamp(rect.xMax, sprite.rect.xMin, sprite.rect.xMax);
                        rect.yMin = Mathf.Clamp(rect.yMin, sprite.rect.yMin, sprite.rect.yMax);
                        rect.yMax = Mathf.Clamp(rect.yMax, sprite.rect.yMin, sprite.rect.yMax);

                        m_SelectionRect = rect;
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
            }

            if ((e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseDown) && e.button == 0 && m_IsSelecting)
            {
                m_HoverImageDragRectEnd = pixelCoord;

                DrawHoverImageFinal(sprite);
            }

            // Draw out the dragged rect
            if (m_SomeTex == null && m_IsSelecting)
            {
                DrawDraggedHandles(spriteRenderer);
            }

            // Preview
            if (e.type != EventType.Repaint)
                return;

            if (window != SceneView.lastActiveSceneView)
                return;

            DrawHoverImagePreview(sprite);

            m_IsPreviewDrawn = true;
        }

        void DrawHoverImageFinal(Sprite sprite)
        {
            if (m_SomeTex == null || sprite == null)
                return;

            var job = new DrawJob()
            {
                hoverPixelData = m_SomeTex.GetPixelData<Color32>(0),
                targetPixelData = sprite.texture.GetPixelData<Color32>(0),
                hoverTextureSize = new int2(m_SomeTex.width, m_SomeTex.height),
                targetTextureSize = new int2(sprite.texture.width, sprite.texture.height),
                hoverTexturePosition = new int2((int)m_SelectionRect.position.x, (int)m_SelectionRect.position.y),
                spriteRect = sprite.rect
            };

            job.Schedule(sprite.texture.width * sprite.texture.height, 32).Complete();

            sprite.texture.Apply(true, false);

            DestroyImmediate(m_SomeTex);
            m_SomeTex = null;
        }

        void DrawHoverImagePreview(Sprite sprite)
        {
            if (m_SomeTex == null)
                return;

            Texture2D texture = sprite.texture;
            Rect spriteRect = sprite.rect;

            texture.Apply(false, false);

            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(texture.width, texture.height);
            rtDesc.colorFormat = RenderTextureFormat.ARGB32;
            rtDesc.useMipMap = texture.mipmapCount > 1;
            rtDesc.mipCount = texture.mipmapCount;
            rtDesc.autoGenerateMips = false;
            rtDesc.sRGB = texture.isDataSRGB;
            var rt = RenderTexture.GetTemporary(rtDesc);

            DrawHoverImageMaterial.SetVector("_ProtoSprite_SpriteRect", new Vector4(spriteRect.xMin, spriteRect.yMin, spriteRect.xMax, spriteRect.yMax));
            DrawHoverImageMaterial.SetVector("_ProtoSprite_HoverImagePosition", m_SelectionRect.position);
            DrawHoverImageMaterial.SetTexture("_ProtoSprite_HoverImageTex", m_SomeTex);

            Graphics.Blit(texture, rt, DrawHoverImageMaterial);

            Graphics.CopyTexture(rt, texture);

            RenderTexture.ReleaseTemporary(rt);
        }

        [BurstCompile]
        struct DrawJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Color32> hoverPixelData;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> targetPixelData;

            [ReadOnly] public int2 hoverTextureSize;
            [ReadOnly] public int2 targetTextureSize;


            [ReadOnly] public int2 hoverTexturePosition;


            [ReadOnly] public Rect spriteRect;

            public void Execute(int i)
            {
                int2 pixelCoord = new int2(i % targetTextureSize.x, i / targetTextureSize.x);

                if (pixelCoord.x < spriteRect.xMin || pixelCoord.y < spriteRect.yMin || pixelCoord.x >= spriteRect.xMax || pixelCoord.y >= spriteRect.yMax)
                    return;

                int2 hoverPixel = pixelCoord - hoverTexturePosition;

                if (hoverPixel.x < 0 || hoverPixel.y < 0 || hoverPixel.x >= hoverTextureSize.x || hoverPixel.y >= hoverTextureSize.y)
                    return;

                Color32 hoverValue = hoverPixelData[hoverPixel.x + hoverPixel.y * hoverTextureSize.x];

                if (hoverValue.a == 0)
                    return;

                targetPixelData[i] = hoverValue;
            }
        }

        [BurstCompile]
        struct CopyJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<Color32> srcPixelData;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> dstPixelData;

            [ReadOnly] public int2 srcTextureSize;
            [ReadOnly] public int2 dstTextureSize;

            [ReadOnly] public int2 srcPos;
            [ReadOnly] public Rect spriteRect;
            [ReadOnly] public bool clearSrc;

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

                    if (clearSrc)
                    {
                        srcPixelData[srcIndex] = Color.clear;
                    }
                }
            }
        }

        void CopyIntoHoverTexture(Sprite sprite, Rect rect, bool clearSrc)
        {
            if (rect.width == 0 || rect.height == 0)
                return;

            if (m_SomeTex != null)
            {
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
            }

            m_SomeTex = Instantiate(sprite.texture);
            m_SomeTex.Reinitialize((int)rect.width, (int)rect.height);

            m_SelectionRect.position = rect.position;

            var job = new CopyJob()
            {
                srcPixelData = sprite.texture.GetPixelData<Color32>(0),
                dstPixelData = m_SomeTex.GetPixelData<Color32>(0),
                srcTextureSize = new int2(sprite.texture.width, sprite.texture.height),
                dstTextureSize = new int2(m_SomeTex.width, m_SomeTex.height),
                srcPos = new int2((int)m_SelectionRect.position.x, (int)m_SelectionRect.position.y),
                spriteRect = sprite.rect,
                clearSrc = clearSrc
            };

            job.Schedule(m_SomeTex.width * m_SomeTex.height, 32).Complete();

            m_SomeTex.Apply(false, false);
        }

        void DrawDraggedHandles(SpriteRenderer spriteRenderer)
        {
            if (Vector2.Distance(Event.current.mousePosition, m_MouseDownPos) < m_SelectThreshold)
            {
                ClearSelection();
                return;
            }

            Rect rect = new Rect();
            rect.min = math.min((float2)m_HoverImageDragRectStart, (float2)m_HoverImageDragRectEnd);
            rect.max = math.max((float2)m_HoverImageDragRectStart, (float2)m_HoverImageDragRectEnd);
            rect.max += Vector2.one;

            // Draw rect handle where hover image should be
            //
            Sprite sprite = spriteRenderer.sprite;
            Transform t = spriteRenderer.transform;
            Rect spriteRect = sprite.rect;

            // Clamp selection to sprite rect
            {
                rect.xMin = Mathf.Clamp(rect.xMin, spriteRect.xMin, spriteRect.xMax);
                rect.xMax = Mathf.Clamp(rect.xMax, spriteRect.xMin, spriteRect.xMax);
                rect.yMin = Mathf.Clamp(rect.yMin, spriteRect.yMin, spriteRect.yMax);
                rect.yMax = Mathf.Clamp(rect.yMax, spriteRect.yMin, spriteRect.yMax);
            }

            int controlID = GUIUtility.GetControlID("ProtoSprite.SelectTool.MakeSelection".GetHashCode(), FocusType.Passive);

            Rect hoverImageRect = rect;

            Vector3 scale = (new Vector3(hoverImageRect.width, hoverImageRect.height, 1.0f) / sprite.pixelsPerUnit);

            Matrix4x4 tempMatrix = Handles.matrix;

            Vector2 pivotNormalized = sprite.pivot / spriteRect.size;
            
            Vector3 centerLocal = (hoverImageRect.center - spriteRect.position - pivotNormalized * spriteRect.size) / sprite.pixelsPerUnit;

            if (spriteRenderer.flipX)
                centerLocal.x *= -1.0f;
            if (spriteRenderer.flipY)
                centerLocal.y *= -1.0f;

            Handles.matrix = t.localToWorldMatrix * Matrix4x4.TRS(centerLocal, Quaternion.identity, scale);

            Handles.color = Color.white;
            Handles.RectangleHandleCap(controlID, Vector3.zero, Quaternion.identity, 0.5f, Event.current.type);

            Handles.color = Color.black;

            Handles.DrawDottedLines(new Vector3[] {
                new Vector2(-0.5f,-0.5f), new Vector2(0.5f,-0.5f),
                new Vector2(0.5f,-0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0.5f,0.5f), new Vector2(-0.5f,0.5f),
                new Vector2(-0.5f,-0.5f), new Vector2(-0.5f,0.5f) },
                5.0f);

            Handles.matrix = tempMatrix;
        }

        void DrawHandles(SpriteRenderer spriteRenderer)
        {
            if (m_SelectionRect.size.magnitude < 1.0f)
            {
                return;
            }

            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = sprite.texture;
            Transform t = spriteRenderer.transform;

            bool mouseUpAfterDrag = Event.current.type == EventType.MouseUp && Event.current.button == 0;

            int controlID = GUIUtility.GetControlID("ProtoSprite.SelectTool.MoveSelection".GetHashCode(), FocusType.Passive);


            Rect spriteRect = sprite.rect;

            Rect hoverImageRect = m_SelectionRect;

            Vector2 spritePivot = sprite.pivot;


            Vector3 scale = (new Vector3(hoverImageRect.width, hoverImageRect.height, 1.0f) / sprite.pixelsPerUnit);

            Matrix4x4 tempMatrix = Handles.matrix;

            Vector2 pivotNormalized = sprite.pivot / spriteRect.size;

            Vector3 centerLocal = (hoverImageRect.center - spriteRect.position - pivotNormalized * spriteRect.size) / sprite.pixelsPerUnit;

            if (spriteRenderer.flipX)
                centerLocal.x *= -1.0f;
            if (spriteRenderer.flipY)
                centerLocal.y *= -1.0f;

            Vector3 currentCenter = t.TransformPoint(centerLocal);

            Handles.matrix = t.localToWorldMatrix * Matrix4x4.TRS(centerLocal, Quaternion.identity, scale);

            Handles.color = Color.white;
            Handles.RectangleHandleCap(controlID, Vector3.zero, Quaternion.identity, 0.5f, Event.current.type);

            Handles.color = Color.black;
            Handles.DrawDottedLines(new Vector3[] {
                new Vector2(-0.5f,-0.5f), new Vector2(0.5f,-0.5f),
                new Vector2(0.5f,-0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0.5f,0.5f), new Vector2(-0.5f,0.5f),
                new Vector2(-0.5f,-0.5f), new Vector2(-0.5f,0.5f) },
                5.0f);

            Handles.matrix = tempMatrix;

            Vector3 newCenter = Handles.Slider2D(controlID, currentCenter, t.forward, t.right, t.up, 0.0f, Handles.RectangleHandleCap, Vector2.zero);

            if (newCenter != currentCenter)
            {
                if (!m_IsDirty)
                {
                    m_UndoData = new UndoDataPaint();
                    m_UndoData.pixelDataBefore = texture.GetPixels32(0);
                    m_UndoData.texture = texture;
                    ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

                    m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

                    ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                    ProtoSpriteData.SubmitSaveData(saveData);

                    CopyIntoHoverTexture(sprite, m_SelectionRect, true);
                }

                Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

                if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
                {
                    Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
                }

                m_IsDirty = true;
            }

            Vector2 newCenterLocal = t.InverseTransformPoint(newCenter);

            if (spriteRenderer.flipX)
                newCenterLocal.x *= -1.0f;
            if (spriteRenderer.flipY)
                newCenterLocal.y *= -1.0f;

            Vector2 newHoverImageRectCenter = (newCenterLocal * sprite.pixelsPerUnit) + spriteRect.position + pivotNormalized * spriteRect.size;// + spriteRect.size * 0.5f + ((pivotNormalized - Vector2.one * 0.5f) * spriteRect.size);

            Vector2 newHoverImagePositionFloat = newHoverImageRectCenter - hoverImageRect.size * 0.5f;
            m_SelectionRect.position = new Vector2(Mathf.FloorToInt(newHoverImagePositionFloat.x + 0.5f), Mathf.FloorToInt(newHoverImagePositionFloat.y + 0.5f));


            if (mouseUpAfterDrag && m_IsDirty)
            {
                var pixelData = new NativeArray<Color32>(texture.GetPixelData<Color32>(0), Allocator.TempJob);

                var job = new DrawJob()
                {
                    hoverPixelData = m_SomeTex.GetPixelData<Color32>(0),
                    targetPixelData = pixelData,
                    hoverTextureSize = new int2(m_SomeTex.width, m_SomeTex.height),
                    targetTextureSize = new int2(sprite.texture.width, sprite.texture.height),
                    hoverTexturePosition = new int2((int)m_SelectionRect.position.x, (int)m_SelectionRect.position.y),
                    spriteRect = sprite.rect
                };

                job.Schedule(sprite.texture.width * sprite.texture.height, 32).Complete();

                m_UndoData.pixelDataAfter = pixelData.ToArray();

                pixelData.Dispose();
            }
        }

        void ClearSelection()
        {
            m_SelectionRect.size = Vector2.zero;
        }

        void RotateCCW()
        {
            if (!m_IsDirty)
            {
                m_UndoData = new UndoDataPaint();
                m_UndoData.pixelDataBefore = m_TargetTexture.GetPixels32(0);
                m_UndoData.texture = m_TargetTexture;
                ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

                m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

                ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                ProtoSpriteData.SubmitSaveData(saveData);

                CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
            }

            Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

            if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
            {
                Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
            }

            m_IsDirty = true;


            Texture2D texture = m_SomeTex;


            // texture width and height swap

            // Copy the color32 pixel data
            var srcPixelData = texture.GetPixelData<Color32>(0);
            var dstPixelData = new NativeArray<Color32>(srcPixelData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int height = texture.height;
            int width = texture.width;

            // Pivot
            //TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

            int count = 1;

            for (int i = 0; i < count; i++)
            {
                RotatePixelDataJob job;
                job.dst = dstPixelData;
                job.src = srcPixelData;
                job.height = height;
                job.width = width;
                job.Schedule(width * height, 32).Complete();
                //importer.spritePivot = new Vector2(1.0f - importer.spritePivot.y, importer.spritePivot.x);

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
            texture.Reinitialize(texture.height, texture.width);

            m_SelectionRect.width = texture.width;
            m_SelectionRect.height = texture.height;

            texture.SetPixelData<Color32>(dstPixelData, 0);
            texture.Apply(true, false);

            dstPixelData.Dispose();
        }

        void FlipV()
        {
            if (!m_IsDirty)
            {
                m_UndoData = new UndoDataPaint();
                m_UndoData.pixelDataBefore = m_TargetTexture.GetPixels32(0);
                m_UndoData.texture = m_TargetTexture;
                ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

                m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

                ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                ProtoSpriteData.SubmitSaveData(saveData);

                CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
            }

            Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

            if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
            {
                Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
            }

            m_IsDirty = true;


            Texture2D texture = m_SomeTex;


            // texture width and height swap

            // Copy the color32 pixel data
            var srcPixelData = texture.GetPixelData<Color32>(0);
            var dstPixelData = new NativeArray<Color32>(srcPixelData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int height = texture.height;
            int width = texture.width;

            FlipVPixelDataJob job;
            job.dst = dstPixelData;
            job.src = srcPixelData;
            job.height = height;
            job.width = width;
            job.Schedule(width * height, 32).Complete();

            texture.SetPixelData<Color32>(dstPixelData, 0);
            texture.Apply(true, false);

            dstPixelData.Dispose();
        }

        void FlipH()
        {
            if (!m_IsDirty)
            {
                m_UndoData = new UndoDataPaint();
                m_UndoData.pixelDataBefore = m_TargetTexture.GetPixels32(0);
                m_UndoData.texture = m_TargetTexture;
                ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

                m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

                ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                ProtoSpriteData.SubmitSaveData(saveData);

                CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
            }

            Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

            if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
            {
                Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
            }

            m_IsDirty = true;


            Texture2D texture = m_SomeTex;


            // texture width and height swap

            // Copy the color32 pixel data
            var srcPixelData = texture.GetPixelData<Color32>(0);
            var dstPixelData = new NativeArray<Color32>(srcPixelData.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            int height = texture.height;
            int width = texture.width;

            FlipHPixelDataJob job;
            job.dst = dstPixelData;
            job.src = srcPixelData;
            job.height = height;
            job.width = width;
            job.Schedule(width * height, 32).Complete();

            texture.SetPixelData<Color32>(dstPixelData, 0);
            texture.Apply(true, false);

            dstPixelData.Dispose();
        }

        void Copy()
        {
            if (m_SelectionRect.size == Vector2.zero)
                return;

            if (m_SomeTex == null)
            {
                CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, false);
                m_ClipboardData = m_SomeTex.EncodeToPNG();
                EditorUtility.SetDirty(this);
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
            }
            else
            {
                m_ClipboardData = m_SomeTex.EncodeToPNG();
            }
        }

        void Cut()
        {
            if (m_SelectionRect.size == Vector2.zero)
                return;

            if (m_SomeTex == null)
            {
                if (!m_IsDirty)
                {
                    m_UndoData = new UndoDataPaint();
                    m_UndoData.pixelDataBefore = m_TargetTexture.GetPixels32(0);
                    m_UndoData.texture = m_TargetTexture;
                    ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

                    m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

                    ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
                    ProtoSpriteData.SubmitSaveData(saveData);

                    //CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
                }

                Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

                

                m_IsDirty = true;

                CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
                m_TargetTexture.Apply(true, false);

                m_UndoData.pixelDataAfter = m_TargetTexture.GetPixels32(0);

                m_ClipboardData = m_SomeTex.EncodeToPNG();
                //EditorUtility.SetDirty(this);
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
                m_SelectionRect = Rect.zero;

                if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
                {
                    Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
                }
            }
            else
            {
                //Debug.Log("dirty: " + m_IsDirty);

                Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");


                m_SelectionRect = Rect.zero;
                //m_IsDirty = true;

                //EditorUtility.SetDirty(this);

                

                if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
                {
                    Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
                }

                //Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

                //m_SelectionRect = new Rect(0, 0, 0, 0);
                //m_IsDirty = true;

                

                m_ClipboardData = m_SomeTex.EncodeToPNG();
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
                m_TargetTexture.Apply(true, false);

                m_UndoData.pixelDataAfter = m_TargetTexture.GetPixels32(0);
            }
        }

        void Paste()
        {
            if (m_ClipboardData == null || m_ClipboardData.Length == 0)
                return;

            DrawHoverImageFinal(m_TargetSprite);

            m_UndoData = new UndoDataPaint();
            m_UndoData.pixelDataBefore = m_TargetTexture.GetPixels32(0);
            m_UndoData.texture = m_TargetTexture;
            ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Select Tool");

            m_UndoGroupAtTimeOfDirty = Undo.GetCurrentGroup();

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(m_TargetTexture);
            ProtoSpriteData.SubmitSaveData(saveData);

            if (!m_IsDirty)
            {
               

                //CopyIntoHoverTexture(m_TargetSprite, m_SelectionRect, true);
            }

            Undo.RegisterCompleteObjectUndo(this, "ProtoSprite Select Tool");

            if (Undo.GetCurrentGroupName() == "ProtoSprite Select Tool")
            {
                Undo.CollapseUndoOperations(m_UndoGroupAtTimeOfDirty);
            }

            m_IsDirty = true;

            if (m_SomeTex != null)
            {
                DestroyImmediate(m_SomeTex);
                m_SomeTex = null;
            }

            m_SomeTex = new Texture2D(1, 1);
            m_SomeTex.LoadImage(m_ClipboardData, false);
            Color[] colors = m_SomeTex.GetPixels();
            m_SomeTex.Reinitialize(m_SomeTex.width, m_SomeTex.height, TextureFormat.RGBA32, false);
            m_SomeTex.SetPixels(colors);
            m_SomeTex.Apply(false, false);

            //Debug.Log("some tex format: " + m_SomeTex.format + " sprite tex format: " + m_TargetSprite.texture.format);

            Rect sceneRect = SceneView.lastActiveSceneView.cameraViewport;
            var coord = ProtoSpriteData.GetPixelCoord(new Vector2(sceneRect.width * 0.5f, sceneRect.height * 0.5f)) - new int2(m_SomeTex.width / 2, m_SomeTex.height / 2);
            //Debug.Log("Coord: " + coord + " mouse pos: " + Event.current.mousePosition);
            //Debug.Log("SceneView.lastActiveSceneView.cameraViewport: " + SceneView.lastActiveSceneView.cameraViewport);
            m_SelectionRect = new Rect(coord.x, coord.y, m_SomeTex.width, m_SomeTex.height);
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

        [BurstCompile]
        struct FlipVPixelDataJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> src;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> dst;

            public int height;
            public int width;

            public void Execute(int i)
            {
                int x = i % width;
                int y = i / width;

                dst[x + y * width] = src[x + (height - y - 1) * width];
            }
        }

        [BurstCompile]
        struct FlipHPixelDataJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> src;
            [NativeDisableParallelForRestriction] public NativeArray<Color32> dst;

            public int height;
            public int width;

            public void Execute(int i)
            {
                int x = i % width;
                int y = i / width;

                dst[x + y * width] = src[(width - x - 1) + y * width];
            }
        }
    }
}