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

		private static readonly List<string> m_AssignableRoles = new List<string>
		{
			"....",
		};


		[Command("set-channel"), RequireBotManagerOrAdmin]
		public async Task SetChannelCommand(CommandContext ctx, string channelName, DiscordChannel channel)
		{
			channelName = channelName.ToLowerInvariant();

			if (!m_AssignableChannels.Contains(channelName))
			{
				await ctx.RespondAsync("Unknown channel setting");
				return;
			}

			UpdateConfigValue($"Channels:{channelName}", channel.Id);

			await ctx.RespondAsync($"Updated channel for {channelName}.");
		}


		[Command("set-role"), RequireBotManagerOrAdmin]
		public async Task SetChannelCommand(CommandContext ctx, string roleName, DiscordRole role)
		{
			roleName = roleName.ToLowerInvariant();

			if (!m_AssignableRoles.Contains(roleName))
			{
				await ctx.RespondAsync("Unknown role setting");
				return;
			}

			UpdateConfigValue($"Roles:{roleName}", role.Id);

			await ctx.RespondAsync($"Updated role for {roleName}.");
		}


		private void UpdateConfigValue(string path, object value)
		{
			// todo

		}

	}
}
