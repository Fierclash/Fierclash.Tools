/// #LogicScript

using UnityEngine;

namespace Fierclash.Tools
{
	/// <summary>
	/// Utility functions for math operations.
	/// </summary>
	public static class MathUtility
	{
		/// <summary>
		/// Determines if a value is within an interval.
		/// </summary>
		/// <returns></returns>
		public static bool Within(int index, int min, int max, bool minInclusive = true, bool maxInclusive = true)
		{
			return (min < index && index < max) ||
					(minInclusive && index == min) ||
					(maxInclusive && index == max);
		}

		/// <summary>
		/// Determines if a value is within an interval.
		/// </summary>
		/// <returns></returns>
		public static bool Within(float index, float min, float max, bool minInclusive = true, bool maxInclusive = true)
		{
			return (min < index && index < max) ||
					(minInclusive && index == min) ||
					(maxInclusive && index == max);
		}

		/// <summary>
		/// Converts a row-major int to a coordinate (row, column).
		/// </summary>
		/// <returns></returns>
		public static Vector2Int FromRowMajor(int x, int cols) =>
			new Vector2Int(x / cols, x % cols);

		/// <summary>
		/// Converts a column-major int to a coordinate (row, column).
		/// </summary>
		/// <returns></returns>
		public static Vector2Int FromColumnMajor(int x, int rows) =>
			new Vector2Int(x % rows, x / rows);

		/// <summary>
		/// Converts a coordinate to a row-major or column-major int.
		/// <para>For row-major, pass in (x, y, cols).</para>
		/// <para>For column-major, pass in (x, y, rows).</para>
		/// </summary>
		/// <returns></returns>
		public static int ToMajor(int x, int y, int m) =>
			x * m + y;

		/// <summary>
		/// Converts a polar coordinate (in degrees) to a cartesian coordinate.
		/// </summary>
		/// <returns></returns>
		public static Vector2 ConvertPolarToCartesian(Vector2 polarCoordinate) =>
			ConvertCartesianToPolar(polarCoordinate.x, polarCoordinate.y);

		/// <summary>
		/// Converts a polar coordinate (in degrees) to a cartesian coordinate.
		/// </summary>
		/// <returns></returns>
		public static Vector2 ConvertPolarToCartesian(float radius, float angle)
		{
			float angleRad = angle * Mathf.Deg2Rad; // Convert to rad since Mathf trig functions use rad
			float x = radius * Mathf.Cos(angleRad);
			float y = radius * Mathf.Sin(angleRad);
			Vector2 position = new Vector2(x, y);
			return position;
		}

		/// <summary>
		/// Converts a cartesian coordinate into a polar coordinate (in degrees).
		/// </summary>
		/// <returns></returns>
		public static Vector2 ConvertCartesianToPolar(Vector2 cartestianCoordinate) =>
			ConvertCartesianToPolar(cartestianCoordinate.x, cartestianCoordinate.y);

		/// <summary>
		/// Converts a cartesian coordinate into a polar coordinate (in degrees).
		/// </summary>
		/// <returns></returns>
		public static Vector2 ConvertCartesianToPolar(float x, float y)
		{
			// [1/3/23] TODO: Create general solution
			float radius = Mathf.Sqrt(x * x + y * y);
			float angle = Mathf.Atan2(y, x);
			Vector2 polarCoordinate = new Vector2(radius, angle);
			return polarCoordinate;
		}
	}
}
