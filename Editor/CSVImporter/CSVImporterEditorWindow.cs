/// #EditorScript

#pragma warning disable 4014 // Disable warning for Task not being used by await

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;


namespace Fierclash.Tools
{
	internal sealed class CSVImporterEditorWindow : EditorWindow
	{
		public class StringListSO: ScriptableObject { public List<string> value; }

		CSVImporterRuntimeData data;
		StringListSO sheetsCache;
		SerializedObject sheetsCacheSerializedObject;

		bool downloadInProgress;

		[MenuItem("Fierclash/Tools/CSV Importer")]
		public static void Init()
		{
			var window = (CSVImporterEditorWindow)GetWindow(typeof(CSVImporterEditorWindow),
							false,
							"CSV Importer",
							true);
			window.Show();
			CenterWindow(window);
		}

		void OnEnable()
		{
			_InitCSVImporterData();
			InitSheetsCache();

			void _InitCSVImporterData()
			{
				// Instantiate runtime data
				data = new CSVImporterRuntimeData();

				// Load Settings
				var settings = CSVImporterLoadUtility.ImportSettingsFromConfig();
				CSVImporterValidationUtility.ValidateSettings(settings);

				// Load default data
				CSVImporterEditorWindowUtility.SetProfileIndexToDefault(data);

				// Load data from Settings
				CSVImporterLoadUtility.LoadProfileGUIDsFromSettings(data, settings);
				CSVImporterLoadUtility.LoadGUIDToProfileMapFromSettings(data, settings);

				// Validate loaded data
				CSVImporterValidationUtility.ValidateProfileGUIDs(data);
				CSVImporterValidationUtility.ValidateGUIDToProfileMap(data);
			}
		}

		void OnDisable()
		{
			UpdateSettings();
			DestroyImmediate(sheetsCache);
		}

		void InitSheetsCache()
		{
			if (sheetsCache != null) DestroyImmediate(sheetsCache);
			sheetsCache = ScriptableObject.CreateInstance<StringListSO>();
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile != null) sheetsCache.value = profile.sheets.ToList();
			else sheetsCache.value = new();
			sheetsCacheSerializedObject = new SerializedObject(sheetsCache);
		}

		static void CenterWindow(EditorWindow window)
		{
			Rect main = EditorGUIUtility.GetMainWindowPosition();
			Rect pos = window.position;
			float centerWidth = (main.width - pos.width) * 0.5f;
			float centerHeight = (main.height - pos.height) * 0.5f;
			pos.x = main.x + centerWidth;
			pos.y = main.y + centerHeight;
			window.position = pos;
		}

		public void OnGUI()
		{
			DrawCSVImporterProfileField();
			DrawAssetPathField();
			DrawAssetNamePrefixField();
			DrawDocIDField();
			DrawImportMode();
			DrawSheetsField();
			DrawImportButton();
		}

