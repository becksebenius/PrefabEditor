﻿/*
 	PrefabEditor.cs
 	Author: Beck Sebenius
 	Contact: rseben at gmail dot com
 	
 	Description:
 		PrefabEditor.cs creates a hotkey to edit a prefab in an empty scene, and allows
 	you to use Ctrl+S to save changes to that prefab.
 	
 	* Opens the PrefabEditor scene when CTRL+E is pressed on a prefab,
 		then creates an instance of the prefab.
 		
 	* If the PrefabEditor scene doesn't exist next to PrefabEditor.cs, one is created.
 	
 	* Watches for asset saves on the PrefabEditor scene, and saves changes 
 		to the currently edited prefab.
*/


using UnityEngine;
using UnityEditor;
using System.Linq;

// Scriptable Object used to find monoscript, which aids us in finding
//	the PrefabEditor scene
// This lets us put the PrefabEditor folder wherever we like :)
public class PrefabEditor : ScriptableObject {}

[InitializeOnLoad]
public class PrefabEditorAssetProcessor : UnityEditor.AssetModificationProcessor 
{
	const string MenuItemPath = "Assets/Edit Prefab %e";
	
	static string scenePath;
	static PrefabEditorAssetProcessor ()
	{
		// Create prefab editor instance so that we can find it's monoscript location
		//	that way we can find its sister path for the PrefabEditor scene.
		var instance = ScriptableObject.CreateInstance<PrefabEditor>();
		var monoscript = MonoScript.FromScriptableObject(instance);
		var path = AssetDatabase.GetAssetPath(monoscript);
		
		//Derive scene path from asset path
		scenePath = path.Substring(0, path.Length - 2) + "unity";
		
		//Cleanup instance
		GameObject.DestroyImmediate(instance);		
	}
	
	[MenuItem(MenuItemPath, true)]
	public static bool ValidateEditPrefab ()
	{
		var prefab = Selection.activeGameObject;
		
		if(!prefab) return false;
		
		if(PrefabUtility.GetPrefabType(prefab) != PrefabType.Prefab)
			return false;
		
		return true;
	}
	
	[MenuItem(MenuItemPath)]
	public static void EditPrefab ()
	{
		EditPrefab(Selection.activeGameObject);
	}
	
	public static void EditPrefab (GameObject prefab)
	{
		if(!prefab) 
			return;
		
		//Make sure we're editing a prefab
		if(PrefabUtility.GetPrefabType(prefab) != PrefabType.Prefab)
			return;
		
		//Saving niceness in case you're currently editing a different scene
		if(!EditorApplication.SaveCurrentSceneIfUserWantsTo())
			return;
		
		//Open the PrefabEditor scene
		if(!EditorApplication.OpenScene(scenePath))
		{
			// Create a new scene in case the PrefabEditor scene doesn't exist for some reason
			EditorApplication.NewScene();
		}
		
		//Clean up the PrefabEditor scene since the last use
		var allGameObjects = GameObject.FindObjectsOfType(typeof(GameObject));
		foreach(var go in allGameObjects)
		{
			GameObject.DestroyImmediate(go);
		}
		
		//Overwrite existing scene
		EditorApplication.SaveScene(scenePath);
		
		//Instantiate the prefab and select it
		var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		Selection.activeGameObject = instance;
		
		//Focus our scene view camera to aid in editing
		if(SceneView.lastActiveSceneView)
		{
			SceneView.lastActiveSceneView.Focus();
		}
	}
	
	public static string[] OnWillSaveAssets (string[] paths)
	{
		//Only perform prefab saving if we're in the PrefabEditor scene
		if(EditorApplication.currentScene != scenePath)
			return paths;
		
		//Make sure this save pass is actually trying to save the PrefabEditor scene
		if(!paths.Contains(scenePath))
			return paths;
		
		//If we hit this point, it means we have the prefab editor open
		//	and it is in the process of being saved.
		var allGameObjects = GameObject.FindObjectsOfType(typeof(GameObject));
		foreach(var obj in allGameObjects)
		{
			var go = obj as GameObject;
			
			//Is this gameobject a prefab instance?
			if(PrefabUtility.GetPrefabType(go) != PrefabType.PrefabInstance)
				continue;
			
			//Is this the root of the prefab?
			if(PrefabUtility.FindPrefabRoot(go) != go)
				continue;
			
			//Apply changes
			PrefabUtility.ReplacePrefab(go, PrefabUtility.GetPrefabParent(go), ReplacePrefabOptions.ConnectToPrefab);
		}
		
		return paths;
	}
}
