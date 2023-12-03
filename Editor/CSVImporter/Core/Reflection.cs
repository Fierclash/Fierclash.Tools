/// #LogicScript

using System;
using System.Linq;
using System.Reflection;

namespace Fierclash.Tools
{
	/// <summary>
	/// Utilities class for reflection-based operations.
	/// </summary>
	/// <remarks>
	/// <seealso cref="VisualNovelEngine.Utility.Reflection"/> 
	/// should be used with caution as <seealso cref="System.Reflection"/> 
	/// may cause performance overhead.
	/// </remarks>
	public static class Reflection
	{
		/// <summary>
		/// Retrieves all types deriving from a base class.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Type[] GetDerivedTypesFromBaseType<T>() where T : class
		{
			Assembly assembly = Assembly.GetAssembly(typeof(T));
			Type baseType = typeof(T);
			Type[] derivedTypes = assembly.GetTypes() // Get all types in assembly
										.Where(baseType.IsAssignableFrom) // Select types that are assignable to base
										.Where(t => baseType != t) // Exclude base type
										.ToArray(); // Cast to array
			return derivedTypes;
		}

		/// <summary>
		/// Creates an instance of a class given its class name.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T CreateInstanceFromName<T>(string name) where T : class
		{
			Assembly assembly = Assembly.GetAssembly(typeof(T));
			Type baseType = typeof(T);
			Type type = assembly.GetTypes()
								.Where(baseType.IsAssignableFrom)
								.FirstOrDefault(t => baseType != t && t.Name.Equals(name));
			
			// Default Guard
			if (type == default) return default; 

			// Use Activator to create an instance of given type
			return (T)Activator.CreateInstance(type);
		}
	}
}
