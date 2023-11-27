/// #UtilityScript

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Fierclash.Tools
{
	internal static class EditorUtility
	{
		internal static Rect GetInsetRect(float maxWidth, float maxHeight, float xInset, float yInset)
		{
			return new Rect(xInset,
							yInset,
							maxWidth - 2f * xInset,
							maxHeight - 2f * yInset);
		}
	}
}
#endif
