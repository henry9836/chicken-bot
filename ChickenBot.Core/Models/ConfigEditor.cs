using System.Text.Json.Nodes;
using ChickenBot.API.Interfaces;

namespace ChickenBot.Core.Models
{
	/// <summary>
	/// Service to modify the bot config file during runtime
	/// </summary>
	public class ConfigEditor : IConfigEditor
	{
		public string Config { get; }

		public ConfigEditor(string path)
		{
			Config = path;
		}

		/// <summary>
		/// Inserts/Updates a custom JsonValue, useful for modifying multiple properties or inserting complex types
		/// </summary>
		/// <param name="path">Path to the Json property, separated by colons (:)</param>
		/// <param name="nodeValue">The new value for the property</param>
		/// <returns><see langword="true"/> if it could update the config value</returns>
		public async Task<bool> ModifyConfigNodeAsync(string path, JsonValue nodeValue)
		{
			var jsonText = await File.ReadAllTextAsync(Config);

			var root = JsonNode.Parse(jsonText);

			if (root == null)
			{
				return false;
			}

			if (SetConfigKey(root, path, nodeValue))
			{
				jsonText = root.ToJsonString();

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
		public async Task<bool> ModifyConfigAsync<T>(string path, T value)
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

			if(SetConfigKey(root, path, newValue))
			{
				jsonText = root.ToJsonString();

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

			JsonNode target = root;

			foreach (var node in pathSplit.Take(pathSplit.Length - 1))
			{
				var newTarget = target[node];

				if (newTarget == null)
				{
					newTarget = new JsonObject();

					target[node] = newTarget;
				}

				target = newTarget;
			}

			// Set/Update target property

			var targetObj = target.AsObject();

			if (targetObj.ContainsKey(targetNode))
			{
				targetObj.Remove(targetNode);
			}

			return targetObj.TryAdd(targetNode, value);
		}
	}
}
