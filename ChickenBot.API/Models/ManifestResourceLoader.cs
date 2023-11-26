using System.Reflection;
using Newtonsoft.Json;

namespace ChickenBot.API.Models
{
	/// <summary>
	/// Provides some tools to load JSON resources, either from file or from an embedded asset
	/// </summary>
	public static class ManifestResourceLoader
	{
		/// <summary>
		/// Loads the requested resource, from the specified file, or from the specified manifest resource if the file doesn't exist.
		/// </summary>
		/// <remarks>
		/// The assembly to search for the manifest resource is inferred from the stack. This method has to be called from within the assembly that holds the asset
		/// </remarks>
		/// <typeparam name="T">Type to deserialize the asset as</typeparam>
		/// <param name="manifestID">The manifest ID for the embedded resoruce</param>
		/// <param name="fileName">The file to preferably load the resoruce from</param>
		/// <returns>Loaded resoruce, or null</returns>
		public static T? LoadResource<T>(string manifestID, string fileName)
		{
			return LoadResource<T>(Assembly.GetCallingAssembly(), manifestID, fileName);
		}

		/// <summary>
		/// Loads the requested resource, from the specified file, or from the specified manifest resource if the file doesn't exist.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The assembly to search for the manifest resource is inferred from the stack. This method has to be called from within the assembly that holds the asset
		/// </para>
		/// <para>
		/// The resource Manifest ID inferred from the calling assembly name, and specified file name
		/// </para>
		/// </remarks>
		/// <typeparam name="T">Type to deserialize the asset as</typeparam>
		/// <param name="fileName">The file to preferably load the resoruce from</param>
		/// <returns>Loaded resoruce, or null</returns>
		public static T? LoadResource<T>(string fileName)
		{
			var caller = Assembly.GetCallingAssembly();
			var name = caller.GetName().Name;
			var manifestId = $"{name}.{fileName}";

			return LoadResource<T>(Assembly.GetCallingAssembly(), manifestId, fileName);
		}
		/// <summary>
		/// Loads the requested resource, from the specified file, or from the specified manifest resource if the file doesn't exist.
		/// </summary>
		/// <typeparam name="T">Type to deserialize the asset as</typeparam>
		/// <param name="manifestID">The manifest ID for the embedded resoruce</param>
		/// <param name="fileName">The file to preferably load the resoruce from</param>
		/// <param name="assembly">The assembly to scan for the embedded manifest resource</param>
		/// <returns>Loaded resoruce, or null</returns>
		public static T? LoadResource<T>(Assembly assembly, string manifestID, string fileName)
		{
			string json;
			if (File.Exists(fileName))
			{
				// Load via file
				json = File.ReadAllText(fileName);
			}
			else
			{
				// Load via manifest stream

				using var stream = assembly.GetManifestResourceStream(manifestID);

				if (stream == null)
				{
					return default;
				}

				using var reader = new StreamReader(stream);

				json = reader.ReadToEnd();

			}

			// Parse resource
			return JsonConvert.DeserializeObject<T>(json);
		}
	}
}
