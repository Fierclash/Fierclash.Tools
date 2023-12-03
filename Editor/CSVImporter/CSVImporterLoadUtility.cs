/// #UtilityScript

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Fierclash.Tools
{
	internal static class CSVImporterLoadUtility
	{
#if FIERCLASH_TOOLS_DEVELOPER
		private static readonly string CONFIG_PATH = "Assets/Fierclash.Tools/Editor/CSVImporter/config.json";
#else
		private static readonly string CONFIG_PATH = "Packages/Fierclash.Tools/Editor/CSVImporter/config.json";
#endif
		private static readonly string SETTINGS_DEFAULT_PATH = "Assets/Fierclash.Tools/Settings/Fierclash.Tools.CSVImporter.Settings.json";

		public static CSVImporterConfig ImportConfig()
		{
			// Access Config file path
			// If no config exists, create one
			CSVImporterConfig config = FileUtility.ReadJsonFromPath<CSVImporterConfig>(CONFIG_PATH);
			if (config == null)
			{
				config = new CSVImporterConfig()
				{
					settingsGUID = "",
				};
				FileUtility.WriteJsonToPath(config, CONFIG_PATH);
				AssetDatabase.Refresh();
			}

			return config;
		}

		public static CSVImporterSettings ImportSettingsFromConfig()
		{
			// Load Config
			var config = ImportConfig();

			// Access Settings SO
			// If no SO exists, create one
			string settingsAssetPath = AssetDatabase.GUIDToAssetPath(config.settingsGUID);
			var settings = FileUtility.ReadJsonFromPath<CSVImporterSettings>(settingsAssetPath);
			if (settings == null)
			{
				settings = new CSVImporterSettings()
				{
					profiles = new CSVImporterProfile[0],
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

		public static void ExportSettingsFromConfig(CSVImporterSettings settings)
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

		public static void LoadProfileGUIDsFromSettings(CSVImporterRuntimeData data, CSVImporterSettings settings)
		{
			if (data.profileGUIDs == null) data.profileGUIDs = new();
			foreach (var profile in settings.profiles)
			{
				string profileGUID = profile.profileGUID;
				data.profileGUIDs.Add(profileGUID);
			}
		}

		public static void LoadGUIDToProfileMapFromSettings(CSVImporterRuntimeData data, CSVImporterSettings settings)
		{
			if (data.GUIDToProfileMap == null) data.GUIDToProfileMap = new();
			foreach (var settingsProfile in settings.profiles)
			{
				// Profile GUID Guard
				// Profile GUID will be stored in a map in runtime data, so it is important
				// to ensure it is a valid GUID and is not an existing key.
				string profileGUID = settingsProfile.profileGUID;
				Guid x;
				if (!Guid.TryParse(profileGUID, out x) || // GUID cannot be empty
					data.GUIDToProfileMap.ContainsKey(profileGUID)) // Skip Existing Entries
				{
					continue;
				}

				var profile = new CSVImporterProfile(settingsProfile);
				data.GUIDToProfileMap.Add(profileGUID, profile);
			}
		}

		public static void LoadSettingsProfiles(CSVImporterSettings settings, CSVImporterRuntimeData data)
		{
			// Uses runtime data to update Settings profiles
			// Importer Profile contains:
			//	- Profile GUID
			//	- Profile Name
			//	- Google Sheets ID
			//	- Asset Prefix
			//	- Array of Sheets Names
			//	- Import Mode

			int profileCount = data.profileGUIDs.Count;
			var newProfiles = new CSVImporterProfile[profileCount];
			for (int i = 0; i < profileCount; i++)
			{
				string profileGUID = data.profileGUIDs[i];
				if (data.GUIDToProfileMap.ContainsKey(profileGUID))
				{
					var profile = data.GUIDToProfileMap[profileGUID];
					newProfiles[i] = new CSVImporterProfile(profile);
				}
				// Edge case if Profile was not properly stored in runtime data
				else
				{
					newProfiles[i] = new CSVImporterProfile()
					{
						profileGUID = profileGUID,
						profileName = "New Profile",
						googleSheetsID = "",
						assetPath = "",
						assetPrefix = "",
						sheets = new string[0],
						importMode = CSVImportMode.None
					};
				}
			}
			settings.profiles = newProfiles;
		}
	}

	internal static class CSVImporterValidationUtility
	{
		public static void ValidateSettings(CSVImporterSettings settings)
		{
			// Instantiate any null values
			if (settings.profiles == null) settings.profiles = new CSVImporterProfile[0];

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

			// Update profiles by replacing duplicate GUIDs
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
			settings.profiles = profilesList.ToArray();
		}

		public static void ValidateProfileGUIDs(CSVImporterRuntimeData data)
		{
			data.profileGUIDs = data.profileGUIDs.Distinct()
												.Where(x => !string.IsNullOrEmpty(x))
												.ToList();
		}

		public static void ValidateGUIDToProfileMap(CSVImporterRuntimeData data)
		{
			var keys = data.GUIDToProfileMap.Keys; // Cache to iterate through and modify dictionary
			foreach (var key in keys)
			{
				// Fill in null value entries
				var profile = data.GUIDToProfileMap[key];
				if (profile == null) data.GUIDToProfileMap[key] = new CSVImporterProfile()
				{
					profileGUID = key,
					profileName = "New Profile",
					googleSheetsID = "",
					assetPath = "",
					assetPrefix = "",
					sheets = new string[0],
					importMode = CSVImportMode.None,
				};

				// Instantiate null sheet arrays
				if (profile.sheets == null) data.GUIDToProfileMap[key].sheets = new string[0];
			}
		}
	}

	internal static class CSVImporterEditorWindowUtility
	{
		public static void SetProfileIndexToDefault(CSVImporterRuntimeData data)
		{
			data.profileIndex = 0;
		}

		public static bool IsAnyProfiles(CSVImporterRuntimeData data)
		{
			return data.profileGUIDs.Any();
		}

		public static string[] GetIndexedProfileNames(CSVImporterRuntimeData data)
		{
			var list = new List<string>();
			for (int i = 0; i < data.profileGUIDs.Count; i++)
			{
				string profileGUID = data.profileGUIDs[i];
				var profile = data.GUIDToProfileMap[profileGUID];
				string profileName = string.Format("[{0}] {1}", i, profile.profileName);
				list.Add(profileName);
			}
			return list.ToArray();
		}
	}

	internal static class CSVImporterUtility
	{
		public static int GetProfileIndex(CSVImporterRuntimeData data)
		{
			return data.profileIndex;
		}

		public static CSVImporterProfile GetProfileFromProfileIndex(CSVImporterRuntimeData data)
		{
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			return GetProfileFromProfileIndex(data, profileGUID);
		}

		public static CSVImporterProfile GetProfileFromProfileIndex(CSVImporterRuntimeData data, string profileGUID)
		{
			if (!data.GUIDToProfileMap.ContainsKey(profileGUID)) return null;
			return new CSVImporterProfile(data.GUIDToProfileMap[profileGUID]);
		}

		public static string GetProfileGUIDFromProfileIndex(CSVImporterRuntimeData data)
		{
			int index = data.profileIndex;
			if (index < 0 || index >= data.profileGUIDs.Count) return "";
			return data.profileGUIDs[index];
		}

		public static void SetProfileIndex(CSVImporterRuntimeData data, int index)
		{
			data.profileIndex = Mathf.Min(index, data.profileGUIDs.Count);
		}

		public static void SetProfileIndexToLastIndex(CSVImporterRuntimeData data)
		{
			SetProfileIndex(data, data.profileGUIDs.Count - 1);
		}

		public static void SetProfileNameAtProfileIndex(CSVImporterRuntimeData data, string profileName)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.profileName = profileName;
			}
		}

		public static void SetAssetPathAtProfileIndex(CSVImporterRuntimeData data, string assetPath)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.assetPath = assetPath;
			}
		}

		public static void SetAssetPrefixAtProfileIndex(CSVImporterRuntimeData data, string assetPrefix)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.assetPrefix = assetPrefix;
			}
		}

		public static void SetDocIDAtProfileIndex(CSVImporterRuntimeData data, string docID)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.googleSheetsID = docID;
			}
		}

		public static void SetSheetsAtProfileIndex(CSVImporterRuntimeData data, string[] sheets)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.sheets = new string[sheets.Length];
				Array.Copy(sheets, profile.sheets, sheets.Length);
			}
		}

		public static void SetImportModeAtProfileIndex(CSVImporterRuntimeData data, CSVImportMode mode)
		{
			var profileGUID = GetProfileGUIDFromProfileIndex(data);
			if (data.GUIDToProfileMap.ContainsKey(profileGUID))
			{
				var profile = data.GUIDToProfileMap[profileGUID];
				profile.importMode = mode;
			}
		}

		public static void AddProfileToGUIDToProfileMap(CSVImporterRuntimeData data)
		{
			// Create a new Scene Profile and store in GUID->SceneProfile map
			string profileGUID = Guid.NewGuid().ToString();
			data.profileGUIDs.Add(profileGUID);
			data.GUIDToProfileMap.Add(profileGUID, new CSVImporterProfile()
			{
				profileGUID = profileGUID,
				profileName = "New Profile",
				googleSheetsID = "",
				assetPath = "",
				assetPrefix = "",
				sheets = new string[0],
				importMode = CSVImportMode.None,
			});
		}
		public static void RemoveProfileGUIDAtProfileIndex(CSVImporterRuntimeData data)
		{
			// Remove Profile GUID from stored GUIDs
			string profileGUID = GetProfileGUIDFromProfileIndex(data);
			data.profileGUIDs.Remove(profileGUID);
		}
	}
}
#endif