/// #DataStructScript

using System;
using System.Collections.Generic;

namespace Fierclash.Tools
{
	[System.Serializable]
	internal sealed class SceneProfile
	{
		public string profileGUID;
		public string profileName;
		public string[] sceneGUIDs;

		public SceneProfile() { }

		public SceneProfile(SceneProfile other)
		{
			profileGUID = other.profileGUID;
			profileName = other.profileName;
			if (other.sceneGUIDs != null)
			{
				sceneGUIDs = new string[other.sceneGUIDs.Length];
				Array.Copy(other.sceneGUIDs, sceneGUIDs, sceneGUIDs.Length);	
			}
		}
	}

	[System.Serializable]
	internal sealed class SceneData
	{
		public string path;
		public string name;
	}

	[System.Serializable]
	internal class SceneMenuConfig
	{
		public string settingsGUID;
	}

	internal sealed class SceneMenuRuntimeData
	{
		public int profileIndex;
		public SceneProfile buildSettingsProfile;
		public List<string> profileGUIDs; // Important that ProfileGUIDs are indexable and appendable, so we choose Lists
		public Dictionary<string, SceneProfile> GUIDToSceneProfileMap;
		public Dictionary<string, SceneData> GUIDToSceneDataMap;
	}
}
