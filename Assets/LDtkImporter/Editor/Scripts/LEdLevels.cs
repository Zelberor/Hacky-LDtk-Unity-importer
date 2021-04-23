﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace LEd {

[System.Serializable]
public class Level {
	public string identifier;
	public int uid;
	public int pxWid;
	public int pxHei;
	public LayerInstance[] layerInstances;

	public bool import(string importDir, int pixelsPerUnit, ImportedTileset[] tilesets, Layer[] layers) {
		GameObject baseObject = null;
		bool success = true;
		try {
			AssetDatabase.StartAssetEditing();
			baseObject = new GameObject(identifier + "_" + uid);
			int sortingOrderAddition = 1;
			for (int i = 0; i < layerInstances.Length; ++i) {
				if (!layerInstances[i].import(ref baseObject, pixelsPerUnit, tilesets, layers, ref sortingOrderAddition)) {
					Debug.LogError("Failed to import layerInstance \"" + layerInstances[i].__identifier + "\" in level \"" + identifier + "\".");
					success = false;
					break;
				}
				Debug.Log("Imported layerInstance \"" + layerInstances[i].__identifier + "\" in level \"" + identifier + "\" successfully.");
			}

			if (success) {
				string savePath = importDir + "/" + identifier + "_" + uid + ".prefab";
				success = AssetTools.createAsset(baseObject, savePath, false, true);
			}
		} finally {
			AssetDatabase.StopAssetEditing();
			if (baseObject != null) {
				Object.DestroyImmediate(baseObject);
			}
		}
		return success;
	}
}

[System.Serializable]
public class LayerInstance {
	public string __identifier;
	public string __type;
	public int __cWid; // layer width (grid based)
	public int __cHei; // layer height (grid based)
	public int __gridSize;
	public int levelId;
	public int layerDefUid = -1;
	public int pxOffsetX; // optional offset that could happen when resizing levels
	public int pxOffsetY;
	public int seed;
	public IntGridTile[] intGrid; // only populated if layer is an IntGrid
	public Autotile[] autoTiles; // only populated if layer is an Auto-layer
	public GridTile[] gridTiles; // only populated if layer is a TileLayer
	public EntityInstance[] entityInstances; // only populated if layer is an EntityLayer

	public bool import(ref GameObject baseObjectInOut, int pixelsPerUnit, ImportedTileset[] tilesets, Layer[] layers, ref int sortingOrderAddition) {
		if (__type == "IntGrid" || __type == "AutoLayer" || __type == "Tiles") {
			int tileSetIndex = getMatchingImportedTilesetIndex(tilesets, layers);
			if (tileSetIndex < 0) {
				Debug.LogWarning("Layerinstance \"" + __identifier + "\" has type \"" + __type + "\", but no matching tileset has been found. Layerinstance will be skipped.");
			} else {
				if (!importTiles(ref baseObjectInOut, tilesets[tileSetIndex], pixelsPerUnit, ref sortingOrderAddition)) {
					return false;
				}
			}
		} else if (__type == "Entities") {
			Debug.LogWarning("Layerinstance \"" + __identifier + "\" has type \"Entities\", but Entities are currently not supported. Layerinstance will be skipped.");
		} else {
			Debug.LogWarning("Layerinstance \"" + __identifier + "\" has unknown type \"" + __type + "\". Layerinstance will be skipped.");
		}
		return true;
	}

	public bool importTiles(ref GameObject baseObjectInOut, ImportedTileset tileset, int pixelsPerUnit, ref int sortingOrderAddition) {
		string baseName =  __type + "_" + __identifier;
		float gridOffsetX = ((float) pxOffsetX) / ((float) pixelsPerUnit);
		float gridOffsetY = ((float) pxOffsetY) / ((float) pixelsPerUnit);
		Vector3 gridOffset = new Vector3(gridOffsetX, gridOffsetY, 0);
		TilemapStackMaker tilemaps = new TilemapStackMaker(ref baseObjectInOut, baseName, pixelsPerUnit, sortingOrderAddition, gridOffset);

		//Import stuff into tilemap
		Vector2Int tileSize = new Vector2Int(__gridSize, __gridSize);
		for (int i = autoTiles.Length - 1; i >= 0 ; --i) {
			if (!autoTiles[i].import(ref tilemaps, tileset, tileSize)) {
				Debug.LogError("Failed to import autoTiles[" + i + "] into " + baseName + ".");
				return false;
			}
		}
		for (int i = gridTiles.Length - 1; i >= 0 ; --i) {
			if (!gridTiles[i].import(ref tilemaps, tileset, tileSize)) {
				Debug.LogError("Failed to import gridTiles[" + i + "] into " + baseName + ".");
				return false;
			}
		}
		sortingOrderAddition = tilemaps.getNextSortingOrderAddition();
		return true;
	}

	public int getMatchingImportedTilesetIndex(in ImportedTileset[] tilesets, in Layer[] layers) {
		int tileSetUID = -1;
		for (int i = 0; i < layers.Length; ++i) {
			if (layers[i].identifier == __identifier) {
				tileSetUID = layers[i].autoTilesetDefUid;
				break;
			}
		}
		if (tileSetUID < 0) {
			Debug.LogWarning("Matching layer with identifier \"" + __identifier + "\" not found in dataset.");
			return -1;
		}

		int tileSetIndex = -1;
		for (int i = 0; i < tilesets.Length; ++i) {
			if (tilesets[i].uid == tileSetUID) {
				tileSetIndex = i;
				break;
			}
		}
		if (tileSetIndex < 0) {
			Debug.LogWarning("Matching tileset with uid \"" + tileSetUID + "\" not found in dataset.");
		}
		return tileSetIndex;
	}
}

[System.Serializable]
public class IntGridTile {
	public int coordId;
	public int v;
}

[System.Serializable]
public class Autotile {
	public int ruleId;
	public AutotileResult[] results;

	public bool import(ref TilemapStackMaker mapInOut, ImportedTileset tileset, Vector2Int tileSize) {
		for (int i = results.Length - 1; i >= 0; --i) {
			if (!results[i].import(ref mapInOut, tileset, tileSize)) {
				return false;
			}
		}
		return true;
	}
}

[System.Serializable]
public class AutotileResult {
	public int coordId;
	public AutotileResultTile[] tiles;
	public int flips;

	public bool import(ref TilemapStackMaker mapInOut, ImportedTileset tileset, Vector2Int tileSize) {
		Matrix4x4 symMat = LevelTools.createSymmetryMatrix(isXSym(), isYSym());
		for (int i = tiles.Length - 1; i >= 0; --i) {
			Tile tile = null;
			if (!tileset.getTile(tiles[i].tileId, ref tile)) {
				return false;
			}
			Vector3Int pos = new Vector3Int(tiles[i].__x, tiles[i].__y, 0);
			mapInOut.setTileAndTransformMatrix(pos, symMat, tile, tileSize);
		}
		return true;
	}

	public bool isXSym() {
		return (flips & 1) > 0;
	}

	public bool isYSym() {
		return (flips & 2) > 0;
	}
}

[System.Serializable]
public class AutotileResultTile {
	public int tileId;
	public int __x;
	public int __y;
	public int __srcX;
	public int __srcY;
}

[System.Serializable]
public class GridTile {
	public int coordId;
	public int tileId;
	public int __x;
	public int __y;
	public int __srcX;
	public int __srcY;

	public bool import(ref TilemapStackMaker mapInOut, ImportedTileset tileset, Vector2Int tileSize) {
		Vector3Int pos = new Vector3Int(__x, __y, 0);
		Tile tile = null;
		if (!tileset.getTile(tileId, ref tile)) {
			return false;
		}
		mapInOut.setTileAndTransformMatrix(pos, Matrix4x4.identity, tile, tileSize);
		return true;
	}
}

[System.Serializable]
public class EntityInstance {
	public string __identifier;
	public int __cx;
	public int __cy;
	public int defUid;
	public int x;
	public int y;
	// field instances not implemented
}

/*
	Manages everything Tilemap related
	including creation of:
		Grid Gameobject
		Grid Component
		Tilemap Gameobjects
		Tilemap Components
		TilemapRenderer Components
	as children of baseInOut
*/
public class TilemapStackMaker {

	public static int maxLayers = 4;
	private List<Tilemap> tilemaps;
	private GameObject grid;
	private string tilemapName;
	private int sortingOrderAddition;

	public TilemapStackMaker(ref GameObject baseObjectInOut, string baseName, int pixelsPerUnit, int sortingOrderAddition, Vector3 gridOffset) {
		tilemaps = new List<Tilemap>(1);
		tilemapName = "Tilemap_" + baseName;
		this.sortingOrderAddition = sortingOrderAddition;
		string gridName = "Grid_" + baseName;

		//Setup grid
		//Check, if grid GameObject for this layer already exists
		grid = null;
		for (int i = 0; i < baseObjectInOut.transform.childCount; ++i) {
			GameObject child = baseObjectInOut.transform.GetChild(i).gameObject;
			if (child.name == gridName) {
				grid = child;
				break;
			}
		}
		//Create grid GameObject if neccessary and add as child to baseInOut
		if (grid == null) {
			grid = new GameObject(gridName);
			grid.transform.SetParent(baseObjectInOut.transform);
		} else { //Otherwise clean up grid
			// Delete all grid-Components
			List<Grid> grids = new List<Grid>();
			grid.GetComponents<Grid>(grids);
			foreach (Grid grid in grids)
			{
				Object.DestroyImmediate(grid);
			}
			// Delete all children
			for (int i = 0; i < grid.transform.childCount; ++i) {
				Object.DestroyImmediate(grid.transform.GetChild(i).gameObject);
			}
		}
		//Set grid position offset
		grid.transform.Translate(gridOffset);
		// Create grid component
		Grid tileGrid = grid.AddComponent<Grid>();
		tileGrid.cellGap = new Vector3(0,0,0);
		tileGrid.cellLayout = Grid.CellLayout.Rectangle;
		tileGrid.cellSwizzle = Grid.CellSwizzle.XYZ;
		float gridSize = 1f / ((float) pixelsPerUnit); //makes each cell one pixel wide
		tileGrid.cellSize = new Vector3(gridSize, gridSize, 0);
	}

	public void setTileAndTransformMatrix(Vector3Int pos, Matrix4x4 mat, TileBase tile, Vector2Int tileSize) {
		this.setTileAndTransformMatrixInternal(pos, mat, tile, tileSize, 0);
	}

	private void setTileAndTransformMatrixInternal(Vector3Int pos, Matrix4x4 mat, TileBase tile, Vector2Int tileSize, int layer) {
		if (layer >= maxLayers) {
			return;
		}
		// Add tilemaps, if neccessary
		while (layer >= tilemaps.Count) {
			this.addTilemap();
		}
		// if there are tiles at the position move them down
		moveTilesOneLayerDown(pos, tileSize, layer);
		// set tile
		justSetTileAndTransformMatrix(pos, mat, tile, layer);
	}

	// Set tile and transformmatrix without checking if the layer has space
	private void justSetTileAndTransformMatrix(Vector3Int pos, Matrix4x4 mat, TileBase tile, int layer) {
		Vector3Int posInvY = new Vector3Int(pos.x, -pos.y, 0);
		tilemaps[layer].SetTile(posInvY, tile);
		tilemaps[layer].SetTransformMatrix(posInvY, mat);
	}

	private void moveTilesOneLayerDown(Vector3Int pos, Vector2Int tileSize, int layer) {
		for (int y = pos.y - (tileSize.y - 1); y < pos.y + tileSize.y; ++y) {
			for (int x = pos.x - (tileSize.x - 1); x < pos.x + tileSize.x; ++x) {
				Vector3Int checkPosInvY = new Vector3Int(x, -y, 0);
				Vector3Int checkPos = new Vector3Int(x, y, 0);
				if (tilemaps[layer].HasTile(checkPosInvY)) {
					Matrix4x4 mat = tilemaps[layer].GetTransformMatrix(checkPosInvY);
					Tile tile = tilemaps[layer].GetTile<Tile>(checkPosInvY);
					setTileAndTransformMatrixInternal(checkPos, mat, tile, tileSize, layer + 1);
					justSetTileAndTransformMatrix(checkPos, Matrix4x4.identity, null, layer);
				}
			}
		}
	}

	private void addTilemap() {
		// Create GameObject to hold Tilemap
		GameObject tilemapChild = new GameObject(tilemapName + "_" + tilemaps.Count);
		tilemapChild.transform.SetParent(grid.transform);
		//Create tilemap
		Tilemap tilemap = tilemapChild.AddComponent<Tilemap>();
		//Create tilemap renderer
		TilemapRenderer render = tilemapChild.AddComponent<TilemapRenderer>();
		render.sortingOrder = -(tilemaps.Count + sortingOrderAddition);
		render.sortOrder = TilemapRenderer.SortOrder.TopLeft;

		tilemaps.Add(tilemap);
	}

	public int getNextSortingOrderAddition() {
		return tilemaps.Count + sortingOrderAddition;
	}
}

public static class LevelTools {

	public static Vector3Int coordIdToVector(int coordId, int __cWid) {
		Vector3Int vec = new Vector3Int(0, 0, 0);
		vec.y = coordId / __cWid;
		vec.x = coordId - vec.y * __cWid;
		vec.y = -vec.y;
		return vec;
	}

	public static Matrix4x4 createSymmetryMatrix(bool flipX, bool flipY) {
		Matrix4x4 mat = Matrix4x4.identity;
		Vector3 scale = new Vector3(1, 1, 1);
		if (flipX) {
			scale.x = -1;
		}
		if (flipY) {
			scale.y = -1;
		}
		mat *= Matrix4x4.Scale(scale);
		return mat;
	}

}

}