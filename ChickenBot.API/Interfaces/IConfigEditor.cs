using System.Text.Json.Nodes;

namespace ChickenBot.API.Interfaces
{
	/// <summary>
	/// Service to modify the bot config file during runtime
	/// </summary>
	public interface IConfigEditor
	{
		/// <summary>
		/// Inserts/Updates a custom JsonValue, useful for modifying multiple properties or inserting complex types
		/// </summary>
		/// <param name="path">Path to the Json property, separated by colons (:)</param>
		/// <param name="nodeValue">The new value for the property</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		Task<bool> ModifyConfigNodeAsync(string path, JsonValue nodeValue);


		/// <summary>
		/// Inserts/Updates a property of the config. If nodes in the path don't exist, they will be created
		/// </summary>
		/// <typeparam name="T">The type of the new value</typeparam>
		/// <param name="path">Path to the Json property, separated by colons (:)</param>
		/// <param name="value">The new value to set the target property to</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		Task<bool> ModifyConfigAsync<T>(string path, T value);
	}
}
