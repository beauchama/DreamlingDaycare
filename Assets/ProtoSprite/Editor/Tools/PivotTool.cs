using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEditor.Sprites;
using Unity.Collections;
using UnityEngine.U2D;
using UnityEditor.ShortcutManagement;
using UnityEditor.U2D.Sprites;

namespace ProtoSprite.Editor
{
    public class PivotTool : ProtoSpriteTool
    {
        Sprite m_TargetSprite = null;
        [SerializeField] Vector2 m_TargetSpritePivotNormalized = Vector2.zero;

        bool m_IsDirty = false;

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("ToolHandlePivot"));
                content.tooltip = "Pivot (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Pivot Tool") + ")";
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

        [Shortcut("ProtoSprite/Pivot Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.C)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<PivotTool>();
        }

        public override void ProtoSpriteWindowGUI()
        {
            base.ProtoSpriteWindowGUI();
            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string invalidReason))
                return;

            Transform t = Selection.activeTransform;

            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;

            var eventType = Event.current.rawType;

            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.Vector2Field("Pivot", m_TargetSpritePivotNormalized);

            if (EditorGUI.EndChangeCheck())
            {
                m_TargetSpritePivotNormalized = newValue;
                SetSpriteTempPivot(sprite, m_TargetSpritePivotNormalized);
                m_IsDirty = true;
            }

            if (eventType == EventType.MouseUp || eventType == EventType.KeyUp)
            {
                //Debug.Log("event before " + eventType + " after: " + Event.current.rawType);
                Finish(m_TargetSprite);
            }
        }

		public override void OnActivated()
        {
        }

        public override void OnWillBeDeactivated()
        {
            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string invalidReason))
                return;

            Finish(m_TargetSprite);

            m_TargetSprite = null;
        }

        void UpdateTarget()
        {
            bool validSelection = ProtoSpriteWindow.IsSelectionValidProtoSprite(out string reason);
            bool changedTarget = false;

            if (m_TargetSprite == null && validSelection)
            {
                changedTarget = true;
            }

            if (m_TargetSprite != null && !validSelection)
            {
                changedTarget = true;
            }

            if (m_TargetSprite != null && validSelection)
            {
                Transform t = Selection.activeTransform;
                SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
                Sprite sprite = spriteRenderer.sprite;

                changedTarget = m_TargetSprite != sprite;
            }

            if (changedTarget && m_TargetSprite != null)
            {
                Finish(m_TargetSprite);
            }

            if (changedTarget)
            {
                try
                {
                    Transform t = Selection.activeTransform;
                    SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
                    Sprite sprite = spriteRenderer.sprite;

                    m_TargetSpritePivotNormalized = sprite.pivot / sprite.rect.size;
                }
                catch { }
            }
        }
        

        public override void OnToolGUI(EditorWindow window)
        {
            UpdateTarget();

            if (!(window is SceneView sceneView))
                return;

            ProtoSpriteData.RepaintSceneViewsIfUnityFocused();
            ProtoSpriteData.RepaintSpriteEditorWindow();

            if (!ProtoSpriteWindow.IsSelectionValidProtoSprite(out string invalidReason))
            {
                ProtoSpriteData.DrawInvalidHandles();
                return;
            }

            int passiveControl = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveControl);

            Transform t = Selection.activeTransform;

            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);
            m_TargetSprite = sprite;
            Event ev = Event.current;

            bool mouseUp = false;
            bool mouseDown = false;
            bool mouseDrag = false;
            bool ctrlModifier = false;
            if (ev.rawType == EventType.MouseUp && ev.button == 0)
            {
                mouseUp = true;
            }

            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                mouseDown = true;
            }

            if (ev.type == EventType.MouseDrag && ev.button == 0)
            {
                mouseDrag = true;
            }

            if ((ev.modifiers & EventModifiers.Control) != 0 || (ev.modifiers & EventModifiers.Command) != 0)
            {
                ctrlModifier = true;
            }

            float size = 0.05f * HandleUtility.GetHandleSize(t.position);

            if (ev.type == EventType.Repaint)
            {
                // Draw sprite pivot
                var tempColor = Handles.color;
                Handles.color = Color.white;
                Handles.DrawSolidDisc(t.position, t.forward, size);
                Handles.color = Color.black;
                Handles.DrawWireDisc(t.position, t.forward, size, 0.02f * HandleUtility.GetHandleSize(t.position));
                Handles.color = tempColor;
            }

            try
            {
                int controlID = GUIUtility.GetControlID("ProtoSprite.PivotTool".GetHashCode(), FocusType.Passive);


                Rect spriteRect = sprite.rect;

                Vector2 spritePivot = sprite.pivot;
                

                Vector3 scale = (new Vector3(spriteRect.width, spriteRect.height, 1.0f) / sprite.pixelsPerUnit);

                Matrix4x4 tempMatrix = Handles.matrix;


                m_TargetSpritePivotNormalized = GetSpriteTempPivot(sprite);

                Vector2 pivotNormalized = m_TargetSpritePivotNormalized;

                if (spriteRenderer.flipX)
                    pivotNormalized.x = 1.0f - pivotNormalized.x;
                if (spriteRenderer.flipY)
                    pivotNormalized.y = 1.0f - pivotNormalized.y;

                Vector3 centerLocal = -((pivotNormalized - Vector2.one * 0.5f) * spriteRect.size) / sprite.pixelsPerUnit;

                Vector3 currentCenter = t.TransformPoint(centerLocal);

                Handles.matrix = t.localToWorldMatrix * Matrix4x4.TRS(centerLocal, Quaternion.identity, scale);

                Handles.color = Color.white;
                Handles.RectangleHandleCap(controlID, Vector3.zero, Quaternion.identity, 0.5f, ev.type);

                Handles.matrix = tempMatrix;

                Vector3 newCenter = Handles.Slider2D(controlID, currentCenter, t.forward, t.right, t.up, 0.0f, Handles.RectangleHandleCap, Vector2.zero);

                Vector3 newCenterLocal = t.InverseTransformPoint(newCenter);

                // Clamp to pixels
                if (ctrlModifier)
                {
                    var maxLocalSize = ProtoSpriteWindow.kMaxTextureSize / sprite.pixelsPerUnit;

                    float localPixelSize = maxLocalSize / ProtoSpriteWindow.kMaxTextureSize;

                    if (spriteRect.width % 2 == 0)
                    {
                        newCenterLocal.x = Mathf.Round((newCenterLocal.x) / localPixelSize) * localPixelSize;
                    }
                    else
                    {
                        newCenterLocal.x = Mathf.Round((newCenterLocal.x + localPixelSize * 0.5f) / localPixelSize) * localPixelSize - localPixelSize * 0.5f;
                    }

                    if (spriteRect.height % 2 == 0)
                    {
                        newCenterLocal.y = Mathf.Round((newCenterLocal.y) / localPixelSize) * localPixelSize;
                    }
                    else
                    {
                        newCenterLocal.y = Mathf.Round((newCenterLocal.y + localPixelSize * 0.5f) / localPixelSize) * localPixelSize - localPixelSize * 0.5f;
                    }
                }

                if (spriteRenderer.flipX)
                {
                    newCenterLocal.x *= -1.0f;
                    centerLocal.x *= -1.0f;
                }

                if (spriteRenderer.flipY)
                {
                    newCenterLocal.y *= -1.0f;
                    centerLocal.y *= -1.0f;
                }

                Vector3 diff = newCenterLocal - centerLocal;

                if ((mouseDown || mouseDrag) && !mouseUp && newCenterLocal != centerLocal)
                {
                    m_TargetSpritePivotNormalized = Vector2.one * 0.5f - ((Vector2)newCenterLocal * sprite.pixelsPerUnit) / spriteRect.size;
                    SetSpriteTempPivot(sprite, m_TargetSpritePivotNormalized);
                    m_IsDirty = true;
                }

                if (mouseUp)
                {
                    Finish(m_TargetSprite);
                }
            }
            catch { }
        }

        Vector2 GetSpriteTempPivot(Sprite sprite)
        {
            if (!m_IsDirty)
                ProtoSpriteData.GenerateFullRectOverrideGeometry(sprite);

            var positions = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);
            var uvs = sprite.GetVertexAttribute<Vector2>(UnityEngine.Rendering.VertexAttribute.TexCoord0);

            Vector3 centerLocal = Vector3.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];
                centerLocal += pos;
            }
            centerLocal /= (float)positions.Length;

            Rect spriteRect = sprite.rect;

            Vector2 pivotNormalized = Vector2.one * 0.5f - ((centerLocal * sprite.pixelsPerUnit) / spriteRect.size);

            return pivotNormalized;
        }

        void SetSpriteTempPivot(Sprite sprite, Vector2 pivotNormalized)
        {
            if (!m_IsDirty)
                ProtoSpriteData.GenerateFullRectOverrideGeometry(sprite);

            var positions = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);
            var uvs = sprite.GetVertexAttribute<Vector2>(UnityEngine.Rendering.VertexAttribute.TexCoord0);


            Vector3 centerLocal = Vector3.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];
                centerLocal += pos;
            }
            centerLocal /= (float)positions.Length;

            Rect spriteRect = sprite.rect;


            var newPositions = new NativeArray<Vector3>(positions.Length, Allocator.Persistent);
            var newUVs = new NativeArray<Vector2>(positions.Length, Allocator.Persistent);


            for (int i = 0; i < newPositions.Length; i++)
            {
                newPositions[i] = (positions[i] - centerLocal) - (Vector3)((pivotNormalized - Vector2.one * 0.5f) * (new Vector2(spriteRect.width, spriteRect.height) / sprite.pixelsPerUnit));
                newUVs[i] = uvs[i];
            }

            sprite.SetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position, newPositions);
            sprite.SetVertexAttribute<Vector2>(UnityEngine.Rendering.VertexAttribute.TexCoord0, newUVs);

            newPositions.Dispose();
            newUVs.Dispose();
        }

        void Finish(Sprite sprite)
        {
            if (!m_IsDirty)
                return;

            ProtoSpriteData.Saving.SaveTextureIfDirty(sprite.texture, false);

            Texture2D texture = sprite.texture;

            // Undo
            UndoDataPivot undoData = new UndoDataPivot();
            undoData.texture = texture;
            undoData.spriteName = sprite.name;
            undoData.spritePivotNormalizedBefore = sprite.pivot / new Vector2(texture.width, texture.height);
            ProtoSpriteData.SubmitUndoData(undoData, "ProtoSprite Pivot Tool");

            // Support multisprites
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = dataProvider.GetSpriteRects();
            foreach (var rect in spriteRects)
            {
                if (rect.name == sprite.name)
                {
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = m_TargetSpritePivotNormalized;
                    break;
                }
            }

            // Write the updated data back to the data provider
            dataProvider.SetSpriteRects(spriteRects);

            // Apply the changes made to the data provider
            dataProvider.Apply();

            // Reimport the asset to have the changes applied
            var assetImporter = dataProvider.targetObject as AssetImporter;
            assetImporter.SaveAndReimport();

            // Undo
            undoData.spritePivotNormalizedAfter = sprite.pivot / new Vector2(texture.width, texture.height);

            m_IsDirty = false;
        }
    }
}