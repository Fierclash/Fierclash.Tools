/// #EditorScript

#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.IMGUI.Controls;
using System;

namespace Fierclash.Tools
{
	internal sealed class SceneMenuEditorWindow : EditorWindow
	{
		MultiColumnHeaderState multiColumnHeaderState;
		MultiColumnHeader multiColumnHeader;
		MultiColumnHeaderState.Column[] columns;

		SceneMenuRuntimeData sceneMenuData;

		static readonly float nameLabelMinWidth = 200f;
		static readonly float buttonWidth = 40f;
		Vector2 scrollPosition;
		Color[] rowColors = new Color[2]
		{
			new Color(.2f, .2f, .2f, 1f),
			new Color(.3f, .3f, .3f, 1f),
		};

		[MenuItem("Fierclash/Tools/Scene Menu")]
		public static SceneMenuEditorWindow Open()
		{
			var window = (SceneMenuEditorWindow)GetWindow(typeof(SceneMenuEditorWindow),
															false,
															"Scene Menu",
															true);

			window.Show();
			return window;
		}

		void OnEnable()
		{
			InitColumns();
			_InitSceneMenuData();

			void _InitSceneMenuData()
			{
				// Instantiate runtime data
				sceneMenuData = new SceneMenuRuntimeData();

				// Load Settings
				var settings = SceneMenuLoadUtility.ImportSettingsFromConfig();
				SceneMenuValidationUtility.ValidateSettings(settings);

				// Load default data
				SceneMenuEditorWindowUtility.SetProfileIndexToBuildSettings(sceneMenuData);

				// Load data from Build Settings
				SceneMenuLoadUtility.LoadBuildSettingsProfile(sceneMenuData);
				SceneMenuLoadUtility.LoadGUIDToSceneDataMapFromBuildSettings(sceneMenuData);

				// Load data from Settings
				SceneMenuLoadUtility.LoadProfileGUIDsFromSettings(sceneMenuData, settings);
				SceneMenuLoadUtility.LoadGUIDToSceneProfileMapFromSettings(sceneMenuData, settings);
				SceneMenuLoadUtility.LoadGUIDToSceneDataMapFromSettings(sceneMenuData, settings);

				// Validate loaded data
				SceneMenuValidationUtility.ValidateBuildSettingsProfile(sceneMenuData);
				SceneMenuValidationUtility.ValidateGUIDToSceneProfileMap(sceneMenuData);
				SceneMenuValidationUtility.ValidateGUIDToSceneDataMap(sceneMenuData);
			}
		}

		void OnDisable()
		{
			UpdateSettings();
		}

