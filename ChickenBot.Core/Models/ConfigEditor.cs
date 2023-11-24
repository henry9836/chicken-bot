using System.Text.Json;
using System.Text.Json.Nodes;
using ChickenBot.API.Interfaces;

namespace ChickenBot.Core.Models
{
	/// <summary>
	/// Service to modify the bot config file during runtime
	/// </summary>
	public class ConfigEditor : IConfigEditor
	{
		/// <summary>
		/// The path to the json config file
		/// </summary>
		public string Config { get; }

		/// <summary>
		/// Serializer settings to modify the format of the resulting updated config file
		/// </summary>
		public JsonSerializerOptions SerializerOptions { get; }

		/// <summary>
		/// Creates a new json config editor for the specified file, with default serializer settings
		/// </summary>
		/// <param name="path">Path to the config file</param>
		public ConfigEditor(string path)
		{
			Config = path;
			SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General)
			{
				WriteIndented = true
			};
		}

		/// <summary>
		/// Creates a new json config editor for the specified file, with the specified serializer settings
		/// </summary>
		/// <param name="path">Path to the json config file</param>
		/// <param name="serializerOptions">Json serializer settings used when modifying the config file</param>
		public ConfigEditor(string path, JsonSerializerOptions serializerOptions)
		{
			Config = path;
			SerializerOptions = serializerOptions;
		}

		/// <summary>
		/// Inserts/Updates a custom JsonValue, useful for modifying multiple properties or inserting complex types
		/// </summary>
		/// <param name="path">Path to the Json property, separated by colons (:)</param>
		/// <param name="nodeValue">The new value for the property</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		public async Task<bool> ModifyConfigAsync(string path, JsonValue nodeValue)
		{
			var jsonText = await File.ReadAllTextAsync(Config);

			var root = JsonNode.Parse(jsonText);

			if (root == null)
			{
				return false;
			}

			if (SetConfigKey(root, path, nodeValue))
			{
				jsonText = root.ToJsonString(SerializerOptions);

				await File.WriteAllTextAsync(Config, jsonText);

				return true;
			}
			return false;
		}

		/// <summary>
		/// Inserts/Updates a property of the config. If nodes in the path don't exist, they will be created
		/// </summary>
		/// <typeparam name="T">The type of the new value</typeparam>
		/// <param name="path">Path to the Json property, separated by colons (:)</param>
		/// <param name="value">The new value to set the target property to</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		public async Task<bool> UpdateValueAsync<T>(string path, T value)
		{
			var jsonText = await File.ReadAllTextAsync(Config);

			var root = JsonNode.Parse(jsonText);

			if (root == null)
			{
				return false;
			}

			var newValue = JsonValue.Create(value);

			if (newValue == null)
			{
				return false;
			}

			if (SetConfigKey(root, path, newValue))
			{
				jsonText = root.ToJsonString(SerializerOptions);

				await File.WriteAllTextAsync(Config, jsonText);

				return true;
			}
			return false;
		}

		/// <summary>
		/// Append an object to an array, creating the array property if needed
		/// </summary>
		/// <typeparam name="T">Type of the object to append</typeparam>
		/// <param name="path">Path to the Json array property</param>
		/// <param name="value">Value to append to the array</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		public async Task<bool> AppendValueAsync<T>(string path, T value)
		{
			var jsonText = await File.ReadAllTextAsync(Config);

			var root = JsonNode.Parse(jsonText);

			if (root == null)
			{
				return false;
			}

			if (AppendValue(root, path, value))
			{
				jsonText = root.ToJsonString(SerializerOptions);

				await File.WriteAllTextAsync(Config, jsonText);

				return true;
			}
			return false;
		}

		/// <summary>
		/// Modifies a JsonNode, iterating down the json tree to the specified parent node, to then add/update a target property
		/// </summary>
		/// <typeparam name="T">Type of the json property</typeparam>
		/// <param name="root">Json root/context node</param>
		/// <param name="path">Path to the target property</param>
		/// <param name="value">Value to set the target property to</param>
		/// <returns<see langword="true"/> if it could modify the json object</returns>
		private bool SetConfigKey(JsonNode root, string path, JsonValue value)
		{
			var pathSplit = path.Split(':');

			var targetNode = pathSplit.Last();

			// Iterate to target node, creating nodes as needed

			JsonNode target = IterateToTarget(root, pathSplit.Take(pathSplit.Length - 1));

			// Set/Update target property

			var targetObj = target.AsObject();

			if (targetObj.ContainsKey(targetNode))
			{
				targetObj.Remove(targetNode);
			}

			return targetObj.TryAdd(targetNode, value);
		}

		/// <summary>
		/// Appends a value to a JSON array
		/// </summary>
		/// <typeparam name="T">Type to append to the array</typeparam>
		/// <param name="root">Root/context json node</param>
		/// <param name="path">Path to the json array property</param>
		/// <param name="value">Value to append to the array</param>
		/// <returns><see langword="true"/> if the value could be appended</returns>
		private bool AppendValue<T>(JsonNode root, string path, T value)
		{
			var pathSplit = path.Split(':');

			var targetNode = pathSplit.Last();

			// Iterate to target node, creating nodes as needed

			JsonNode target = IterateToTarget(root, pathSplit.Take(pathSplit.Length - 1));

			// Check/Create array property

			var property = target[targetNode];

			if (property == null)
			{
				property = new JsonArray();
				target[targetNode] = property;
			}

			// Append value to array

			try
			{
				var array = property.AsArray();

				array.Add(value);
			}
			catch (InvalidOperationException)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Iterates down the Json tree looking for the target node, creating missing parent nodes
		/// </summary>
		/// <param name="root">The root/context node</param>
		/// <param name="path">The path to the json node</param>
		/// <returns>The specified existing or newly created json node</returns>
		private JsonNode IterateToTarget(JsonNode root, IEnumerable<string> path)
		{
			JsonNode target = root;

			foreach (var node in path)
			{
				var newTarget = target[node];

				if (newTarget == null)
				{
					newTarget = new JsonObject();

					target[node] = newTarget;
				}

				target = newTarget;
			}

			return target;
		}
	}
}
