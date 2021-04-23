using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LDtk {

[System.Serializable]
public class Defs {
	public Layer[] layers;
	//entities not implemented;
	public Tileset[] tilesets;
	//levelFields not implemented
	//enums not implemented;
	//externalEnums not implemented;
}

[System.Serializable]
public class Tileset {
	public string identifier;
	public int uid;
	public string relPath;
	public int pxWid;
	public int pxHei;
	public int tileGridSize;
	public int spacing;
	public int padding;
	// savedSelections not implemented

	/*
	Imports the tileset as a Multi-Sprite
	Returns a ImportedTileset that contains identifier, uid and path to the sprite(s)
	*/
	public ImportedTileset import (string jsonDir, int pixelsPerUnit, string importDir) {
		string spriteSheetPathExternal = jsonDir + "/" + relPath;

		//Check, if spriteSheetPathExternal exists
		if (!System.IO.File.Exists(spriteSheetPathExternal)) {
			// try using relPath as full path
			spriteSheetPathExternal = relPath;
			// check again
			if (!System.IO.File.Exists(spriteSheetPathExternal)) {
				spriteSheetPathExternal = EditorUtility.OpenFilePanelWithFilters("Spritesheet \"" + spriteSheetPathExternal + "\" not found. Please locate.", jsonDir, new string[] {"Image file", "png"});
				if (!System.IO.File.Exists(spriteSheetPathExternal)) {
					Debug.LogError("Unable to locate \"" + spriteSheetPathExternal + "\".");
					return null;
				}
			}
		}

		string spriteSheetPathImportedInternal = "Assets/" + importDir + "/" + identifier + Path.GetExtension(spriteSheetPathExternal);
		
		//Import tileset texture
		if (!AssetTools.importExternalAsset(spriteSheetPathExternal, importDir, identifier, true, true)) {
			Debug.LogError("Could not load in external spritesheet \"" + spriteSheetPathExternal + "\"");
			return null;
		}

		if (!checkImportedTexture(spriteSheetPathImportedInternal)) {
			Debug.LogError("Loaded spritesheet does not match json data.");
			return null;
		}

		// Set texture properties
		TextureImporter imp = (TextureImporter) TextureImporter.GetAtPath(spriteSheetPathImportedInternal);
		imp.textureType = TextureImporterType.Sprite;
		imp.spriteImportMode = SpriteImportMode.Multiple;
		imp.spritePixelsPerUnit = pixelsPerUnit;
		imp.spriteBorder = new Vector4(1, 1, 1, 1);
		imp.sRGBTexture = true;
		imp.filterMode = FilterMode.Point;
		imp.textureCompression = TextureImporterCompression.Uncompressed;
		imp.mipmapEnabled = false;
		imp.streamingMipmaps = false;

		imp.spritesheet = createTilesetMetaData();

		// Reimport texture
		imp.SaveAndReimport();
		AssetDatabase.ImportAsset(spriteSheetPathImportedInternal, ImportAssetOptions.ForceUpdate);

		//Tiles get created when getTile in class Importedtileset is called
		string tilesFolderName = identifier + "_tiles";
		string tilesAssetDirInternal = importDir + "/" + tilesFolderName;

		return new ImportedTileset(identifier, uid, tilesAssetDirInternal, imp.spritesheet.Length - 1, spriteSheetPathImportedInternal);
	}

	public SpriteMetaData[] createTilesetMetaData() {
		List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();
		int i = 0;
		for (int y = padding; y <= (pxHei - tileGridSize - padding); y += (tileGridSize + spacing)) {
			for (int x = padding; x <= (pxWid - tileGridSize - padding); x += (tileGridSize + spacing)) {
				SpriteMetaData metaData = new SpriteMetaData();
				metaData.alignment = 0;
				metaData.border = new Vector4(1, 1, 1, 1);
				metaData.name = getTileIdentifier(i, identifier);
				metaData.pivot = new Vector2(0.5f, 0.5f);
				metaData.rect = new Rect(x, pxHei - y - tileGridSize, tileGridSize, tileGridSize); //fuck unity's inverted y axis
				metaDataList.Add(metaData);

				++i;
			}
		}

		return metaDataList.ToArray();
	}

	public static void createTileAsset(Sprite sprite, string dir) {
		Tile tile = Tile.CreateInstance<Tile>();
		tile.name = sprite.name;
		tile.sprite = sprite;
		tile.transform = Matrix4x4.identity;
		AssetTools.createAsset(tile, dir + "/" + tile.name + ".asset", false, true);
	}

	private bool checkImportedTexture(string path) {
		Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		if (!tex) {
			return false;
		}
		bool okWidth = tex.width == pxWid;
		bool okHeight = tex.height == pxHei;
		return okHeight && okWidth;
	}

	public static string getTileIdentifier(int tileId, string tilesetIdentifier) {
		return tilesetIdentifier + "_" + tileId;
	}
}

[System.Serializable]
public class Layer {
	public string identifier;
	public string type;
	public int uid;
	public int autoTilesetDefUid;

	//rest not implemented
}

/*
Contains identifier, uid and path to the sprite(s)
*/
public class ImportedTileset {
	public string identifier;
	public int uid;
	public string tileAssetDir;
	public int maxTileId;
	private List<Sprite> sprites;

	public ImportedTileset(string identifier, int uid, string tileAssetDir, int maxTileId, string spriteSheetPathImportedInternal) {
		this.identifier = identifier;
		this.uid = uid;
		this.tileAssetDir = tileAssetDir;
		this.maxTileId = maxTileId;

		Object[] spriteObjs = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPathImportedInternal);
		sprites = new List<Sprite>(spriteObjs.Length - 1);
		if (spriteObjs == null) {
			Debug.LogError("Spritesheet at \"" + spriteSheetPathImportedInternal + "\" could not be loaded.");
		} else {
			for (int i = 0; i < spriteObjs.Length; ++i) {
				try {
					Sprite sprite = (Sprite) spriteObjs[i];
					if (sprite != null) {
						sprites.Add(sprite);
					}
				} catch (System.InvalidCastException) {}
			}
			if (sprites.Count != maxTileId + 1) {
				Debug.LogError("Loaded sprite count does not match spritesheet.");
			}
		}
	}

	public bool getTile(int tileId, ref Tile tile) {
		if (!(tileId >= 0 && tileId <= maxTileId && tileId <= sprites.Count)) {
			Debug.LogError("Too high tileId " + tileId + " requested in ImportedTileset \"" + identifier + "\".");
			return false;
		}
		string assetpath = "Assets/" + tileAssetDir + "/" + Tileset.getTileIdentifier(tileId, identifier) + ".asset";
		tile = AssetDatabase.LoadAssetAtPath<Tile>(assetpath);
		if (tile == null) {
			// Create the tile
			if (!createTileAsset(tileId)) {
				Debug.LogError("Tile asset \"" + assetpath + "\" could not be created.");
				return false;
			}
			tile = AssetDatabase.LoadAssetAtPath<Tile>(assetpath);
			if (tile == null) {
				Debug.LogError("Tile asset \"" + assetpath + "\" could not be loaded." );
				return false;
			}
		}
		return true;
	}

	private bool createTileAsset(int tileId) {
		Sprite sprite = getSpriteByIdentifier(tileId);
		if (sprite == null) {
			Debug.LogError("Sprite for tileId " + tileId + " was not loaded.");
			return false;
		}
		Tile tile = Tile.CreateInstance<Tile>();
		tile.name = sprite.name;
		tile.sprite = sprite;
		tile.transform = Matrix4x4.identity;
		return AssetTools.createAsset(tile, tileAssetDir + "/" + tile.name + ".asset", true, true);
	}

	private Sprite getSpriteByIdentifier(int tileId) {
		string spriteIdentifier = Tileset.getTileIdentifier(tileId, identifier);
		if (sprites[tileId].name == spriteIdentifier) {
			return sprites[tileId];
		}
		for (int i = 0; i < sprites.Count; ++i) {
			if (sprites[i].name == spriteIdentifier) {
				return sprites[i];
			}
		}
		return null;
	}
}


}
