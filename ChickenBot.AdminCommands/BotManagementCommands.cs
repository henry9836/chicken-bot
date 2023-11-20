using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ChickenBot.API.Atrributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace ChickenBot.AdminCommands
{
	public class BotManagementCommands : BaseCommandModule
	{
		private static readonly List<string> m_AssignableChannels = new List<string>
		{
			"quotes",
			"verified",
			"nsfw-quotes",
			"petitions"
		};

		private readonly IConfiguration m_Configuration;


		[Command("set-channel"), RequireBotManagerOrAdmin]
		public async Task SetChannelCommand(CommandContext ctx, string channelName, DiscordChannel channel)
		{
			channelName = channelName.ToLowerInvariant();

			if (!m_AssignableChannels.Contains(channelName))
			{
				await ctx.RespondAsync("Unknown channel setting");
				return;
			}
		}





	}
}
