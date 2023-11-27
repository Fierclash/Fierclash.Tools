/// #UtilityScript

#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Fierclash.Tools
{
	internal static class SceneMenuValidationUtility
	{
		public static void ValidateSettings(SceneMenuSettings settings)
		{
			// Instantiate any null values
			if (settings.profiles == null) settings.profiles = new SceneProfile[0];

			// Update any profiles with invalid GUID
			// [NOTE] Profile GUIDs use C#'s System.Guid while Scene GUIDs use UnityEditor.GUID.
			foreach (var profile in settings.profiles)
			{
				Guid y;
				if (!Guid.TryParse(profile.profileGUID, out y))
				{
					profile.profileGUID = Guid.NewGuid().ToString();
				}
			}

			// Update profiles with duplicate GUIDs
			var profilesList = settings.profiles.ToList();
			var occurences = profilesList.Select(x => x.profileGUID)
										.GroupBy(x => x)
										.ToDictionary(x => x.Key, x => x.Count());
			foreach (var profile in profilesList)
			{
				string GUID = profile.profileGUID;
				if (occurences[GUID] > 1) // Implies there is duplicates with this same GUID
				{
					profile.profileGUID = Guid.NewGuid().ToString();
					--occurences[GUID]; // Update occurence map
				}
			}

			// Cull any invalid and duplicate Scene GUIDs in each Scene Profile
			// [NOTE] Profile GUIDs use C#'s System.Guid while Scene GUIDs use UnityEditor.GUID.
			foreach (var profile in profilesList)
			{
				var sceneGUIDs = profile.sceneGUIDs.Where(x =>
				{
					string path = AssetDatabase.GUIDToAssetPath(x); // Scene Asset with GUID exists
					return !string.IsNullOrEmpty(path);
				}).Distinct();

				profile.sceneGUIDs = sceneGUIDs.ToArray();
			}
			settings.profiles = profilesList.ToArray();
		}

		public static void ValidateBuildSettingsProfile(SceneMenuRuntimeData data)
		{
			var buildSettingsSceneGUIDs = data.buildSettingsProfile.sceneGUIDs.ToList();
			var nullSceneGUIDs = new List<string>();
			foreach (var sceneGUID in buildSettingsSceneGUIDs)
			{
				string path = AssetDatabase.GUIDToAssetPath(sceneGUID);
				if (string.IsNullOrEmpty(path))
				{
					nullSceneGUIDs.Add(sceneGUID);
					continue;
				}

				var sceneAsset = AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset)) as SceneAsset;
				if (sceneAsset == null) nullSceneGUIDs.Add(sceneGUID);
			}

			foreach (var nullSceneGUID in nullSceneGUIDs)
			{
				buildSettingsSceneGUIDs.Remove(nullSceneGUID);
			}
			data.buildSettingsProfile.sceneGUIDs = buildSettingsSceneGUIDs.ToArray();
		}

		public static void ValidateGUIDToSceneProfileMap(SceneMenuRuntimeData data)
		{
			foreach (var kvp in data.GUIDToSceneProfileMap)
			{
				var sceneGUIDs = kvp.Value.sceneGUIDs.ToList();
				var nullSceneGUIDs = new List<string>();
				foreach (var sceneGUID in sceneGUIDs)
				{
					string path = AssetDatabase.GUIDToAssetPath(sceneGUID);
					if (string.IsNullOrEmpty(path))
					{
						nullSceneGUIDs.Add(sceneGUID);
						continue;
					}

					var sceneAsset = AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset)) as SceneAsset;
					if (sceneAsset == null) nullSceneGUIDs.Add(sceneGUID);
				}

				foreach (var nullSceneGUID in nullSceneGUIDs)
				{
					sceneGUIDs.Remove(nullSceneGUID);
				}

				kvp.Value.sceneGUIDs = sceneGUIDs.ToArray();
			}
		}

		public static void ValidateGUIDToSceneDataMap(SceneMenuRuntimeData data)
		{
			var allSceneGUIDs = data.GUIDToSceneProfileMap.Values.SelectMany(x => x.sceneGUIDs)
									.Distinct();
			var missingSceneGUIDs = new List<string>();
			var nullSceneGUIDs = new List<string>();
			foreach (var sceneGUID in allSceneGUIDs)
			{
				string path = AssetDatabase.GUIDToAssetPath(sceneGUID);
				if (string.IsNullOrEmpty(path) ||
					AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset)) as SceneAsset == null)
				{
					nullSceneGUIDs.Add(sceneGUID);
				}
				else if (!data.GUIDToSceneDataMap.ContainsKey(sceneGUID) ||
					data.GUIDToSceneDataMap[sceneGUID] == null)
				{
					missingSceneGUIDs.Add(sceneGUID);
				}
			}

			foreach (var sceneGUID in missingSceneGUIDs)
			{
				string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
				if (data.GUIDToSceneDataMap.ContainsKey(sceneGUID))
				{
					data.GUIDToSceneDataMap[sceneGUID] = new SceneData()
					{
						path = scenePath,
						name = sceneAsset.name,
					};
				}
				else
				{
					data.GUIDToSceneDataMap.Add(sceneGUID, new SceneData()
					{
						path = scenePath,
						name = sceneAsset.name,
					});
				}
			}

			foreach (var nullSceneGUID in nullSceneGUIDs)
			{
				foreach (var kvp in data.GUIDToSceneProfileMap)
				{
					var sceneGUIDs = kvp.Value.sceneGUIDs.ToList();
					if (sceneGUIDs.Contains(nullSceneGUID))
					{
						Debug.Log("Deleting null");
						sceneGUIDs.Remove(nullSceneGUID);
						data.GUIDToSceneProfileMap[kvp.Key].sceneGUIDs = sceneGUIDs.ToArray();
					}
				}
				data.GUIDToSceneDataMap.Remove(nullSceneGUID);
			}
		}
	}
}
#endif