		void DrawCSVImporterProfileField()
		{
			GUILayout.BeginHorizontal();
			{
				// Popup Field
				if (CSVImporterEditorWindowUtility.IsAnyProfiles(data))
				{
					var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
					string profileName = profile.profileName;
					string newProfileName = EditorGUILayout.DelayedTextField(profileName);
					if (newProfileName != profileName)
					{
						CSVImporterUtility.SetProfileNameAtProfileIndex(data, newProfileName);
						UpdateSettings();
					}
					var profileNames = CSVImporterEditorWindowUtility.GetIndexedProfileNames(data);
					int index = EditorGUILayout.Popup(data.profileIndex, 
													profileNames, 
													GUILayout.Width(20f));
					if (index != data.profileIndex)
					{
						data.profileIndex = index;

						InitSheetsCache(); // Uppdate sheets cache
					}
				}
				else
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.TextField("");
					EditorGUILayout.Popup(0,
										new string[0],
										GUILayout.Width(20f));
					EditorGUI.EndDisabledGroup();
				}

				// Add / Remove Buttons
				if (GUILayout.Button("+", GUILayout.Width(20f)))
				{
					CSVImporterUtility.AddProfileToGUIDToProfileMap(data);
					CSVImporterUtility.SetProfileIndexToLastIndex(data);
					InitSheetsCache(); // Uppdate sheets cache
					UpdateSettings();
				}
				EditorGUI.BeginDisabledGroup(false);
				if (GUILayout.Button("-", GUILayout.Width(20f)))
				{
					CSVImporterUtility.RemoveProfileGUIDAtProfileIndex(data);
					CSVImporterUtility.SetProfileIndex(data, 0);
					InitSheetsCache(); // Uppdate sheets cache
					UpdateSettings();
				}
				EditorGUI.EndDisabledGroup();

			}
			GUILayout.EndHorizontal();
		}

		void DrawAssetPathField()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile == null)
			{
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.BeginHorizontal();
				{
					// Replicate Field as disabled variation
					EditorGUILayout.TextField("Asset Path", "");
					if (GUILayout.Button("Browse", GUILayout.Width(60f))) { }
				}
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(true);
					{
						EditorGUILayout.TextField("Asset Path", profile.assetPath);
					}
					EditorGUI.EndDisabledGroup();
					if (GUILayout.Button("Browse", GUILayout.Width(60f)))
					{
						string defaultFolderPanel = Directory.Exists(profile.assetPath) ? profile.assetPath :
																						Application.dataPath;
						string path = UnityEditor.EditorUtility.OpenFolderPanel("Select Asset Path",
																				defaultFolderPanel,
																				profile.assetPath);
						// Empty string Guard
						if (!string.IsNullOrEmpty(path))
						{
							// Absolute to Relative Path
							if (path.StartsWith(Application.dataPath))
							{
								path = string.Format("Assets{0}/", path.Substring(Application.dataPath.Length));
							}
							// Save path
							CSVImporterUtility.SetAssetPathAtProfileIndex(data, path);
							UpdateSettings();
						}
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		void DrawAssetNamePrefixField()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile == null)
			{
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.BeginHorizontal();
				{
					// Replicate Field as disabled variation
					EditorGUILayout.TextField("Asset Prefix", "");
				}
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					string newAssetPrefix = EditorGUILayout.DelayedTextField("Asset Prefix", 
																			profile.assetPrefix);
					if (newAssetPrefix != profile.assetPrefix)
					{
						CSVImporterUtility.SetAssetPrefixAtProfileIndex(data, newAssetPrefix);
						UpdateSettings();
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		void DrawDocIDField()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile == null)
			{
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.BeginHorizontal();
				{
					// Replicate Field as disabled variation
					EditorGUILayout.TextField("Doc ID", "");
				}
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					string newDocID = EditorGUILayout.DelayedTextField("Doc ID",
																		profile.googleSheetsID);
					if (newDocID != profile.googleSheetsID)
					{
						CSVImporterUtility.SetDocIDAtProfileIndex(data, newDocID);
						UpdateSettings();
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		void DrawSheetsField()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile == null ||
				sheetsCache == null ||
				sheetsCache.value == null ||
				sheetsCacheSerializedObject == null)
			{
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Label("Sheets");
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				var property = sheetsCacheSerializedObject.FindProperty("value");
				EditorGUILayout.PropertyField(property, new GUIContent("Sheets"), true);
				sheetsCacheSerializedObject.ApplyModifiedProperties();

				if (!sheetsCache.value.SequenceEqual(profile.sheets))
				{
					CSVImporterUtility.SetSheetsAtProfileIndex(data, sheetsCache.value.ToArray());
					UpdateSettings();
				}
			}
		}

		void DrawImportMode()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			if (profile == null)
			{
				EditorGUI.BeginDisabledGroup(true);
				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.EnumPopup("Import Mode", CSVImportMode.None);
				}
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUILayout.BeginHorizontal();
				{
					var newMode = (CSVImportMode)EditorGUILayout.EnumPopup("Import Mode", profile.importMode);
					if (newMode != profile.importMode)
					{
						CSVImporterUtility.SetImportModeAtProfileIndex(data, newMode);
						UpdateSettings();
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		void DrawImportButton()
		{
			var profile = CSVImporterUtility.GetProfileFromProfileIndex(data);
			EditorGUI.BeginDisabledGroup(downloadInProgress && // Not currently downloading sheets
										profile != null); // Profile is selected
			{
				if (GUILayout.Button("Import Sheet"))
				{
					// Execute process
					Action handleOnComplete = () =>
					{
						downloadInProgress = false;
						Debug.Log("Finished file processing.");
					};
					EditorCoroutineUtility.StartCoroutine(_IReadFile(profile, handleOnComplete), 
														this);
				}
			}
			EditorGUI.EndDisabledGroup();

			IEnumerator _IReadFile(CSVImporterProfile profile, Action OnComplete)
			{
				// Download and display progress bar in editor
				downloadInProgress = true;
				string title = "GoogleSheets Download";
				string info = string.Format("Downloading files from Google Sheets.");
				var reader = new GoogleSheetsImporter()
				{
					assetPath = profile.assetPath,
					assetPrefix = profile.assetPrefix,
					docID = profile.googleSheetsID,
					sheetBatch = profile.sheets,
					mode = profile.importMode,
				};
				reader.ImportSheet(() => downloadInProgress = false);
				while (downloadInProgress)
				{
					if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(title, info, 0f))
					{
						downloadInProgress = false;
						break;
					}
					yield return null;
				}
				UnityEditor.EditorUtility.ClearProgressBar();
			}
		}

		void UpdateSettings()
		{
			// Validate runtime data
			CSVImporterValidationUtility.ValidateProfileGUIDs(data);
			CSVImporterValidationUtility.ValidateGUIDToProfileMap(data);

			// Import settings file and update based on runtime data.
			// We import the existing file so we preserve untouched data
			// that our loading processes do not affect.
			var settings = CSVImporterLoadUtility.ImportSettingsFromConfig();
			CSVImporterLoadUtility.LoadSettingsProfiles(settings, data);

			// Validate and export settings
			CSVImporterValidationUtility.ValidateSettings(settings);
			CSVImporterLoadUtility.ExportSettingsFromConfig(settings);
		}
	}
}
#endif

#pragma warning restore 4014