/// #UtilityScript

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Fierclash.Tools
{
	internal static class SceneMenuUtility
	{
		public static int GetProfileIndex(SceneMenuRuntimeData data)
		{
			return data.profileIndex;
		}

		public static SceneProfile GetSceneProfileFromProfileIndex(SceneMenuRuntimeData data)
		{
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			return GetSceneProfileFromProfileIndex(data, profileGUID);
		}

		public static SceneProfile GetSceneProfileFromProfileIndex(SceneMenuRuntimeData data, string profileGUID)
		{
			return new SceneProfile(data.GUIDToSceneProfileMap[profileGUID]);
		}

		public static string GetProfileGUIDFromProfileIndex(SceneMenuRuntimeData data)
		{
			int index = data.profileIndex;
			if (index < 0 || index >= data.profileGUIDs.Count) return "";
			return data.profileGUIDs[index];
		}

		public static string GetProfileNameFromProfileIndex(SceneMenuRuntimeData data)
		{
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (string.IsNullOrEmpty(profileGUID) || 
				!data.GUIDToSceneProfileMap.ContainsKey(profileGUID)) return "";
			return data.GUIDToSceneProfileMap[profileGUID].profileName;
		}

		public static string[] GetSceneGUIDsFromProfileIndex(SceneMenuRuntimeData data)
		{
			// Profile GUID Guard
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (string.IsNullOrEmpty(profileGUID)) return null;

			// Create a copy of Scene GUIDs
			string[] sceneGUIDsRef = data.GUIDToSceneProfileMap[profileGUID].sceneGUIDs;
			string[] sceneGUIDs = new string[sceneGUIDsRef.Length];
			Array.Copy(sceneGUIDsRef, sceneGUIDs, sceneGUIDsRef.Length); // Deep Copy
			return sceneGUIDs;
		}

		public static void SetProfileIndex(SceneMenuRuntimeData data, int index)
		{
			data.profileIndex = Mathf.Min(index, data.profileGUIDs.Count);
		}

		public static void SetProfileIndexToLastIndex(SceneMenuRuntimeData data)
		{
			SetProfileIndex(data, data.profileGUIDs.Count - 1);
		}

		public static void SetProfileNameAtProfileIndex(SceneMenuRuntimeData data, string profileName)
		{
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (string.IsNullOrEmpty(profileGUID)) return;
			data.GUIDToSceneProfileMap[profileGUID].profileName = profileName;
		}

		public static void AddSceneToProfileAtProfileIndex(SceneMenuRuntimeData data, string sceneGUID)
		{
			// Add Scene GUID to currently selected Scene Profile in GUID->SceneProfile map
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			var sceneGUIDList = data.GUIDToSceneProfileMap[profileGUID].sceneGUIDs.ToList();
			if (!sceneGUIDList.Contains(sceneGUID))
			{
				sceneGUIDList.Add(sceneGUID);
				data.GUIDToSceneProfileMap[profileGUID].sceneGUIDs = sceneGUIDList.ToArray();
			}

			// Add Scene GUID to Scene GUID -> Scene Data map
			if (data.GUIDToSceneDataMap.ContainsKey(profileGUID))
			{
				string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
				data.GUIDToSceneDataMap[sceneGUID] = new SceneData()
				{
					path = scenePath,
					name = sceneAsset.name,
				};
			}
		}

		public static void AddProfileToGUIDToSceneProfileMap(SceneMenuRuntimeData data)
		{
			// Create a new Scene Profile and store in GUID->SceneProfile map
			string profileGUID = Guid.NewGuid().ToString();
			data.profileGUIDs.Add(profileGUID);
			data.GUIDToSceneProfileMap.Add(profileGUID, new SceneProfile()
			{
				profileGUID = profileGUID,
				profileName = "New Profile",
				sceneGUIDs = new string[0],
			});
		}

		public static void RemoveProfileGUIDAtProfileIndex(SceneMenuRuntimeData data)
		{
			// Remove Profile GUID from stored GUIDs
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			data.profileGUIDs.Remove(profileGUID);
		}

		public static void RemoveSceneAtProfileIndex(SceneMenuRuntimeData data, int sceneIndex)
		{
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			var sceneGUIDList = data.GUIDToSceneProfileMap[profileGUID].sceneGUIDs.ToList();
			sceneGUIDList.RemoveAt(sceneIndex);
			data.GUIDToSceneProfileMap[profileGUID].sceneGUIDs = sceneGUIDList.ToArray();
		}
	}
}
#endif
