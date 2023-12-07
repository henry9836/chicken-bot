using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Attributes
{
	public class RequireBotSpam : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var configuration = ctx.Services.GetRequiredService<IConfiguration>();

			var botSpamChannel = configuration.GetSection("Channels").GetValue("Botspam", 0ul);

			return Task.FromResult(botSpamChannel == 0 || ctx.Channel.Id == botSpamChannel);
		}
	}
}
