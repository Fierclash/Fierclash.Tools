/// #UtilityScript

using UnityEngine;

namespace Fierclash.Tools
{
	internal static class Editor
	{
		public static string GetRelativeAssetPath(string absolutePath)
		{
			if (absolutePath.StartsWith(Application.dataPath))
				return "Assets" + absolutePath.Substring(Application.dataPath.Length);
			return absolutePath;
		}
	}
}
