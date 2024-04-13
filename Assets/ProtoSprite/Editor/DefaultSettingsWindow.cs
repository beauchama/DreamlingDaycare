using UnityEditor;
using UnityEngine;

namespace ProtoSprite.Editor
{
    public class DefaultSettingsWindow : EditorWindow
    {
        bool m_InitializedPosition = false;

        public static Vector2Int DefaultTextureSize
        {
            get
            {
                int x = EditorPrefs.GetInt("ProtoSprite.Editor.DefaultSettingsWindow.DefaultTextureSize.x", 100);
                int y = EditorPrefs.GetInt("ProtoSprite.Editor.DefaultSettingsWindow.DefaultTextureSize.y", 100);
                return new Vector2Int(x, y);
            }
            set
            {
                EditorPrefs.SetInt("ProtoSprite.Editor.DefaultSettingsWindow.DefaultTextureSize.x", value.x);
                EditorPrefs.SetInt("ProtoSprite.Editor.DefaultSettingsWindow.DefaultTextureSize.y", value.y);
            }
        }

        public static float DefaultPPU
        {
            get
            {
                return EditorPrefs.GetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPPU", 100.0f);
            }
            set
            {
                EditorPrefs.SetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPPU", value);
            }
        }

        public static Vector2 DefaultPivot
        {
            get
            {
                var x = EditorPrefs.GetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPivot.x", 0.5f);
                var y = EditorPrefs.GetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPivot.y", 0.5f);
                return new Vector2(x, y);
            }
            set
            {
                EditorPrefs.SetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPivot.x", value.x);
                EditorPrefs.SetFloat("ProtoSprite.Editor.DefaultSettingsWindow.DefaultPivot.y", value.y);
            }
        }


        public static void Open()
        {
            var window = GetWindow<DefaultSettingsWindow>(true, "Default Settings");
            window.m_InitializedPosition = false;

            window.minSize = new Vector2(250, 130);
            window.maxSize = new Vector2(250, 130);
        }

        private void OnLostFocus()
        {
            Close();
        }

        void OnGUI()
        {
            if (!m_InitializedPosition)
            {
                m_InitializedPosition = true;
                Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                position = new Rect(mousePos.x, mousePos.y, position.width, position.height);
            }

            DefaultTextureSize = EditorGUILayout.Vector2IntField("Size", DefaultTextureSize);
            DefaultTextureSize = new Vector2Int(Mathf.Clamp(DefaultTextureSize.x, 1, ProtoSpriteWindow.kMaxTextureSize), Mathf.Clamp(DefaultTextureSize.y, 1, ProtoSpriteWindow.kMaxTextureSize));

            DefaultPivot = EditorGUILayout.Vector2Field("Pivot", DefaultPivot);
            DefaultPPU = Mathf.Max(0.001f, EditorGUILayout.FloatField("PPU", DefaultPPU));
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}