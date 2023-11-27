/// #UtilityScript

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Fierclash.Tools
{
	internal static class SceneMenuEditorWindowUtility
	{
		public static string[] GetIndexedProfileNames(SceneMenuRuntimeData data)
		{
			var list = new List<string>();
			for (int i = 0; i < data.profileGUIDs.Count; i++)
			{
				string profileGUID = data.profileGUIDs[i];
				var profile = data.GUIDToSceneProfileMap[profileGUID];
				string profileName = string.Format("[{0}] {1}", i, profile.profileName);
				list.Add(profileName);
			}
			return list.ToArray();
		}

		public static bool CheckForBuildSettingsIndex(SceneMenuRuntimeData data)
		{
			return data.profileIndex < 0;
		}

		public static bool CheckForSceneIsActive(string scenePath)
		{
			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				var scene = EditorSceneManager.GetSceneAt(i);
				if (scene.path == scenePath) return true;
			}
			return false;
		}

		public static string GetSceneAssetGUID(UnityEngine.Object obj)
		{
			var sceneAsset = (SceneAsset)obj;
			var sceneAssetPath = AssetDatabase.GetAssetPath(sceneAsset);
			var sceneGUID = AssetDatabase.AssetPathToGUID(sceneAssetPath);
			return sceneGUID;
		}

		public static void SetProfileIndexToBuildSettings(SceneMenuRuntimeData data)
		{
			data.profileIndex = -1;
		}
	}
}
#endif
