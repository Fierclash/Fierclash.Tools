/// #UtilityScript

#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Fierclash.Tools
{
	public static class FileUtility
	{
		public static T ReadJsonFromPath<T>(string path)
		{
			try
			{
				string jsonContent = "";
				using (StreamReader reader = new StreamReader(path))
				{
					jsonContent = reader.ReadToEnd();
					reader.Close();
				}
				if (string.IsNullOrEmpty(jsonContent)) return default;
				return JsonUtility.FromJson<T>(jsonContent);
			}
			catch
			{
				return default;
			}
		}

		public static bool WriteJsonToPath<T>(T data, string path, bool prettyPrint = false)
		{
			try
			{
				string jsonContent = JsonUtility.ToJson(data, prettyPrint);
				if (!Directory.Exists(path))
				{
					string directoryPath = Path.GetDirectoryName(path);
					Directory.CreateDirectory(directoryPath);
				}
				using (StreamWriter writer = new StreamWriter(path))
				{
					writer.Write(jsonContent);
					writer.Close();
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
#endif