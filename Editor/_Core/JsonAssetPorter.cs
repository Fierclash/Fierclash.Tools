/// #LogicScript

#if UNITY_EDITOR
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;

namespace Fierclash.Tools
{
	internal class JsonAssetPorter
	{
		public string jsonGUID; 

		public T Import<T>()
		{
			var jsonPath = AssetDatabase.GUIDToAssetPath(jsonGUID);
			var jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
			if (jsonFile == null)
			{
				Debug.LogErrorFormat("Could not locate asset for GUID {0}.", jsonGUID);
				return default;
			}

			var asset = JsonUtility.FromJson<T>(jsonFile.text);
			return asset;
		}

		public void Export<T>(T asset) where T : new()
		{
			if (!typeof(T).IsSerializable && !(typeof(ISerializable).IsAssignableFrom(typeof(T)))) return;
			var jsonPath = AssetDatabase.GUIDToAssetPath(jsonGUID);
			if (string.IsNullOrEmpty(jsonPath))
			{
				Debug.LogErrorFormat("Could not locate asset path for GUID {0}.", jsonGUID);
				return;
			}

			string jsonContent = JsonUtility.ToJson(asset, true);
			StreamWriter writer = new(jsonPath);
			writer.Write(jsonContent);
			writer.Close();
			AssetDatabase.Refresh();
		}
	}
}
#endif
