using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Attributes
{
	public class RequireBotSpam : CheckBaseAttribute
	{
		public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var configuration = ctx.Services.GetRequiredService<IConfiguration>();

			var botSpamChannel = configuration.GetSection("Channels").GetValue("bot-spam", 0ul);

			var result = botSpamChannel == 0 || ctx.Channel.Id == botSpamChannel;

			if (!result && !help)
			{
				var emoji = DiscordEmoji.FromName(ctx.Client, ":toothless_no:", true);
				if (emoji != null)
				{
					await ctx.Message.CreateReactionAsync(emoji);
				}
			}

			return result;
		}
	}
}
