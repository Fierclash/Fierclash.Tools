/// #DataStructScript

using System;
using System.Collections.Generic;

namespace Fierclash.Tools
{
	[Serializable]
	internal enum CSVImportMode
	{
		None,
		ImportMain,
		ImportBatch,
	}

	[Serializable]
	internal sealed class CSVImporterProfile
	{
		public string profileGUID;
		public string profileName;
		public string googleSheetsID;
		public string assetPath;
		public string assetPrefix;
		public string[] sheets;
		public CSVImportMode importMode;

		public CSVImporterProfile() { }

		public CSVImporterProfile(CSVImporterProfile other)
		{
			profileGUID = other.profileGUID;
			profileName = other.profileName;
			googleSheetsID = other.googleSheetsID;
			assetPath = other.assetPath;
			assetPrefix = other.assetPrefix;
			if (other.sheets != null)
			{
				int length = other.sheets.Length;
				sheets = new string[length];
				Array.Copy(other.sheets, sheets, length);
			}
			importMode = other.importMode;
		}
	}

	[Serializable]
	internal sealed class CSVImporterSettings
	{
		public CSVImporterProfile[] profiles;
	}

	[Serializable]
	internal sealed class CSVImporterConfig
	{
		public string settingsGUID;
	}

	internal sealed class CSVImporterRuntimeData
	{
		public int profileIndex;
		public List<string> profileGUIDs; // Important that ProfileGUIDs are indexable and appendable, so we choose Lists
		public Dictionary<string, CSVImporterProfile> GUIDToProfileMap;
	}
}
