/// #LogicScript

using System;
using System.IO;
using UnityEngine;

namespace Fierclash.Tools
{
	/// <summary>
	/// Utility methods class for working with .json files.
	/// </summary>
	public static class Json
	{
		/// <summary>
		/// Loads data from a .json file stored in a directory.
		/// </summary>
		public static object LoadFromJson(string filePath, Type type)
		{
			// Read file
			StreamReader reader = new StreamReader(filePath);
			string json = reader.ReadToEnd();
			reader.Close();

			// Process .json
			var data = JsonUtility.FromJson(json, type);
			return data;
		}

		/// <summary>
		/// Loads data from a .json file stored in a directory.
		/// </summary>
		public static T LoadFromJson<T>(string filePath) where T : class
		{
			return (T)LoadFromJson(filePath, typeof(T));
		}


		/// <summary>
		/// Stores serializable data into a .json file in a directory.
		/// </summary>
		/// <returns>
		/// True if save process went successfully.
		/// </returns>
		public static bool SaveToJson<T>(T data, string filePath, bool createFile = true, bool formatted = true) where T : class
		{
			// Extension Guard
			if (Path.GetExtension(filePath) != ".json") filePath += ".json";

			// Format to .json
			string json = JsonUtility.ToJson(data, formatted);
			if (string.IsNullOrEmpty(json)) return false;

			// Cannot save as .json if file doesn't exist but cannot create file
			if (!Directory.Exists(filePath) && !createFile) return false;

			// Save as .json
			StreamWriter writer = new StreamWriter(filePath);
			writer.Write(json);
			writer.Close();

			return true;
		}
	}
}
