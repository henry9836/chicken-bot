using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Attributes
{
	public class RequireVoiceOrBotSpam : CheckBaseAttribute
	{
		private static int m_Count = 0;

		public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var configuration = ctx.Services.GetRequiredService<IConfiguration>();

			var botSpamChannel = configuration.GetSection("Channels").GetValue("bot-spam", 0ul);
			var voiceChannel = configuration.GetSection("Channels").GetValue("voice", 0ul);

			var isBotSpam = botSpamChannel == 0 || ctx.Channel.Id == botSpamChannel;
			var isVoiceChannel = voiceChannel == 0 || ctx.Channel.Id == voiceChannel;

			if (!(isBotSpam || isVoiceChannel) && !help)
			{
				await ctx.TryReactAsync("toothless_no");
				m_Count++;
				m_Count %= 4;

				if (m_Count == 3)
				{
					await ctx.RespondAsync($"Head over to <#{botSpamChannel}> or <#{voiceChannel}>");
				}

			}

			return isBotSpam || isVoiceChannel;
		}
	}
}
