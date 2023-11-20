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
	}
}
