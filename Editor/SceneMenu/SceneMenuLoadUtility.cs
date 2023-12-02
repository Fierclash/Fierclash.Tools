/// #UtilityScript

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Fierclash.Tools
{
	internal static class SceneMenuLoadUtility
	{
#if FIERCLASH_TOOLS_DEVELOPER
		private static readonly string CONFIG_PATH = "Assets/Fierclash.Tools/Editor/SceneMenu/config.json";
#else
		private static readonly string CONFIG_PATH = "Packages/Fierclash.Tools/Editor/SceneMenu/config.json";
#		endif
		private static readonly string SETTINGS_DEFAULT_PATH = "Assets/Fierclash.Tools/Settings/Fierclash.Tools.SceneMenu.Settings.json";

		public static SceneMenuConfig ImportConfig()
		{
			// Access Config file path
			// If no config exists, create one
			SceneMenuConfig config = FileUtility.ReadJsonFromPath<SceneMenuConfig>(CONFIG_PATH);
			if (config == null)
			{
				config = new SceneMenuConfig()
				{
					settingsGUID = "",
				};
				FileUtility.WriteJsonToPath(config, CONFIG_PATH);
				AssetDatabase.Refresh();
			}

			return config;
		}

		public static SceneMenuSettings ImportSettingsFromConfig()
		{
			// Load Config
			var config = ImportConfig();

			// Access Settings SO
			// If no SO exists, create one
			string settingsAssetPath = AssetDatabase.GUIDToAssetPath(config.settingsGUID);
			var settings = FileUtility.ReadJsonFromPath<SceneMenuSettings>(settingsAssetPath);
			if (settings == null)
			{
				settings = new SceneMenuSettings()
				{
					profiles = new SceneProfile[0],
				};
				FileUtility.WriteJsonToPath(settings, SETTINGS_DEFAULT_PATH, true);

				// Update config's stored settingsGUID
				string settingsGUID = AssetDatabase.AssetPathToGUID(SETTINGS_DEFAULT_PATH);
				config.settingsGUID = settingsGUID;
				FileUtility.WriteJsonToPath(config, CONFIG_PATH, true); // Update config file to match new Settings GUID
				AssetDatabase.Refresh();
			}

			return settings;
		}

		public static void ExportSettingsFromConfig(SceneMenuSettings settings)
		{
			// Load Config
			var config = ImportConfig();

			// Access Settings SO
			// If no SO exists, create one
			string settingsAssetPath = AssetDatabase.GUIDToAssetPath(config.settingsGUID);
			bool exported = FileUtility.WriteJsonToPath(settings, settingsAssetPath, true);
			if (!exported) Debug.LogErrorFormat("Failed to export SceneMenuSettings to {0}.",
												settingsAssetPath);
		}

		public static void LoadProfileGUIDsFromSettings(SceneMenuRuntimeData data, SceneMenuSettings settings)
		{
			if (data.profileGUIDs == null) data.profileGUIDs = new();
			foreach (var profile in settings.profiles)
			{
				string profileGUID = profile.profileGUID;
				data.profileGUIDs.Add(profileGUID);
			}
		}

		public static void LoadGUIDToSceneProfileMapFromSettings(SceneMenuRuntimeData data, SceneMenuSettings settings)
		{
			// Uses Settings to add map entries (Scene GUID -> Scene Profile) in runtime data.
			// Scene Profile contains:
			//	- Profile GUID
			//	- Profile Name
			//	- Scene GUIDs

			if (data.GUIDToSceneProfileMap == null) data.GUIDToSceneProfileMap = new();
			foreach (var profile in settings.profiles)
			{
				// Profile GUID Guard
				// Profile GUID will be stored in a map in runtime data, so it is important
				// to ensure it is a valid GUID and is not an existing key.
				string profileGUID = profile.profileGUID;
				Guid x;
				if (!Guid.TryParse(profileGUID, out x) || // GUID cannot be empty
					data.GUIDToSceneProfileMap.ContainsKey(profileGUID)) // Skip Existing Entries
				{
					continue;
				}

				// Insert Entry into map
				string profileName = profile.profileName;
				var sceneProfile = new SceneProfile() // Deep Copy 
				{
					profileGUID = profileGUID,
					profileName = profileName,
					sceneGUIDs = new string[profile.sceneGUIDs.Length],
				};
				Array.Copy(profile.sceneGUIDs, sceneProfile.sceneGUIDs, profile.sceneGUIDs.Length);
				data.GUIDToSceneProfileMap.Add(profileGUID, sceneProfile);
			}
		}

		public static void LoadGUIDToSceneDataMapFromBuildSettings(SceneMenuRuntimeData data)
		{
			// Uses Build Settings to add map entries (Scene GUID -> Scene Data) in runtime data.
			// Scene Data contains:
			//	- Scene Path
			//	- Scene Name
			// Each non-null scene in Build Settings will be added as an entry.

			if (data.GUIDToSceneDataMap == null) data.GUIDToSceneDataMap = new();
			var buildScenes = EditorBuildSettings.scenes.ToList();
			foreach (var buildScene in buildScenes)
			{
				// Scene GUID Guard
				// Scene GUID will be stored in a map in runtime data, so it is important
				// to ensure it is a valid GUID and is not an existing key.
				string scenePath = buildScene.path; // File path to scene's .unity file
				string sceneGUID = AssetDatabase.AssetPathToGUID(scenePath,// GUID
																AssetPathToGUIDOptions.OnlyExistingAssets); // Get only currently existing assets
				if (string.IsNullOrEmpty(sceneGUID) || // GUID cannot be empty
					data.GUIDToSceneDataMap.ContainsKey(sceneGUID)) // Duplicate Key
				{
					continue;
				}

				// Insert Entry into map
				string sceneName = Path.GetFileNameWithoutExtension(scenePath); // Scene Name
				var scene = new SceneData()
				{
					path = scenePath,
					name = sceneName,
				};
				data.GUIDToSceneDataMap.Add(sceneGUID, scene);
			}
		}

		public static void LoadGUIDToSceneDataMapFromSettings(SceneMenuRuntimeData data, SceneMenuSettings settings)
		{
			// Uses Settings to add map entries (Scene GUID -> Scene Data) in runtime data.
			// Scene Data contains:
			//	- Scene Path
			//	- Scene Name
			// Each non-null scene in Build Settings will be added as an entry.

			if (data.GUIDToSceneDataMap == null) data.GUIDToSceneDataMap = new();
			var sceneGUIDs = settings.profiles.SelectMany(x => x.sceneGUIDs);
			foreach (var sceneGUID in sceneGUIDs)
			{
				// Scene GUID Guard
				// Scene GUID will be stored in a map in runtime data, so it is important
				// to ensure it is a valid GUID and is not an existing key.
				string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID); // Scene Path
				if (string.IsNullOrEmpty(scenePath) || // Scene Asset must exist
					data.GUIDToSceneDataMap.ContainsKey(sceneGUID)) // Duplicate Key
				{
					continue;
				}

				// Insert Entry into map
				string sceneName = Path.GetFileNameWithoutExtension(scenePath); // Scene Name
				var scene = new SceneData()
				{
					path = scenePath,
					name = sceneName,
				};
				data.GUIDToSceneDataMap.Add(sceneGUID, scene);
			}
		}

		public static void LoadSettingsProfiles(SceneMenuSettings settings, SceneMenuRuntimeData data)
		{
			// Uses runtime data to update Settings profiles
			// Scene Profile contains:
			//	- Profile GUID
			//	- Profile Name
			//	- Scene GUIDs

			int profileCount = data.profileGUIDs.Count;
			var newProfiles = new SceneProfile[profileCount];
			for (int i = 0; i < profileCount; i++)
			{
				string profileGUID = data.profileGUIDs[i];
				if (data.GUIDToSceneProfileMap.ContainsKey(profileGUID))
				{
					var profile = data.GUIDToSceneProfileMap[profileGUID];
					string profileName = profile.profileName;
					int profileGUIDsCount = profile.sceneGUIDs.Length;
					string[] sceneGUIDs = new string[profileGUIDsCount];
					Array.Copy(profile.sceneGUIDs, sceneGUIDs, profileGUIDsCount);
					newProfiles[i] = new SceneProfile()
					{
						profileGUID = profileGUID,
						profileName = profileName,
						sceneGUIDs = sceneGUIDs,
					};
				}
				// Edge case if Profile was not properly stored in runtime data
				else
				{
					newProfiles[i] = new SceneProfile()
					{
						profileGUID = profileGUID,
						profileName = "New Profile",
						sceneGUIDs = new string[0],
					};
				}
			}
			settings.profiles = newProfiles;
		}

		public static void LoadBuildSettingsProfile(SceneMenuRuntimeData data)
		{
			// Creates a Scene Profile with Scene GUIDs from the editor's Build Settings.
			// Missing scenes that are stored in Build Settings are ignored.

			var buildScenes = EditorBuildSettings.scenes.ToList();
			var sceneGUIDList = new List<string>();
			foreach (var buildScene in buildScenes)
			{
				string scenePath = buildScene.path; // File path to scene's .unity file
				string sceneGUID = AssetDatabase.AssetPathToGUID(scenePath); // GUID
				if (!string.IsNullOrEmpty(sceneGUID)) // Missing Scene
				{
					sceneGUIDList.Add(sceneGUID);
				}
			}
			data.buildSettingsProfile = new SceneProfile()
			{
				profileName = "Build Settings",
				profileGUID = "",
				sceneGUIDs = sceneGUIDList.Distinct().ToArray(), // Remove Duplicates
			};
		}
	}

}
#endif
