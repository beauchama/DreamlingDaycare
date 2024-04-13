using UnityEditor;
using UnityEngine;
using UnityEditor.Sprites;
using Unity.Mathematics;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;
using System.Collections.Generic;

namespace ProtoSprite.Editor
{
    public class EraseTool : ProtoSpriteTool
    {
        Texture2D m_TargetTexture = null;

        int2 m_BrushResizeMousePrevious = int2.zero;
        int2 m_BrushResizeMouseStart = int2.zero;
        Vector2 m_BrushResizeMouseScreenPrevious = Vector2.zero;
        bool m_IsResizingBrush = false;

        bool m_SceneSelectionOutlineGizmoEnabled = false;

        int m_BrushSize = 25;

        bool m_IsPreviewDrawn = false;
        bool m_IsDirty = false;

        bool m_TryingToPaint = false;

        UndoDataPaint m_UndoData = null;

        int2 m_PreviousMousePixel = int2.zero;

        EditorWindow m_PaintStartWindow = null;

        BrushShape m_BrushShape = BrushShape.CIRCLE;

        List<int2> m_PreviousPixelCoords = new List<int2>();
        Color32 m_PreviousPaintedPixelColor = Color.clear;

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("Grid.EraserTool"));
                content.tooltip = "Erase (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Erase Tool") + ")";
                return content;
            }
        }

        public int BrushSize
        {
            get => m_BrushSize;
            set
            {
                value = Mathf.Clamp(value, 1, ProtoSpriteWindow.kMaxTextureSize);
                m_BrushSize = value;
            }
        }

        public BrushShape BrushShape { get => m_BrushShape; set => m_BrushShape = value; }

        public bool PixelPerfect
        {
            get
            {
                return ProtoSpriteWindow.GetInstance().GetToolInstance<PaintTool>().PixelPerfect;
            }
            set
            {
                ProtoSpriteWindow.GetInstance().GetToolInstance<PaintTool>().PixelPerfect = value;
            }
        }

        public void OnEnable()
        {
            BrushSize = EditorPrefs.GetInt("ProtoSprite.Editor.EraseTool.BrushSize", 25);
            BrushShape = (BrushShape)EditorPrefs.GetInt("ProtoSprite.Editor.EraseTool.BrushShape", (int)BrushShape.CIRCLE);
        }

        public void OnDisable()
        {
            // If the tool is active and we exit play mode then OnDisable is called but OnWillBeDeactivated isn't so we force call it here
            if (ToolManager.activeToolType == GetType())
                OnWillBeDeactivated();

            EditorPrefs.SetInt("ProtoSprite.Editor.EraseTool.BrushSize", BrushSize);
            EditorPrefs.SetInt("ProtoSprite.Editor.EraseTool.BrushShape", (int)BrushShape);
        }

        [Shortcut("ProtoSprite/Erase Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.D)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<EraseTool>();
        }

        public override void ProtoSpriteWindowGUI()
        {
            BrushSize = EditorGUILayout.IntField("Erase Size", BrushSize);

            BrushShape = (BrushShape)EditorGUILayout.Popup("Erase Shape", (int)BrushShape, new string[] { "Circle", "Square" });

            {
                GUIContent label = new GUIContent("Pixel Perfect");
                label.tooltip = "Dynamically adjusts painted pixels to remove L-shapes. Only usable when brush size is 1 pixel.";
                PixelPerfect = EditorGUILayout.Toggle(label, PixelPerfect);
            }
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

            m_BrushResizeMousePrevious = int2.zero;
            m_BrushResizeMouseStart = int2.zero;
            m_BrushResizeMouseScreenPrevious = Vector2.zero;
            m_IsResizingBrush = false;

            m_IsPreviewDrawn = false;
            m_IsDirty = false;

            m_TryingToPaint = false;
        }

        void Finish()
        {
            m_IsResizingBrush = false;

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

            Texture2D selectedTexture = null;

            if (m_TargetTexture != null && !validSelection)
            {
                changedTarget = true;
            }

            if (m_TargetTexture != null && validSelection)
            {
                Transform t = Selection.activeTransform;
                SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
                Sprite sprite = spriteRenderer.sprite;
                selectedTexture = SpriteUtility.GetSpriteTexture(sprite, false);

                changedTarget = m_TargetTexture != selectedTexture;
            }

            if (changedTarget)
            {
                Finish();
            }

            m_TargetTexture = selectedTexture;
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

            Transform t = Selection.activeTransform;

            SpriteRenderer spriteRenderer = t.GetComponent<SpriteRenderer>();
            Sprite sprite = spriteRenderer.sprite;
            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);
            m_TargetTexture = texture;
            int2 pixelCoord = ProtoSpriteData.GetPixelCoord();

            bool isColorPickerOpened = ProtoSpriteData.IsColorPickerOpen();

            DrawHandles(t);

            // Draw brush outline
            if (e.type == EventType.Repaint)
            {
                int2 brushOutlinePixelCoord = pixelCoord;
                if (m_IsResizingBrush)
                {
                    brushOutlinePixelCoord = new int2(m_BrushResizeMouseStart.x, m_BrushResizeMouseStart.y);
                }
                PaintTool.DrawBrushOutline(spriteRenderer, brushOutlinePixelCoord, m_BrushSize, m_BrushShape, window as SceneView);
            }

            int passiveControl = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveControl);

            if (Event.current.GetTypeForControl(passiveControl) == EventType.MouseDown && e.button == 0)
            {
                GUIUtility.hotControl = passiveControl;
            }

            // Resizing brush
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (e.modifiers == EventModifiers.Control || e.modifiers == EventModifiers.Command)
                {
                    m_IsResizingBrush = true;
                    m_BrushResizeMousePrevious = pixelCoord;
                    m_BrushResizeMouseStart = pixelCoord;
                    m_BrushResizeMouseScreenPrevious = e.mousePosition;
                }
                else
                {
                    m_IsResizingBrush = false;
                }
            }

            if (m_IsResizingBrush && e.rawType == EventType.MouseDrag)
            {
                m_BrushResizeMouseScreenPrevious.x += e.delta.x;
                int2 pixelCoordNew = ProtoSpriteData.GetPixelCoord(m_BrushResizeMouseScreenPrevious);
                int2 mouseDiff = pixelCoordNew - m_BrushResizeMousePrevious;
                m_BrushResizeMousePrevious = pixelCoordNew;

                float distance = math.length(mouseDiff);

                Vector2 mouseScreenDiff = e.delta;

                if (mouseScreenDiff.x > 0.0f)
                {
                    distance = Mathf.Abs(distance);
                }
                else
                {
                    distance = -Mathf.Abs(distance);
                }

                BrushSize += (int)distance;
            }

            // Painting
            if (e.rawType == EventType.MouseDown && e.button == 0 && !isColorPickerOpened)
            {
                m_PaintStartWindow = window;
                m_TryingToPaint = true;

                // Holding shift allows for drawing quick lines from previous location
                if (e.modifiers != EventModifiers.Shift)
                    m_PreviousMousePixel = pixelCoord;
            }

            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                m_TryingToPaint = false;

                m_PreviousPixelCoords.Clear();

                if (m_IsResizingBrush)
                {
                    m_IsResizingBrush = false;
                }
                else
                {
                    Finish();
                }
            }
            else if (m_TryingToPaint && window == m_PaintStartWindow && (e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseDown))
            {
                if (!m_IsResizingBrush && (e.modifiers == EventModifiers.None || e.modifiers == EventModifiers.Shift))
                {
                    if (!m_IsDirty)
                    {
                        m_UndoData = new UndoDataPaint();
                        m_UndoData.pixelDataBefore = texture.GetPixels32(0);
                        m_UndoData.texture = texture;
                        ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Erase Tool");
                    }

                    m_IsDirty = true;

                    if (m_PreviousPixelCoords.Count == 0 || math.any(pixelCoord != m_PreviousPixelCoords[m_PreviousPixelCoords.Count - 1]))
                    {
                        //Debug.Log("Painting: " + pixelCoord + " " + m_PreviousPixelCoords.Count);

                        if (m_BrushSize == 1 && PixelPerfect && sprite.rect.Contains(new Vector2(pixelCoord.x, pixelCoord.y)))
                        {
                            m_PreviousPixelCoords.Add(pixelCoord);
                        }


                        var tempCol = sprite.texture.GetPixel(pixelCoord.x, pixelCoord.y);

                        ProtoSpriteData.DrawLineSimultaneousCPUAndGPU(sprite, m_PreviousMousePixel, pixelCoord, BrushSize, Color.clear, BrushShape);

                        // Pixel perfect mode, undo previous pixel if current pixel would create an L-shape
                        if (m_PreviousPixelCoords.Count >= 3 && PixelPerfect && BrushSize == 1)
                        {
                            var a = m_PreviousPixelCoords;

                            var a_0 = m_PreviousPixelCoords[m_PreviousPixelCoords.Count - 1];
                            var a_1 = m_PreviousPixelCoords[m_PreviousPixelCoords.Count - 2];
                            var a_2 = m_PreviousPixelCoords[m_PreviousPixelCoords.Count - 3];

                            if ((math.abs(a_0.x - a_1.x) == 1 && a_2.x == a_1.x && math.abs(a_2.y - a_1.y) == 1 && a_0.y == a_1.y) || (math.abs(a_0.y - a_1.y) == 1 && a_2.y == a_1.y && math.abs(a_2.x - a_1.x) == 1 && a_0.x == a_1.x))
                            {
                                ProtoSpriteData.DrawLineSimultaneousCPUAndGPU(sprite, a_1, a_1, BrushSize, m_PreviousPaintedPixelColor, BrushShape);
                                m_PreviousPixelCoords.RemoveAt(m_PreviousPixelCoords.Count - 2);
                            }
                        }

                        m_PreviousPaintedPixelColor = tempCol;
                    }
                }
            }

            if ((e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseDown) && e.button == 0 && !isColorPickerOpened && window == m_PaintStartWindow)
            {
                m_PreviousMousePixel = pixelCoord;
            }

            // Preview
            if (e.type != EventType.Repaint)
                return;

            if (m_TryingToPaint && m_PaintStartWindow != window)
                return;

            var mouseOverWindow = EditorWindow.mouseOverWindow;

            if (mouseOverWindow != window && !m_IsResizingBrush)
            {
                if (mouseOverWindow == null || mouseOverWindow.GetType() != typeof(SceneView))
                {
                    if (m_IsPreviewDrawn)
                    {
                        m_TargetTexture.Apply(true, false);
                        m_IsPreviewDrawn = false;
                    }
                }
            }

            bool shouldDrawPreview = !isColorPickerOpened && ((mouseOverWindow != null && mouseOverWindow == window && mouseOverWindow.GetType() == typeof(SceneView)) || m_IsResizingBrush || (m_TryingToPaint && m_PaintStartWindow == window));

            if (shouldDrawPreview)
            {
                int2 endTexel = pixelCoord;


                int2 startTexel = pixelCoord;
                if (e.modifiers == EventModifiers.Shift)
                {
                    startTexel = m_PreviousMousePixel;
                }

                if (m_IsResizingBrush)
                {
                    startTexel = m_BrushResizeMouseStart;
                    endTexel = m_BrushResizeMouseStart;
                }

                ProtoSpriteData.DrawPreviewGPU(sprite, startTexel, endTexel, BrushSize, Color.clear, BrushShape);

                m_IsPreviewDrawn = true;
            }
        }
    }
}