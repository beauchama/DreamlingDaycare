using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEditor.Sprites;
using UnityEditor.ShortcutManagement;

namespace ProtoSprite.Editor
{
    public class ResizeTool : ProtoSpriteTool
    {
        Rect m_NextRect = new Rect();
        Rect m_PreviousRect = new Rect();

        SpriteRenderer m_TargetSpriteRenderer = null;

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("RectTool"));
                content.tooltip = "Resize (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Resize Tool") + ")";
                return content;
            }
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
            // If the tool is active and we exit play mode then OnDisable is called but OnWillBeDeactivated isn't so we force call it here
            if (ToolManager.activeToolType == GetType())
                OnWillBeDeactivated();
        }

        [Shortcut("ProtoSprite/Resize Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.N)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<ResizeTool>();
        }

        public override void ProtoSpriteWindowGUI()
		{
            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string reason))
                return;

            Transform t = Selection.activeTransform;
            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;

            int newTextureWidth = Mathf.Clamp(Mathf.RoundToInt(m_NextRect.size.x * sprite.pixelsPerUnit), 1, ProtoSpriteWindow.kMaxTextureSize);
            int newTextureHeight = Mathf.Clamp(Mathf.RoundToInt(m_NextRect.size.y * sprite.pixelsPerUnit), 1, ProtoSpriteWindow.kMaxTextureSize);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Width", newTextureWidth);
            EditorGUILayout.FloatField("Height", newTextureHeight);
            EditorGUI.EndDisabledGroup();
        }

		public override void OnActivated()
        {
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();

            Finish(m_TargetSpriteRenderer);
            m_TargetSpriteRenderer = null;
        }

		public override bool IsToolCompatible(out string invalidReason)
		{
            // Not compatible if target is multisprite
            try
            {
                Transform t = Selection.activeTransform;
                SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
                Sprite sprite = spriteRenderer.sprite;
                Texture texture = sprite.texture;

                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    invalidReason = "Can only resize Sprite textures that are Sprite Mode Single.";
                    return false;
                }
            }
            catch {}

            return base.IsToolCompatible(out invalidReason);
		}

		public override void OnToolGUI(EditorWindow window)
        {
            Event e = Event.current;

            if (!(window is SceneView sceneView))
                return;

            ProtoSpriteData.RepaintSceneViewsIfUnityFocused();
            ProtoSpriteData.RepaintSpriteEditorWindow();

            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string invalidReason) || !IsToolCompatible(out string invalidToolReason))
            {
                ProtoSpriteData.DrawInvalidHandles();
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Transform t = Selection.activeTransform;
            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);
            m_TargetSpriteRenderer = spriteRenderer;

            bool onMouseUp = false;

            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                onMouseUp = true;
            }

            // Check for external changes
            {
                Vector2 spritePivot = sprite.pivot;
                if (spriteRenderer.flipX)
                    spritePivot.x = texture.width - sprite.pivot.x;
                if (spriteRenderer.flipY)
                    spritePivot.y = texture.height - sprite.pivot.y;

                Vector2 min = new Vector2(-(spritePivot.x / sprite.pixelsPerUnit), -(spritePivot.y / sprite.pixelsPerUnit));// - localSize * 0.5f;
                Vector2 max = new Vector2((texture.width - spritePivot.x) / sprite.pixelsPerUnit, (texture.height - spritePivot.y) / sprite.pixelsPerUnit);

                Rect externalRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                if (m_PreviousRect != externalRect)
                {
                    m_NextRect = externalRect;
                    m_PreviousRect = m_NextRect;
                }
            }

            DrawHandles(ref m_NextRect, m_PreviousRect, ProtoSpriteWindow.kMaxTextureSize / sprite.pixelsPerUnit);

            if (onMouseUp)
            {
                Finish(m_TargetSpriteRenderer);
            }
        }

        public static void DrawHandles(ref Rect rect, Rect oldRect, float maxLocalSize)
        {
            Event ev = Event.current;

            Transform t = Selection.activeTransform;

            float size = 0.05f * HandleUtility.GetHandleSize(t.position);

            if (ev.type == EventType.Repaint)
            {
                var tempMatrix = Handles.matrix;

                Handles.matrix = t.localToWorldMatrix;

                Rect tempRect = rect;
                tempRect.width *= 0.5f;
                tempRect.height *= 0.5f;

                //Handles.color = Color.black;
                //Handles.ClearCamera(rect, Camera.current);
                //Handles.DrawSolidRectangleWithOutline(tempRect, new Color(0f, 0f, 1f, 1f), Color.white);

                //Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                //HandleUtility.

                float fillAlpha = 0.1f;

                if (ProtoSpriteWindow.GetInstance().DebugRender)
                    fillAlpha = 0.0f;

                Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 1f, 0f, fillAlpha), Color.white);

                Handles.matrix = tempMatrix;

                // Draw sprite pivot
                var tempColor = Handles.color;
                Handles.color = Color.white;
                Handles.DrawSolidDisc(t.position, t.forward, size);
                Handles.color = Color.black;
                Handles.DrawWireDisc(t.position, t.forward, size, 0.02f * HandleUtility.GetHandleSize(t.position));
                Handles.color = tempColor;
            }

            float xMin = rect.xMin;
            float yMin = rect.yMin;
            float xMax = rect.xMax;
            float yMax = rect.yMax;

            Vector3 minWorldPos = t.TransformPoint(rect.min);
            Vector3 maxWorldPos = t.TransformPoint(rect.max);

            float localPixelSize = maxLocalSize / ProtoSpriteWindow.kMaxTextureSize;

            float selectedLineThickness = 4.0f;

            // Each value is clamped and rouned to:
            // Clamp between 1 and "ProtoSpriteWindow.kMaxSize" pixels
            // Round to only allow increments of a pixel in size

            // xMax
            xMax = DrawEdgeHandle("ProtoSprite.ResizeTool.xMax", maxWorldPos, t, new Vector2(xMax, yMin), new Vector2(xMax, yMax), selectedLineThickness).x;
            xMax = Mathf.Clamp(xMax, rect.min.x + localPixelSize, rect.min.x + maxLocalSize);
            xMax = rect.xMin + Mathf.Round((xMax - rect.xMin) / localPixelSize) * localPixelSize;

            // xMin
            xMin = DrawEdgeHandle("ProtoSprite.ResizeTool.xMin", minWorldPos, t, new Vector2(xMin, yMin), new Vector2(xMin, yMax), selectedLineThickness).x;
            xMin = Mathf.Clamp(xMin, xMax - maxLocalSize, xMax - localPixelSize);
            xMin = xMax - Mathf.Round((xMax - xMin) / localPixelSize) * localPixelSize;

            // yMax
            yMax = DrawEdgeHandle("ProtoSprite.ResizeTool.yMax", maxWorldPos, t, new Vector2(xMin, yMax), new Vector2(xMax, yMax), selectedLineThickness).y;
            yMax = Mathf.Clamp(yMax, oldRect.yMin + localPixelSize, oldRect.yMin + maxLocalSize);
            yMax = oldRect.yMin + Mathf.Round((yMax - oldRect.yMin) / localPixelSize) * localPixelSize;

            // yMin
            yMin = DrawEdgeHandle("ProtoSprite.ResizeTool.yMin", minWorldPos, t, new Vector2(xMin, yMin), new Vector2(xMax, yMin), selectedLineThickness).y;
            yMin = Mathf.Clamp(yMin, yMax - maxLocalSize, yMax - localPixelSize);
            yMin = yMax - Mathf.Round((yMax - yMin) / localPixelSize) * localPixelSize;

            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static Vector2 DrawEdgeHandle(string controlName, Vector3 pos, Transform t, Vector2 localP0, Vector2 localP1, float selectedLineThickness)
        {
            int id = GUIUtility.GetControlID(controlName.GetHashCode(), FocusType.Passive);

            Vector3 newPos = Handles.Slider2D(id, pos, t.forward, t.right, t.up, 0, null, Vector2.zero);

            Vector3 p0 = t.TransformPoint(localP0);
            Vector3 p1 = t.TransformPoint(localP1);

            float distanceToLine = HandleUtility.DistanceToLine(p0, p1);
            HandleUtility.AddControl(id, distanceToLine * 0.2f);

            if (HandleUtility.nearestControl == id || GUIUtility.hotControl == id)
                Handles.DrawLine(p0, p1, selectedLineThickness);

            return t.InverseTransformPoint(newPos);
        }

        void Finish(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
                return;

            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = sprite.texture;

            int newTextureWidth = Mathf.Clamp(Mathf.RoundToInt(m_NextRect.size.x * sprite.pixelsPerUnit), 1, ProtoSpriteWindow.kMaxTextureSize);
            int newTextureHeight = Mathf.Clamp(Mathf.RoundToInt(m_NextRect.size.y * sprite.pixelsPerUnit), 1, ProtoSpriteWindow.kMaxTextureSize);

            float pivotX = (0 - m_NextRect.xMin);
            float pivotY = (0 - m_NextRect.yMin);

            Vector2 pivotLocal = new Vector2(pivotX, pivotY);
            Vector2 pivotPixels = pivotLocal * sprite.pixelsPerUnit;

            if (spriteRenderer.flipX)
                pivotPixels.x = newTextureWidth - pivotPixels.x;
            if (spriteRenderer.flipY)
                pivotPixels.y = newTextureHeight - pivotPixels.y;

            Vector2 pivotNormalized = pivotPixels / new Vector2(newTextureWidth, newTextureHeight);

            float widthOffset = m_NextRect.center.x - m_PreviousRect.center.x;
            int widthPixelDiff = newTextureWidth - texture.width;

            float heightOffset = m_NextRect.center.y - m_PreviousRect.center.y;
            int heightPixelDiff = newTextureHeight - texture.height;

            Vector2 newPivotPixels = pivotPixels;

            Vector2 min = new Vector2(-(newPivotPixels.x / sprite.pixelsPerUnit), -(newPivotPixels.y / sprite.pixelsPerUnit));// - localSize * 0.5f;
            Vector2 max = new Vector2((newTextureWidth - newPivotPixels.x) / sprite.pixelsPerUnit, (newTextureHeight - newPivotPixels.y) / sprite.pixelsPerUnit);

            m_NextRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            m_PreviousRect = m_NextRect;

            // Set new sprite pivot
            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);

            if (pivotNormalized == importerSettings.spritePivot && texture.width == newTextureWidth && texture.height == newTextureHeight)
                return;

            //Debug.Log("pivotNormalized: " + pivotNormalized + " spritePivot: " + importerSettings.spritePivot);

            // Trigger save
            //ProtoSpriteData.Saving.SaveTextureIfDirty(texture);

            var pixelsBefore = texture.GetPixels32(0);
            var pixelsAfter = pixelsBefore;

            // UNDO
            UndoDataResize undoData = new UndoDataResize();
            undoData.texture = texture;
            undoData.textureSizeBefore = new Vector2Int(texture.width, texture.height);
            undoData.pixelDataBefore = pixelsBefore;
            undoData.spritePivotNormalizedBefore = sprite.pivot / new Vector2(texture.width, texture.height);
            ProtoSpriteData.SubmitUndoData(undoData, "ProtoSprite Resize Tool");


            // Copy previous pixels into new texture
            if (texture.width != newTextureWidth || texture.height != newTextureHeight)
            {
                int oldWidth = texture.width;
                int oldHeight = texture.height;

                pixelsAfter = new Color32[newTextureWidth * newTextureHeight];

                bool flipX = spriteRenderer.flipX;
                bool flipY = spriteRenderer.flipY;

                for (int x = 0; x < newTextureWidth; x++)
                {
                    for (int y = 0; y < newTextureHeight; y++)
                    {
                        //Color32 color = new Color32(0, 0, 0, 0);

                        int oldY = y;
                        if (heightOffset > 0 && heightPixelDiff < 0 || heightOffset < 0 && heightPixelDiff > 0)
                        {
                            oldY = flipY ? y : y - heightPixelDiff;
                        }
                        else
                        {
                            oldY = flipY ? y - heightPixelDiff : y;
                        }

                        int oldX = x;
                        if (widthOffset > 0 && widthPixelDiff < 0 || widthOffset < 0 && widthPixelDiff > 0)
                        {
                            oldX = flipX ? x : x - widthPixelDiff;
                        }
                        else
                        {
                            oldX = flipX ? x - widthPixelDiff : x;
                        }

                        if (oldX < oldWidth && oldY < oldHeight && oldY >= 0 && oldX >= 0)
                        {
                            pixelsAfter[x + (y * newTextureWidth)] = pixelsBefore[oldX + (oldY * oldWidth)];
                        }

                        //pixelsAfter[x + (y * newTextureWidth)] = color;
                    }
                }

                texture.Reinitialize(newTextureWidth, newTextureHeight);

                texture.SetPixels32(pixelsAfter);
                // Don't need to apply after this because we're going to reimport the texture anyway to update the sprite pivot
                //texture.Apply(false, false);
            }

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(texture);
            ProtoSpriteData.SubmitSaveData(saveData);
            ProtoSpriteData.Saving.SaveTextureIfDirty(texture, false);
            //SaveToFile();

            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importerSettings.spritePivot = pivotNormalized;
            textureImporter.SetTextureSettings(importerSettings);

            textureImporter.SaveAndReimport();

            // UNDO
            undoData.textureSizeAfter = new Vector2Int(texture.width, texture.height);
            undoData.pixelDataAfter = pixelsAfter;
            undoData.spritePivotNormalizedAfter = sprite.pivot / new Vector2(texture.width, texture.height);
        }
    }
}