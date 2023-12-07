using System.Text.Json.Nodes;
using DSharpPlus.CommandsNext;

namespace ChickenBot.Fun
{
	public static class Extensions
	{
		public static async Task RespondRandom(this CommandContext ctx, params string[] messages)
		{
			var random = new Random();

			var index = random.Next(0, messages.Length);

			await ctx.RespondAsync(messages[index]);
		}

		public static T? JsonRead<T>(this string json, string path)
		{
			var node = JsonNode.Parse(json);

			var split = path.Split(":");

			if (node == null)
			{
				return default;
			}

			var selected = node;

			foreach (var obj in split)
			{
				selected = selected[obj];
				if (selected == null)
				{
					return default;
				}
			}

			return selected.GetValue<T>();
		}
	}
}
