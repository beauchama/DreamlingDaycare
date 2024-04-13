using ProtoSprite.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace ProtoSprite.Editor
{
    public class ProtoSpriteTool : EditorTool
    {
		public override GUIContent toolbarIcon
        {
            get
            {
                return EditorGUIUtility.IconContent("Grid.Default", "ProtoSpriteTool");
            }
        }

        public virtual void ProtoSpriteWindowGUI()
        {

        }

        public virtual bool IsToolCompatible(out string invalidReason)
        {
            invalidReason = "";
            return true;
        }
    }

}