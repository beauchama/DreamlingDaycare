using UnityEditor;
using UnityEngine;
using UnityEditor.Sprites;
using Unity.Mathematics;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;
using System.Collections.Generic;
using UnityEngine.U2D;

namespace ProtoSprite.Editor
{
    public class PaintTool : ProtoSpriteTool
    {
        Texture2D m_TargetTexture = null;

        int2 m_BrushResizeMousePrevious = int2.zero;
        int2 m_BrushResizeMouseStart = int2.zero;
        Vector2 m_BrushResizeMouseScreenPrevious = Vector2.zero;
        bool m_IsResizingBrush = false;

        bool m_SceneSelectionOutlineGizmoEnabled = false;

        Color m_PaintColor = Color.white;
        int m_BrushSize = 25;

        bool m_IsPreviewDrawn = false;
        bool m_IsDirty = false;

        bool m_TryingToPaint = false;

        UndoDataPaint m_UndoData = null;

        int2 m_PreviousMousePixel = int2.zero;

        EditorWindow m_PaintStartWindow = null;

        BrushShape m_BrushShape = BrushShape.CIRCLE;

        bool m_PixelPerfect = false;

        static int s_ColorPickID = "EyeDropper".GetHashCode();

        List<int2> m_PreviousPixelCoords = new List<int2>();
        Color32 m_PreviousPaintedPixelColor = Color.clear;

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("Grid.PaintTool"));
                content.tooltip = "Paint (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Paint Tool") + ")";
                return content;
            }
        }

		public Color PaintColor { get => m_PaintColor; set => m_PaintColor = value; }
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
        public bool PixelPerfect { get => m_PixelPerfect; set => m_PixelPerfect = value; }

        public void OnEnable()
        {
            PaintColor = ProtoSpriteData.EditorPrefs_GetColor("ProtoSprite.Editor.PaintTool.PaintColor", Color.white);
            BrushSize = EditorPrefs.GetInt("ProtoSprite.Editor.PaintTool.BrushSize", 25);
            BrushShape = (BrushShape)EditorPrefs.GetInt("ProtoSprite.Editor.PaintTool.BrushShape", (int)BrushShape.CIRCLE);
            PixelPerfect = (BrushShape)EditorPrefs.GetInt("ProtoSprite.Editor.PaintTool.PixelPerfect", 0) == 0? false : true;
        }

		public void OnDisable()
		{
            // If the tool is active and we exit play mode then OnDisable is called but OnWillBeDeactivated isn't so we force call it here
            if (ToolManager.activeToolType == GetType())
                OnWillBeDeactivated();

            ProtoSpriteData.EditorPrefs_SetColor("ProtoSprite.Editor.PaintTool.PaintColor", PaintColor);
            EditorPrefs.SetInt("ProtoSprite.Editor.PaintTool.BrushSize", BrushSize);
            EditorPrefs.SetInt("ProtoSprite.Editor.PaintTool.BrushShape", (int)BrushShape);
            EditorPrefs.SetInt("ProtoSprite.Editor.PaintTool.PixelPerfect", PixelPerfect? 1 : 0);
        }

        [Shortcut("ProtoSprite/Paint Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.B)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<PaintTool>();
        }

		public override void ProtoSpriteWindowGUI()
        {
            if (ProtoSpriteData.IsColorPickerOpen() && ProtoSpriteData.GetEyeDropperColorPickID() == s_ColorPickID)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUILayout.ColorField(ProtoSpriteData.GetEyeDropperPickedColor());
                }
                else
                {
                    EditorGUILayout.ColorField(PaintColor);
                }
            }
            else
            {
                PaintColor = EditorGUILayout.ColorField(PaintColor);
            }

            BrushSize = EditorGUILayout.IntField("Brush Size", BrushSize);

            BrushShape = (BrushShape)EditorGUILayout.Popup("Brush Shape", (int)BrushShape, new string[] { "Circle", "Square" });

            {
                GUIContent label = new GUIContent("Pixel Perfect");
                label.tooltip = "Dynamically adjusts painted pixels to remove L-shapes. Only usable when brush size is 1 pixel.";
                PixelPerfect = EditorGUILayout.Toggle(label, PixelPerfect);
            }

            // Palette
            EditorGUILayout.Space(10);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            ProtoSpriteWindow.GetInstance().ColorPalette.OnGUI();
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

        public static void DrawDebugSprite(SpriteRenderer spriteRenderer, SceneView sceneView)
        {
            Sprite sprite = spriteRenderer.sprite;
            Transform t = spriteRenderer.transform;

            Rect spriteRect = sprite.rect;
            //Vector2 brushPreviewSize = Vector2.one * brushSize;

            Vector2 scale = (new Vector2(spriteRect.width, spriteRect.height) / sprite.pixelsPerUnit);

            Vector2 spritePivot = sprite.pivot;
            //Vector2 pixelOffset = new Vector2(pixelCoord.x, pixelCoord.y) - sprite.rect.position;


            if (spriteRenderer.flipX)
            {
                spritePivot.x = spriteRect.width - spritePivot.x;
                //pixelOffset.x = spriteRect.width - pixelOffset.x;
                //pixelOffset.x -= Mathf.CeilToInt(brushSize * 0.5f);
            }
            else
            {
                //pixelOffset.x -= Mathf.FloorToInt(brushSize * 0.5f);
            }

            if (spriteRenderer.flipY)
            {
                spritePivot.y = spriteRect.height - spritePivot.y;
                //pixelOffset.y = spriteRect.height - pixelOffset.y;
                //pixelOffset.y -= Mathf.CeilToInt(brushSize * 0.5f);
            }
            else
            {
                //pixelOffset.y -= Mathf.FloorToInt(brushSize * 0.5f);
            }



            Vector2 meshQuadSize = sprite.rect.size / sprite.pixelsPerUnit;

            Rect uvRect = sprite.rect;
            uvRect.xMin /= sprite.texture.width;
            uvRect.xMax /= sprite.texture.width;
            uvRect.yMin /= sprite.texture.height;
            uvRect.yMax /= sprite.texture.height;

            Mesh mesh = ProtoSpriteData.GetQuadMesh(meshQuadSize.x, meshQuadSize.y, uvRect, spriteRenderer.flipX, spriteRenderer.flipY);

            var activeRenderTarget = RenderTexture.active;


            var spriteVertexPositions = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);

            var spriteVerticesCenter = Vector3.zero;

            foreach (var pos in spriteVertexPositions)
            {
                spriteVerticesCenter += pos;
            }

            spriteVerticesCenter /= 4.0f;

            //RenderTexture tempTex = RenderTexture.GetTemporary(brushSize, brushSize);
            //tempTex.filterMode = FilterMode.Point;
            //tempTex.Create();

            //Graphics.SetRenderTarget(tempTex);
            //GL.Clear(true, true, Color.clear);

            //int2 drawPixel = new int2(Mathf.FloorToInt(tempTex.width * 0.5f), Mathf.FloorToInt(tempTex.height * 0.5f));
            //ProtoSpriteData.DrawLineGPU(tempTex, new Rect(0, 0, tempTex.width, tempTex.height), drawPixel, drawPixel, brushSize, Color.white, brushShape);

            RenderTexture tempRT = RenderTexture.GetTemporary(sceneView.camera.pixelWidth, sceneView.camera.pixelHeight);

            Graphics.SetRenderTarget(tempRT);
            GL.Clear(true, true, Color.black);

            Shader theShader = Shader.Find("Hidden/ProtoSprite/Outline");
            Material tempMat = new Material(theShader);

            tempMat.SetTexture("_MainTex", sprite.texture);
            tempMat.SetVector("_SpriteRect", new Vector4(0, 0, sprite.rect.width, sprite.rect.height));
            tempMat.SetPass(2);

            if (spriteRenderer.flipY)
            {
                spriteVerticesCenter.y = -spriteVerticesCenter.y;
            }

            if (spriteRenderer.flipX)
            {
                spriteVerticesCenter.x = -spriteVerticesCenter.x;
            }

            //Debug.Log(spriteVerticesCenter);


            Matrix4x4 matrix = Matrix4x4.TRS(t.TransformPoint((Vector2)spriteVerticesCenter), t.rotation, t.lossyScale);

            //Graphics.SetRenderTarget(activeRenderTarget);

            //GL.sRGBWrite = false;
            GL.Clear(true, true, Color.clear);
            Graphics.DrawMeshNow(mesh, matrix);

            //Graphics.Blit(tempRT, tempRT2, tempMat, 0);
            Graphics.SetRenderTarget(activeRenderTarget);

            Handles.BeginGUI();
            GUI.DrawTexture(new Rect(0, 0, tempRT.width, tempRT.height), tempRT);
            Handles.EndGUI();

            //RenderTexture.ReleaseTemporary(tempTex);
            RenderTexture.ReleaseTemporary(tempRT);

            DestroyImmediate(mesh);
            DestroyImmediate(tempMat);
        }

        public static void DrawBrushOutline(SpriteRenderer spriteRenderer, int2 pixelCoord, int brushSize, BrushShape brushShape, SceneView sceneView)
        {
            Sprite sprite = spriteRenderer.sprite;
            Transform t = spriteRenderer.transform;

            Rect spriteRect = sprite.rect;
            Vector2 brushPreviewSize = Vector2.one * brushSize;

            Vector2 spritePivot = sprite.pivot;
            Vector2 pixelOffset = new Vector2(pixelCoord.x, pixelCoord.y) - sprite.rect.position;


            if (spriteRenderer.flipX)
            {
                spritePivot.x = spriteRect.width - spritePivot.x;
                pixelOffset.x = spriteRect.width - pixelOffset.x;
                pixelOffset.x -= Mathf.CeilToInt(brushSize * 0.5f);
            }
            else
            {
                pixelOffset.x -= Mathf.FloorToInt(brushSize * 0.5f);
            }

            if (spriteRenderer.flipY)
            {
                spritePivot.y = spriteRect.height - spritePivot.y;
                pixelOffset.y = spriteRect.height - pixelOffset.y;
                pixelOffset.y -= Mathf.CeilToInt(brushSize * 0.5f);
            }
            else
            {
                pixelOffset.y -= Mathf.FloorToInt(brushSize * 0.5f);
            }



            Vector2 meshQuadSize = brushPreviewSize / sprite.pixelsPerUnit;

            Mesh mesh = ProtoSpriteData.GetQuadMesh(meshQuadSize.x, meshQuadSize.y, new Rect(0, 0, 1, 1), false, false);

            var activeRenderTarget = RenderTexture.active;

            RenderTexture tempTex = RenderTexture.GetTemporary(brushSize, brushSize);
            tempTex.filterMode = FilterMode.Point;
            tempTex.Create();

            Graphics.SetRenderTarget(tempTex);
            GL.Clear(true, true, Color.clear);

            int2 drawPixel = new int2(Mathf.FloorToInt(tempTex.width * 0.5f), Mathf.FloorToInt(tempTex.height * 0.5f));
            ProtoSpriteData.DrawLineGPU(tempTex, new Rect(0, 0, tempTex.width, tempTex.height), drawPixel, drawPixel, brushSize, Color.white, brushShape);

            RenderTexture tempRT = RenderTexture.GetTemporary(sceneView.camera.pixelWidth, sceneView.camera.pixelHeight);
            RenderTexture tempRT2 = RenderTexture.GetTemporary(sceneView.camera.pixelWidth, sceneView.camera.pixelHeight);

            RenderTexture grabPassRT = RenderTexture.GetTemporary(sceneView.camera.pixelWidth, sceneView.camera.pixelHeight);
            grabPassRT.filterMode = FilterMode.Bilinear;
            grabPassRT.Create();

            Graphics.SetRenderTarget(grabPassRT);
            GL.Clear(true, true, Color.green);

            var sceneViewOriginalRT = sceneView.camera.targetTexture;
            var sceneCamEnabled = sceneView.camera.enabled;

            sceneView.camera.targetTexture = grabPassRT;
            if (sceneView.cameraMode != SceneView.GetBuiltinCameraMode(DrawCameraMode.Wireframe)) // Rendering artifacts occur if in wireframe draw mode and on URP or HDRP
                sceneView.camera.Render();

            if (ProtoSpriteWindow.GetInstance().DebugRender)
                DrawDebugSprite(spriteRenderer, sceneView);

            sceneView.camera.targetTexture = sceneViewOriginalRT;
            sceneView.camera.enabled = sceneCamEnabled;

            Graphics.SetRenderTarget(tempRT2);
            GL.Clear(true, true, Color.black);

            Graphics.SetRenderTarget(tempRT);
            GL.Clear(true, true, Color.black);

            Shader blurShader = Shader.Find("Hidden/ProtoSprite/Outline");
            Material tempMat = new Material(blurShader);

            tempMat.SetTexture("_MainTex", tempTex);
            tempMat.SetTexture("_GrabPass", grabPassRT);
            tempMat.SetPass(1);

            Matrix4x4 matrix = Matrix4x4.TRS(t.TransformPoint(-(Vector3)(spritePivot / sprite.pixelsPerUnit) + (Vector3)(pixelOffset / sprite.pixelsPerUnit) + (Vector3)(brushPreviewSize * 0.5f / sprite.pixelsPerUnit)), t.rotation, t.lossyScale);

            Graphics.DrawMeshNow(mesh, matrix);

            Graphics.Blit(tempRT, tempRT2, tempMat, 0);

            Graphics.SetRenderTarget(activeRenderTarget);

            Handles.BeginGUI();
            GUI.DrawTexture(new Rect(0, 0, tempRT.width, tempRT.height), tempRT2);
            Handles.EndGUI();

            RenderTexture.ReleaseTemporary(tempTex);
            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.ReleaseTemporary(tempRT2);
            RenderTexture.ReleaseTemporary(grabPassRT);

            DestroyImmediate(mesh);
            DestroyImmediate(tempMat);
        }

        void HandleEyeDropper()
        {
            Event e = Event.current;

            if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "EyeDropperClicked")
                {
                    m_PaintColor = ProtoSpriteData.GetEyeDropperLastPickedColor();
                }
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                ProtoSpriteData.EyeDropperStart();
                ProtoSpriteData.SetEyeDropperColorPickID(s_ColorPickID);
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            Event e = Event.current;

            HandleEyeDropper();

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

            // Draw brush outline, a bit distracting when painting so only show outline when resizing to help with seeing it
            if (m_IsResizingBrush && e.type == EventType.Repaint)
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
                        ProtoSpriteData.SubmitUndoData(m_UndoData, "ProtoSprite Paint Tool");
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

                        ProtoSpriteData.DrawLineSimultaneousCPUAndGPU(sprite, m_PreviousMousePixel, pixelCoord, BrushSize, PaintColor, BrushShape);

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

            if ((e.rawType == EventType.MouseDrag || e.rawType == EventType.MouseDown) && e.button == 0 && !isColorPickerOpened  && window == m_PaintStartWindow)
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

                ProtoSpriteData.DrawPreviewGPU(sprite, startTexel, endTexel, BrushSize, PaintColor, BrushShape);

                m_IsPreviewDrawn = true;
            }
        }
    }
}