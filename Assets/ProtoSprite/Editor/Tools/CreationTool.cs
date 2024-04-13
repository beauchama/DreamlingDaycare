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
    public class CreationTool : ProtoSpriteTool
    {
        bool m_SceneSelectionOutlineGizmoEnabled = false;

        bool m_IsSelecting = false;

        Vector2 m_StartMousePos = Vector2.zero;

        static int s_ColorPickID = "EyeDropper".GetHashCode();

        float m_PixelsPerUnit = 16;

        bool m_SpawnAsChildOfTarget = false;

        public Color PaintColor
        {
            get
            {
                return ProtoSpriteWindow.GetInstance().GetToolInstance<PaintTool>().PaintColor;
            }
            set
            {
                ProtoSpriteWindow.GetInstance().GetToolInstance<PaintTool>().PaintColor = value;
            }
        }

        public float PixelsPerUnit
        {
            get => m_PixelsPerUnit;
            set
            {
                value = Mathf.Clamp(value, 0.001f, ProtoSpriteWindow.kMaxTextureSize);
                m_PixelsPerUnit = value;
            }
        }

        public bool SpawnAsChildOfTarget
        {
            get => m_SpawnAsChildOfTarget;
            set
            {
                m_SpawnAsChildOfTarget = value;
            }
        }

        public override GUIContent toolbarIcon
        {
            get
            {
                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("Toolbar Plus"));
                content.tooltip = "Create (" + ShortcutManager.instance.GetShortcutBinding("ProtoSprite/Creation Tool") + ")";
                return content;
            }
        }

        public void OnEnable()
        {
            PixelsPerUnit = EditorPrefs.GetFloat("ProtoSprite.Editor.CreationTool.PixelsPerUnit", 16);
            SpawnAsChildOfTarget = EditorPrefs.GetInt("ProtoSprite.Editor.CreationTool.SpawnAsChildOfTarget", 0) == 0? false : true;
        }

		public void OnDisable()
		{
            // If the tool is active and we exit play mode then OnDisable is called but OnWillBeDeactivated isn't so we force call it here
            if (ToolManager.activeToolType == GetType())
                OnWillBeDeactivated();

            EditorPrefs.SetFloat("ProtoSprite.Editor.CreationTool.PixelsPerUnit", PixelsPerUnit);
            EditorPrefs.SetInt("ProtoSprite.Editor.CreationTool.SpawnAsChildOfTarget", SpawnAsChildOfTarget? 1 : 0);
        }

        [Shortcut("ProtoSprite/Creation Tool", typeof(InternalEngineBridge.ShortcutContext), ProtoSpriteWindow.kToolShortcutsTag, KeyCode.U)]
        public static void ToggleTool()
        {
            ProtoSpriteWindow.ToggleTool<CreationTool>();
        }

		public override void ProtoSpriteWindowGUI()
        {
            base.ProtoSpriteWindowGUI();

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

            {
                GUIContent content = new GUIContent("Pixels Per Unit");
                content.tooltip = "How many pixels in the sprite correspond to one unit in the world.";
                PixelsPerUnit = EditorGUILayout.FloatField(content, PixelsPerUnit);
            }

            {
                GUIContent content = new GUIContent("As Child");
                content.tooltip = "Instantiate the new GameObject as a child of the currently selected Transform.";
                SpawnAsChildOfTarget = EditorGUILayout.Toggle(content, SpawnAsChildOfTarget);
            }

            // Palette
            EditorGUILayout.Space(10);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            ProtoSpriteWindow.GetInstance().ColorPalette.OnGUI();
        }

        public override void OnActivated()
        {
            m_IsSelecting = false;
            m_SceneSelectionOutlineGizmoEnabled = ProtoSpriteData.SceneSelectionGizmo;
            ProtoSpriteData.SceneSelectionGizmo = false;
        }

        public override void OnWillBeDeactivated()
        {
            ProtoSpriteData.SceneSelectionGizmo = m_SceneSelectionOutlineGizmoEnabled;

            m_IsSelecting = false;
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

                Handles.color = new Color(1, 1, 1, 0.1f);
                Handles.DrawWireCube(scale * 0.5f - spritePivot / sprite.pixelsPerUnit, scale);
                Handles.matrix = tempMatrix;
            }
        }

        Vector3 GetWorldPos(Vector2 mousePos)
        {
            Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePos);

            Transform targetT = Selection.activeTransform;

            Vector3 planeForward = Vector3.forward;
            Vector3 planePosition = Vector3.zero;

            if (targetT != null)
            {
                planeForward = targetT.forward;
                planePosition = targetT.position;
            }

            Plane plane = new Plane(planeForward, planePosition);
            plane.Raycast(worldRay, out float distance);
            Vector3 intersectPos = worldRay.origin + worldRay.direction * distance;

            

            if (targetT != null)
            {
                intersectPos = targetT.InverseTransformPoint(intersectPos);
            }

            Vector3 spawnPosition = intersectPos;
            spawnPosition.z = 0.0f;

            return spawnPosition;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            Event e = Event.current;

            if (!(window is SceneView))
                return;

            ProtoSpriteData.RepaintSceneViewsIfUnityFocused();
            ProtoSpriteData.RepaintSpriteEditorWindow();


            Transform targetT = Selection.activeTransform;

            bool isColorPickerOpened = ProtoSpriteData.IsColorPickerOpen();

            if (targetT != null && targetT.GetComponent<SpriteRenderer>() != null)
            {
                DrawHandles(targetT);
            }

            int passiveControl = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveControl);

            if (Event.current.GetTypeForControl(passiveControl) == EventType.MouseDown && e.button == 0)
            {
                GUIUtility.hotControl = passiveControl;
            }

            if (e.rawType == EventType.MouseDown && e.button == 0 && !isColorPickerOpened)
            {
                m_IsSelecting = true;
                m_StartMousePos = e.mousePosition;
            }

            if (e.rawType == EventType.MouseUp && e.button == 0 && m_IsSelecting)
            {
                m_IsSelecting = false;

                float one_over_ppu = 1.0f / PixelsPerUnit;

                Vector2 worldPosStart = GetWorldPos(m_StartMousePos);

                worldPosStart.x = Mathf.Floor(worldPosStart.x / one_over_ppu) * one_over_ppu;
                worldPosStart.y = Mathf.Floor(worldPosStart.y / one_over_ppu) * one_over_ppu;

                Vector2 worldPosEnd = GetWorldPos(e.mousePosition);

                worldPosEnd.x = Mathf.Floor(worldPosEnd.x / one_over_ppu) * one_over_ppu;
                worldPosEnd.y = Mathf.Floor(worldPosEnd.y / one_over_ppu) * one_over_ppu;

                Rect rect = new Rect();
                rect.min = math.min((float2)worldPosStart, (float2)worldPosEnd);
                rect.max = math.max((float2)worldPosStart, (float2)worldPosEnd);
                rect.max += Vector2.one * one_over_ppu;

                if (targetT != null)
                {
                    Handles.matrix = targetT.localToWorldMatrix;
                }
                else
                {
                    Handles.matrix = Matrix4x4.identity;
                }

                Handles.color = Color.white;
                Handles.DrawWireCube(rect.center, rect.size);


                Vector2Int textureSize = new Vector2Int(Mathf.RoundToInt(rect.width / one_over_ppu), Mathf.RoundToInt(rect.height / one_over_ppu));

                textureSize.x = Mathf.Clamp(textureSize.x, 1, ProtoSpriteWindow.kMaxTextureSize);
                textureSize.y = Mathf.Clamp(textureSize.y, 1, ProtoSpriteWindow.kMaxTextureSize);

                if (worldPosEnd.x > worldPosStart.x)
                {
                    rect.width = textureSize.x * one_over_ppu;
                }
                else
                {
                    rect.xMin = rect.xMax - textureSize.x * one_over_ppu;
                }

                if (worldPosEnd.y > worldPosStart.y)
                {
                    rect.height = textureSize.y * one_over_ppu;
                }
                else
                {
                    rect.yMin = rect.yMax - textureSize.y * one_over_ppu;
                }


                Vector2 pivot = new Vector2(Mathf.Floor(textureSize.x * 0.5f) / textureSize.x, Mathf.Floor(textureSize.y * 0.5f) / textureSize.y);

                Vector2 spawnPos = new Vector2(Mathf.Floor(rect.center.x / one_over_ppu) * one_over_ppu, Mathf.Floor(rect.center.y / one_over_ppu) * one_over_ppu);

                GameObject go = ProtoSpriteWindow.CreateProtoSprite(PixelsPerUnit, pivot, textureSize, spawnPos, PaintColor);

                if (go != null)
                {
                    if (SpawnAsChildOfTarget)
                    {
                        go.transform.SetParent(targetT);
                        go.transform.localPosition = spawnPos;
                        go.transform.localRotation = quaternion.identity;
                        go.transform.localScale = Vector3.one;
                    }
                    else if (targetT != null)
                    {
                        go.transform.SetParent(null);
                        go.transform.localPosition = targetT.TransformPoint(spawnPos);
                        go.transform.localRotation = targetT.rotation;
                        go.transform.localScale = targetT.lossyScale;
                    }
                }
            }

            if (m_IsSelecting)
            {
                float one_over_ppu = 1.0f / PixelsPerUnit;

                Vector2 worldPosStart = GetWorldPos(m_StartMousePos);

                worldPosStart.x = Mathf.Floor(worldPosStart.x / one_over_ppu) * one_over_ppu;
                worldPosStart.y = Mathf.Floor(worldPosStart.y / one_over_ppu) * one_over_ppu;

                Vector2 worldPosEnd = GetWorldPos(e.mousePosition);

                worldPosEnd.x = Mathf.Floor(worldPosEnd.x / one_over_ppu) * one_over_ppu;
                worldPosEnd.y = Mathf.Floor(worldPosEnd.y / one_over_ppu) * one_over_ppu;

                

                Rect rect = new Rect();
                rect.min = math.min((float2)worldPosStart, (float2)worldPosEnd);
                rect.max = math.max((float2)worldPosStart, (float2)worldPosEnd);
                rect.max += Vector2.one * one_over_ppu;

                if (targetT != null)
                {
                    Handles.matrix = targetT.localToWorldMatrix;
                }
                else
                {
                    Handles.matrix = Matrix4x4.identity;
                }

                Handles.color = Color.red;
                Handles.DrawWireCube(rect.center, rect.size);

                Vector2Int textureSize = new Vector2Int(Mathf.RoundToInt(rect.width / one_over_ppu), Mathf.RoundToInt(rect.height / one_over_ppu));

                textureSize.x = Mathf.Clamp(textureSize.x, 1, ProtoSpriteWindow.kMaxTextureSize);
                textureSize.y = Mathf.Clamp(textureSize.y, 1, ProtoSpriteWindow.kMaxTextureSize);

                if (worldPosEnd.x > worldPosStart.x)
                {
                    rect.width = textureSize.x * one_over_ppu;
                }
                else
                {
                    rect.xMin = rect.xMax - textureSize.x * one_over_ppu;
                }

                if (worldPosEnd.y > worldPosStart.y)
                {
                    rect.height = textureSize.y * one_over_ppu;
                }
                else
                {
                    rect.yMin = rect.yMax - textureSize.y * one_over_ppu;
                }


                

                Handles.color = Color.white;
                Handles.DrawWireCube(rect.center, rect.size);
            }
            else
            {
                float one_over_ppu = 1.0f / PixelsPerUnit;

                Vector2 worldPosStart = GetWorldPos(e.mousePosition);

                worldPosStart.x = Mathf.Floor(worldPosStart.x / one_over_ppu) * one_over_ppu;
                worldPosStart.y = Mathf.Floor(worldPosStart.y / one_over_ppu) * one_over_ppu;

                Vector2 worldPosEnd = GetWorldPos(e.mousePosition);

                worldPosEnd.x = Mathf.Floor(worldPosEnd.x / one_over_ppu) * one_over_ppu;
                worldPosEnd.y = Mathf.Floor(worldPosEnd.y / one_over_ppu) * one_over_ppu;

                

                Rect rect = new Rect();
                rect.min = math.min((float2)worldPosStart, (float2)worldPosEnd);
                rect.max = math.max((float2)worldPosStart, (float2)worldPosEnd);
                rect.max += Vector2.one * one_over_ppu;

                if (targetT != null)
                {
                    Handles.matrix = targetT.localToWorldMatrix;
                }
                else
                {
                    Handles.matrix = Matrix4x4.identity;
                }

                Handles.color = Color.white;
                Handles.DrawWireCube(rect.center, rect.size);
            }
        }
    }
}