		void InitColumns()
		{
			this.columns = new MultiColumnHeaderState.Column[]
			{
				// Name Label
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false, // At least one column must be there.
					autoResize = true,
					minWidth = nameLabelMinWidth,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("Scene", "Name of the scene."),
					headerTextAlignment = TextAlignment.Left,
				},
				// Open Scene
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false,
					autoResize = false,
					minWidth = buttonWidth,
					maxWidth = buttonWidth,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("Open", "Open the scene and removes all other open scenes."),
					headerTextAlignment = TextAlignment.Center,
				},
				// Open Scene Additively
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false,
					autoResize = false,
					minWidth = buttonWidth,
					maxWidth = buttonWidth,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("+", "Open the scene additively."),
					headerTextAlignment = TextAlignment.Center,
				},
				// Remove Scene Additively
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false,
					autoResize = false,
					minWidth = buttonWidth,
					maxWidth = buttonWidth,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("-", "Close the scene additively."),
					headerTextAlignment = TextAlignment.Center,
				},
			};

			this.multiColumnHeaderState = new MultiColumnHeaderState(columns: this.columns);

			this.multiColumnHeader = new MultiColumnHeader(state: this.multiColumnHeaderState);

			// When we change visibility of the column we resize columns to fit in the window.
			this.multiColumnHeader.visibleColumnsChanged += (multiColumnHeader) => multiColumnHeader.ResizeToFit();

			// Initial resizing of the content.
			this.multiColumnHeader.ResizeToFit();
		}

		void OnGUI()
		{
			DrawProfileOptions();
			DrawSceneTable();
		}

		void DrawProfileOptions()
		{
			int index = SceneMenuUtility.GetProfileIndex(sceneMenuData);
			bool isBuildSettingIndex = SceneMenuEditorWindowUtility.CheckForBuildSettingsIndex(sceneMenuData);
			string profileName = SceneMenuUtility.GetProfileNameFromProfileIndex(sceneMenuData);

			GUILayout.BeginHorizontal();
			{
				if (isBuildSettingIndex)
				{
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.TextField("Build Settings");
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					string newProfileName = EditorGUILayout.DelayedTextField(profileName);
					if (newProfileName != profileName)
					{
						SceneMenuUtility.SetProfileNameAtProfileIndex(sceneMenuData, newProfileName);
						UpdateSettings();
					}
				}
				// Popup Field
				{
					var profileNames = SceneMenuEditorWindowUtility.GetIndexedProfileNames(sceneMenuData)
																	.ToList();
					profileNames.Insert(0, "Build Settings");
					int newIndex = EditorGUILayout.Popup(index + 1,
												profileNames.ToArray(), 
												GUILayout.Width(20f)) - 1; // Account for Build Settings Option offset
					if (newIndex != index)
					{
						SceneMenuUtility.SetProfileIndex(sceneMenuData, newIndex);
						UpdateSettings();
					}
				}
				if (GUILayout.Button("+", GUILayout.Width(20f)))
				{
					SceneMenuUtility.AddProfileToGUIDToSceneProfileMap(sceneMenuData);
					SceneMenuUtility.SetProfileIndexToLastIndex(sceneMenuData);
					UpdateSettings();
				}
				EditorGUI.BeginDisabledGroup(SceneMenuUtility.GetProfileIndex(sceneMenuData) < 0);
				if (GUILayout.Button("-", GUILayout.Width(20f)))
				{
					SceneMenuUtility.RemoveProfileGUIDAtProfileIndex(sceneMenuData);
					SceneMenuUtility.SetProfileIndex(sceneMenuData, -1);
					UpdateSettings();
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndHorizontal();
		}

		void DrawSceneTable()
		{
			// After compilation and some other events data of the window is lost if it's not saved in some kind of container. Usually those containers are ScriptableObject(s).
			if (this.multiColumnHeader == null) InitColumns();

			// Basically we just draw something. Empty space. Which is `FlexibleSpace` here on top of the window.
			// We need this for - `GUILayoutUtility.GetLastRect()` because it needs at least 1 thing to be drawn before it.
			//GUILayout.FlexibleSpace();

			// Get automatically aligned rect for our multi column header component.
			Rect windowRect = GUILayoutUtility.GetLastRect();

			// Here we are basically assigning the size of window to our newly positioned `windowRect`.
			windowRect.y += EditorGUIUtility.singleLineHeight;
			windowRect.width = this.position.width;
			windowRect.height = this.position.height - EditorGUIUtility.singleLineHeight;

			float columnHeight = EditorGUIUtility.singleLineHeight;

			// This is a rect for our multi column table.
			Rect columnRectPrototype = new Rect(source: windowRect)
			{
				height = columnHeight, // This is basically a height of each column including header.
			};

			// Just enormously large view if you want it to span for the whole window. This is how it works [shrugs in confusion].
			Rect positionalRectAreaOfScrollView = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);

			// Create a `viewRect` since it should be separate from `rect` to avoid circular dependency.
			Rect viewRect = new Rect(source: windowRect)
			{
				xMax = this.columns.Sum((column) => column.width) // Scroll max on X is basically a sum of width of columns.
			};

			// Scene Menu Data
			int index = SceneMenuUtility.GetProfileIndex(sceneMenuData);
			bool isBuildSettingsIndex = SceneMenuEditorWindowUtility.CheckForBuildSettingsIndex(sceneMenuData);


			this.scrollPosition = GUI.BeginScrollView(
				position: positionalRectAreaOfScrollView,
				scrollPosition: this.scrollPosition,
				viewRect: viewRect,
				alwaysShowHorizontal: false,
				alwaysShowVertical: true
			);
			{
				// Draw header for columns here.
				this.multiColumnHeader.OnGUI(rect: columnRectPrototype, xScroll: 0.0f);

				//int rows = EditorBuildSettings.scenes.Length;
				var profile = isBuildSettingsIndex ? sceneMenuData.buildSettingsProfile :
														SceneMenuUtility.GetSceneProfileFromProfileIndex(sceneMenuData);
				int rows = profile.sceneGUIDs.Length;
				for (int row = 0; row < rows; row++)
				{
					//! We draw each type of field here separately because each column could require a different type of field as seen here.
					// This can be improved if we want to have a more robust system. Like for example, we could have logic of drawing each field moved to object itself.
					// Then here we would be able to just iterate through array of these objects and call a draw methods for these fields and use this window for many types of objects.
					// But example with such a system would be too complicated for gamedev.stackexchange, so I have decided to not overengineer and just use hard coded indices for columns - `columnIndex`.

					Rect rowRect = new Rect(source: columnRectPrototype);

					rowRect.y += columnHeight * (row + 1);

					// Draw a texture before drawing each of the fields for the whole row.
					EditorGUI.DrawRect(rect: rowRect, color: rowColors[row % rowColors.Length]);

					// Get Scene data
					string profileGUID = profile.profileGUID;
					string sceneGUID = profile.sceneGUIDs[row];
					SceneData sceneData = null;
					if (!sceneMenuData.GUIDToSceneDataMap.TryGetValue(sceneGUID, out sceneData) ||
						sceneData == null)
					{
						UpdateSettings(); // Validate and update settings
						break;
					}
					string scenePath = sceneData.path;
					string sceneName = sceneData.name;
					Color nameTextColor = SceneMenuEditorWindowUtility.CheckForSceneIsActive(scenePath) ?
											Color.white :
											Color.gray;
					
					_DrawNameColumn(row);
					_DrawOpenColumn();
					_DrawOpenAdditivelyColumn();
					_DrawCloseAdditivelyColumn();


					// Name field
					void _DrawNameColumn(int row)
					{
						int columnIndex = 0;

						if (this.multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
						{
							int visibleColumnIndex = this.multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);

							Rect columnRect = this.multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);

							// This here basically is a row height, you can make it any value you like. Or you could calculate the max field height here that your object has and store it somewhere then use it here instead of `EditorGUIUtility.singleLineHeight`.
							// We move position of field on `y` by this height to get correct position.
							columnRect.y = rowRect.y;

							GUIStyle nameFieldGUIStyle = new GUIStyle(GUI.skin.label)
							{
								padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2),
								normal = new GUIStyleState()
								{
									textColor = nameTextColor
								}
							};

							if (!isBuildSettingsIndex)
							{
								Event current = Event.current;
								if (columnRect.Contains(current.mousePosition) && 
									current.type == EventType.ContextClick)
								{
									GenericMenu menu = new GenericMenu();

									menu.AddItem(new GUIContent("Delete"), false, _OnDelete);
									menu.ShowAsContext();

									current.Use();

									void _OnDelete()
									{
										SceneMenuUtility.RemoveSceneAtProfileIndex(sceneMenuData, index);
										UpdateSettings();
									}
								}
							}

							EditorGUI.LabelField(
								position: this.multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect),
								label: new GUIContent(sceneName),
								style: nameFieldGUIStyle
							);
						}
					}

					// Open Column
					void _DrawOpenColumn()
					{
						int columnIndex = 1;
						if (this.multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
						{
							int visibleColumnIndex = this.multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);

							Rect columnRect = this.multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);

							columnRect.y = rowRect.y;

							GUIStyle nameFieldGUIStyle = new GUIStyle(GUI.skin.label)
							{
								padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2)
							};

							if (GUI.Button(
								position: this.multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect),
								content: new GUIContent("Open")
								))
							{
								EditorSceneManager.SaveOpenScenes();
								EditorSceneManager.OpenScene(scenePath);
							}
						}
					}

					// Open Additively Column
					void _DrawOpenAdditivelyColumn()
					{
						int columnIndex = 2;
						if (this.multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
						{
							int visibleColumnIndex = this.multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);

							Rect columnRect = this.multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);

							columnRect.y = rowRect.y;

							GUIStyle nameFieldGUIStyle = new GUIStyle(GUI.skin.label)
							{
								padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2)
							};


							var oldColor = GUI.backgroundColor;
							GUI.backgroundColor = new Color(.5f, 1f, .5f, 1f);
							if (GUI.Button(
								position: this.multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect),
							content: new GUIContent("+")
								))
							{
								EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
							}
							GUI.backgroundColor = oldColor;
						}
					}

					// Open Additively Column
					void _DrawCloseAdditivelyColumn()
					{
						int columnIndex = 3;
						if (this.multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
						{
							int visibleColumnIndex = this.multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);

							Rect columnRect = this.multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);

							columnRect.y = rowRect.y;

							GUIStyle nameFieldGUIStyle = new GUIStyle(GUI.skin.label)
							{
								padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2)
							};

							var oldColor = GUI.backgroundColor;
							GUI.backgroundColor = new Color(1f, .5f, .5f, 1f);
							if (GUI.Button(
								position: this.multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect),
							content: new GUIContent("-")
								))
							{
								var scene = EditorSceneManager.GetSceneByPath(scenePath);
								EditorSceneManager.SaveScene(scene);
								EditorSceneManager.CloseScene(scene, true);
							}
							GUI.backgroundColor = oldColor;
						}
					}
				}
				
				if (!isBuildSettingsIndex)
				{
					Rect rowRect1 = new Rect(source: columnRectPrototype);
					Rect rowRect2 = new Rect(source: columnRectPrototype);
					rowRect2.y = columnHeight * (rows + 2) + 5f;

					rowRect1.y = columnHeight * (rows + 2) + 5f;
					rowRect1.height = 18f;
					rowRect1.width = columns[0].width;
					// Draw a texture before drawing each of the fields for the whole row.
					EditorGUI.DrawRect(rect: rowRect2, color: rowColors[(rows + 2) % rowColors.Length]);

					//var insertedObj = EditorGUI.ObjectField(rowRect1, obj, typeof(Scene), false);
					UnityEngine.Object obj = null;
					obj = EditorGUI.ObjectField(rowRect1, obj, typeof(SceneAsset), false);
					if (obj != null)
					{
						string sceneGUID = SceneMenuEditorWindowUtility.GetSceneAssetGUID(obj);
						SceneMenuUtility.AddSceneToProfileAtProfileIndex(sceneMenuData, sceneGUID);
						UpdateSettings();
					}
				}
			}

			GUI.EndScrollView(handleScrollWheel: true);
		}

		void UpdateSettings()
		{
			// Validate runtime data
			SceneMenuValidationUtility.ValidateBuildSettingsProfile(sceneMenuData);
			SceneMenuValidationUtility.ValidateGUIDToSceneProfileMap(sceneMenuData);
			SceneMenuValidationUtility.ValidateGUIDToSceneDataMap(sceneMenuData);

			// Import settings file and update based on runtime data.
			// We import the existing file so we preserve untouched data
			// that our loading processes do not affect.
			var settings = SceneMenuLoadUtility.ImportSettingsFromConfig();
			SceneMenuLoadUtility.LoadSettingsProfiles(settings, sceneMenuData);

			// Validate and export settings
			SceneMenuValidationUtility.ValidateSettings(settings);
			SceneMenuLoadUtility.ExportSettingsFromConfig(settings);
		}
	}
}
#endif
