using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.Test
{
	public class TestCommand : BaseCommandModule
	{
		[Command("ping")]
		public async Task PingCommand(CommandContext ctx)
		{
			await ctx.RespondAsync("Hello World!");
		}
	}
}
