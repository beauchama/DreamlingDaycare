using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

namespace ProtoSprite.Editor
{
	[System.Serializable]
	public class UndoDataBase
	{
        public int group = -1;
        [SerializeField] public Texture2D texture = null;

        public virtual void DoUndo() { }

        public virtual void DoRedo() { }

        public virtual long TotalBytes()
        {
            return 0;
        }
	}

    [System.Serializable]
    public class UndoCustomFunctions : UndoDataBase
    {
        public delegate void MyDelegate();
        public MyDelegate undoDelegate;
        public MyDelegate redoDelegate;

		public override void DoUndo()
		{
			base.DoUndo();

            if (undoDelegate == null)
                return;

            undoDelegate.Invoke();
		}

		public override void DoRedo()
		{
			base.DoRedo();

            if (redoDelegate == null)
                return;

            redoDelegate.Invoke();
		}
	}


	[System.Serializable]
	public class UndoDataPaint : UndoDataBase
	{
        [SerializeField] public Color32[] pixelDataBefore = null;
        [SerializeField] public Color32[] pixelDataAfter = null;

        public override void DoUndo()
        {
            var undoCopy = pixelDataBefore;

            texture.SetPixelData(undoCopy, 0);
            texture.Apply(true, false);

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(texture);
            ProtoSpriteData.SubmitSaveData(saveData);
        }

        public override void DoRedo()
        {
            var undoCopy = pixelDataAfter;

            texture.SetPixelData(undoCopy, 0);
            texture.Apply(true, false);

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(texture);
            ProtoSpriteData.SubmitSaveData(saveData);
        }

		public override long TotalBytes()
		{
            long total = 0;

            if (pixelDataBefore != null)
            {
                total += pixelDataBefore.Length * 4;
            }

            if (pixelDataAfter != null)
            {
                total += pixelDataAfter.Length * 4;
            }

            return total;
		}
	}

    [System.Serializable]
    public class UndoDataResize : UndoDataBase
    {
        [SerializeField] public Color32[] pixelDataBefore = null;
        [SerializeField] public Vector2Int textureSizeBefore = Vector2Int.zero;
        [SerializeField] public Vector2 spritePivotNormalizedBefore = Vector2.zero;

        [SerializeField] public Color32[] pixelDataAfter = null;
        [SerializeField] public Vector2Int textureSizeAfter = Vector2Int.zero;
        [SerializeField] public Vector2 spritePivotNormalizedAfter = Vector2.zero;

        public override void DoUndo()
        {
            texture.Reinitialize(textureSizeBefore.x, textureSizeBefore.y);
            texture.SetPixelData(pixelDataBefore, 0);
            texture.Apply(false, false);

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(texture);
            ProtoSpriteData.SubmitSaveData(saveData);

            ProtoSpriteData.Saving.SaveTextureIfDirty(texture, false);

            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);
            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importerSettings.spritePivot = spritePivotNormalizedBefore;
            textureImporter.SetTextureSettings(importerSettings);

            textureImporter.SaveAndReimport();
        }

        public override void DoRedo()
        {
            texture.Reinitialize(textureSizeAfter.x, textureSizeAfter.y);
            texture.SetPixelData(pixelDataAfter, 0);
            texture.Apply(false, false);

            ProtoSpriteData.SaveData saveData = new ProtoSpriteData.SaveData(texture);
            ProtoSpriteData.SubmitSaveData(saveData);

            ProtoSpriteData.Saving.SaveTextureIfDirty(texture, false);

            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(importerSettings);
            importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importerSettings.spritePivot = spritePivotNormalizedAfter;
            textureImporter.SetTextureSettings(importerSettings);

            textureImporter.SaveAndReimport();
        }

        public override long TotalBytes()
        {
            long total = 0;

            if (pixelDataBefore != null)
            {
                total += pixelDataBefore.Length * 4;
            }

            if (pixelDataAfter != null)
            {
                total += pixelDataAfter.Length * 4;
            }

            return total;
        }
    }

    public class UndoDataPivot : UndoDataBase
    {
        [SerializeField] public string spriteName = "";
        [SerializeField] public Vector2 spritePivotNormalizedBefore = Vector2.zero;
        [SerializeField] public Vector2 spritePivotNormalizedAfter = Vector2.zero;

        public override void DoUndo()
        {
            ProtoSpriteData.Saving.SaveTextureIfDirty(texture, false);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = dataProvider.GetSpriteRects();

            foreach (var rect in spriteRects)
            {
                if (rect.name == spriteName)
                {
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = spritePivotNormalizedBefore;
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
        }

        public override void DoRedo()
        {
            ProtoSpriteData.Saving.SaveTextureIfDirty(texture, false);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = dataProvider.GetSpriteRects();

            foreach (var rect in spriteRects)
            {
                if (rect.name == spriteName)
                {
                    rect.alignment = SpriteAlignment.Custom;
                    rect.pivot = spritePivotNormalizedAfter;
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
        }

        public override long TotalBytes()
        {
            long total = 0;

            return total;
        }
    }
}