using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LDtk {
public class ImporterWindow : EditorWindow {

	private const string windowName = "LDtk Importer";
	private string jsonPath = "";
	private int pixelsPerUnit = 16;
	private string importDir = "";
	private static bool deleteWithoutPromptConfirmed = false;

	[MenuItem("Assets/Import/" + windowName)]
	public static void ShowWindow() {
		EditorWindow.GetWindow<ImporterWindow>(windowName);
	}

	void OnGUI() {
		//Window code
		string jsonImportMessage = "Choose LEd file to import:";
		GUILayout.Label(jsonImportMessage);
		EditorGUILayout.BeginHorizontal();
		jsonPath = EditorGUILayout.TextField("LDtk/Json Path", jsonPath);
		bool browse = GUILayout.Button("Browse");
		EditorGUILayout.EndHorizontal();

		pixelsPerUnit = EditorGUILayout.IntField("Pixels per unit", pixelsPerUnit);
		importDir = EditorGUILayout.TextField("Import directory", importDir);
		GUILayout.Label("(relative to Assets)");
		importDir = GeneralTools.fixDir(importDir);

		TilemapStackMaker.maxLayers = EditorGUILayout.IntField("Maximum Tilemaps per Layer", TilemapStackMaker.maxLayers);

		AssetTools.deleteWithoutPrompt = EditorGUILayout.Toggle("Delete without prompt", AssetTools.deleteWithoutPrompt);
		if (AssetTools.deleteWithoutPrompt && !deleteWithoutPromptConfirmed) {
			if (EditorUtility.DisplayDialog("Activating \"Delete without prompt\" will disable all overwrite or delete prompts.", "This is potentially dangerous. Are you sure?", "I am sure", "Reset")) {
				AssetTools.deleteWithoutPrompt = true;
				deleteWithoutPromptConfirmed = true;
			} else {
				AssetTools.deleteWithoutPrompt = false;
			}
		} else if (!AssetTools.deleteWithoutPrompt) {
			deleteWithoutPromptConfirmed = false;
		}

		if (browse) {
			jsonPath = EditorUtility.OpenFilePanelWithFilters(jsonImportMessage, "./", new string[] {"LDtk file", "ldtk", "Json file", "json"});
		}

		bool import = GUILayout.Button("Import");
		if (import) {
			Debug.ClearDeveloperConsole();
			bool result = Importer.import(jsonPath, importDir, pixelsPerUnit);
			if (result) {
				Debug.Log("LEd project \"" + jsonPath + "\" imported successfully.");
			} else {
				Debug.LogError("LEd project import failed.");
			}
		}
	}
}

}
