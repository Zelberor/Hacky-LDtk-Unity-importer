using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace LDtk {
public static class Importer
{
	public const string supportedApp = "LDtk";
	public const string supportedAppVersion = "0.9.3";
	

	public static bool import(string jsonPath, string importDir, int pixelsPerUnit) {
		importDir = GeneralTools.fixDir(importDir);
		importDir = importDir + "/" + Path.GetFileNameWithoutExtension(jsonPath);
		if (pixelsPerUnit <= 0) {
			Debug.LogError("Pixels per unit must not be <= 0!");
			return false;
		}
		if (!System.IO.File.Exists(jsonPath)) {
			Debug.LogError("Unable to locate \"" + jsonPath + "\"");
			return false;
		}

		//Read json
		string json = System.IO.File.ReadAllText(jsonPath);
		ProjectJSON projectData = JsonUtility.FromJson<ProjectJSON>(json);

		//check version
		if (projectData.__header__.app != supportedApp) {
			Debug.LogError("Unsupported app: " + projectData.__header__.app);
			return false;
		}
		if (projectData.__header__.appVersion != supportedAppVersion) {
			Debug.LogError("Unsupported app version: " + projectData.__header__.appVersion);
			return false;
		}
		if (projectData.externalLevels) {
			Debug.LogError("External Levels are currently not supported");
			return false;
		}

		string jsonDir = GeneralTools.fixDir(Path.GetDirectoryName(jsonPath));
		string tilesetsDir = importDir;
		// Import tilesets
		ImportedTileset[] importedTilesets = new ImportedTileset[projectData.defs.tilesets.Length];
		for (int i = 0; i < projectData.defs.tilesets.Length; ++i) {
			ImportedTileset iT = projectData.defs.tilesets[i].import(jsonDir, pixelsPerUnit, tilesetsDir);
			if (iT == null) {
				Debug.LogError("Importing tileset " + i + " failed.");
				return false;
			}
			importedTilesets[i] = iT;
			Debug.Log("Tileset " + i + " \"" + iT.identifier + "\" successfully imported.");
		}

		// Import levels
		for (int i = 0; i < projectData.levels.Length; ++i) {
			if (!projectData.levels[i].import(importDir, pixelsPerUnit, importedTilesets)) {
				Debug.LogError("Importing level " + i + " \"" + projectData.levels[i].identifier + "\" failed.");
				return false;
			}
			Debug.Log("Level " + i + " \"" + projectData.levels[i].identifier + "\" successfully imported.");
		}
		

		return true;
	}
}

public static class GeneralTools {

	public static string fixDir(string dir) {
		if (dir.Length > 1) {
			char[] trim = {'\\', '/'};
			dir = dir.TrimEnd(trim);
		}
		return dir;
	}

}

public static class AssetTools {
	//All assetPath or assetDir variables root in the Assets folder. Used path for AssetDatabase funktions: "Assets/assetPath"

	public static bool deleteWithoutPrompt = false;

	private static string addAssetToPath(string assetPath) {
		return "Assets/" + assetPath;
	}

	public static bool deleteAssetIfExists(string assetPath, bool overWriteMessage = false) {
		//Check if asset exists
		if (AssetDatabase.LoadAssetAtPath<Object>(addAssetToPath(assetPath)) != null) {
			bool delete = false;
			if (!deleteWithoutPrompt) {
				if (overWriteMessage) {
					delete = EditorUtility.DisplayDialog("Asset \"" + assetPath + "\" already exists.", "Delete \"" + assetPath + "\"?", "Delete", "Cancel import");
				} else {
					delete = EditorUtility.DisplayDialog("Delete asset \"" + assetPath + "\"?", "Delete \"" + assetPath + "\"?", "Delete", "Cancel import");
				}
			} else {
				delete = true;
			}

			if (delete) {
				AssetDatabase.MoveAssetToTrash(addAssetToPath(assetPath));
			} else {
				return false;
			}
		}
		return true;
	}

	public static bool createInternalFolder(string assetFolder) {
		System.IO.Directory.CreateDirectory(Application.dataPath + "/" + GeneralTools.fixDir(assetFolder));
		return true;
	}

	/*Imports external asset
		fullExternalPath must include filename and extension
		assetDir is the directory the asset will be imported into
		assetName is the new asset name WITHOUT extension
	*/
	public static bool importExternalAsset(string fullExternalPath, string assetDir, string assetName, bool autoCreateFolder = true, bool overWrite = false) {
		string assetPath = assetDir + "/" + assetName + Path.GetExtension(fullExternalPath);
		if (autoCreateFolder) {
			//make sure import Folder exists
			if (!createInternalFolder(assetDir)) {
				return false;
			}
		}
		if (overWrite) {
			//delete file at assetPath, if present
			if (!deleteAssetIfExists(assetPath, true)) {
				return false;
			}
		}
		AssetDatabase.Refresh();
		FileUtil.CopyFileOrDirectory(fullExternalPath, Application.dataPath + "/" + assetPath);
		AssetDatabase.ImportAsset(addAssetToPath(assetPath));
		//Check, if the asset can be loaded
		if (AssetDatabase.LoadAssetAtPath<Object>(addAssetToPath(assetPath)) == null) {
			return false;
		}
		return true;
	}

	public static bool createAsset(Object obj, string assetPath, bool autoCreateFolder = true, bool overWrite = false) {
		if (autoCreateFolder) {
			//make sure Folder exists
			if (!createInternalFolder(Path.GetDirectoryName(assetPath))) {
				return false;
			}
		}
		if (overWrite) {
			//delete file at assetPath, if present
			if (!deleteAssetIfExists(assetPath, true)) {
				return false;
			}
		}
		AssetDatabase.Refresh();

		//Handle GameObjects and other assets
		GameObject gObj;
		try {
			gObj = (GameObject) obj;
		} catch (System.InvalidCastException) {
			gObj = null;
		}
		if (gObj == null) {
			//obj is not a GameObject
			return createAssetWithAssetDatabase(obj, assetPath);
		} else {
			//obj is a GameObject
			return createPrefab(gObj, assetPath);
		}
	}

	private static bool createAssetWithAssetDatabase(Object obj, string assetPath) {
		AssetDatabase.CreateAsset(obj, addAssetToPath(assetPath));
		//Check, if the asset can be loaded
		if (AssetDatabase.LoadAssetAtPath<Object>(addAssetToPath(assetPath)) == null) {
			return false;
		}
		return true;
	}

	private static bool createPrefab(GameObject obj, string assetPath) {
		bool success = false;
		PrefabUtility.SaveAsPrefabAsset(obj, addAssetToPath(assetPath), out success);
		return success;
	}
}

}
