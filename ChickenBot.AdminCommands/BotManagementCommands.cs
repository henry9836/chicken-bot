using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands
{
	[Category("Admin")]
	public class BotManagementCommands : BaseCommandModule
	{
		private readonly IConfigEditor m_ConfigEditor;
		private readonly ILogger<BotManagementCommands> m_Logger;

		public BotManagementCommands(IConfigEditor configEditor, ILogger<BotManagementCommands> logger)
		{
			m_ConfigEditor = configEditor;
			m_Logger = logger;
		}

		private static readonly List<string> m_AssignableChannels = new List<string>
		{
			"quotes",
			"verified",
			"nsfw-quotes",
			"petitions",
			"bot-spam",
			"bot-log",
			"join-leave",
			"voice"
		};

		[Command("set-channel"), RequireBotManagerOrAdmin, Description("Sets a channel in the config")]
		public async Task SetChannelCommand(CommandContext ctx)
		{
			var channels = string.Join(", ", m_AssignableChannels.Select(x => $"`{x}`"));

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Set-Channel")
				.WithDescription($"Sets a channel in the config.\nConfigurable Channels: {channels}\nSet a channel with `set-channel [name] [channel reference]`")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("set-channel"), RequireBotManagerOrAdmin, Description("Sets a channel in the config")]
		public async Task SetChannelCommand(CommandContext ctx, string channelName, DiscordChannel channel)
		{
			channelName = channelName.ToLowerInvariant();

			if (!m_AssignableChannels.Contains(channelName))
			{
				await ctx.RespondAsync("Unknown channel setting");
				return;
			}

			await m_ConfigEditor.UpdateValueAsync($"Channels:{channelName}", channel.Id);

			m_Logger.LogInformation("User updated channel for {name} to {channel} ({channelID})", channelName, channel.Name, channel.Id);

			await ctx.RespondAsync($"Updated channel for {channelName}.");

		}

		private static readonly List<string> m_AssignableRoles = new List<string>()
		{
			"verified",
			"bot-dev"
		};

		[Command("set-role"), RequireBotManagerOrAdmin, Description("Sets a role in the config")]
		public async Task SetRoleCommand(CommandContext ctx)
		{
			var roles = string.Join(", ", m_AssignableRoles.Select(x => $"`{x}`"));

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Set-Role")
				.WithDescription($"Sets a role in the config.\nConfigurable Channels: {roles}\nSet a channel with `set-role [name] [@role mention]`")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("set-role"), RequireBotManagerOrAdmin, Description("Sets a role in the config")]
		public async Task SetRoleCommand(CommandContext ctx, string roleName, DiscordRole role)
		{
			roleName = roleName.ToLowerInvariant();

			if (!m_AssignableRoles.Contains(roleName))
			{
				await ctx.RespondAsync("Unknown role setting");
				return;
			}

			await m_ConfigEditor.UpdateValueAsync($"Roles:{roleName}", role.Id);

			m_Logger.LogInformation("Role updated channel for {name} to {role} ({roleID})", roleName, role.Name, role.Id);

			await ctx.RespondAsync($"Updated channel for {roleName}.");
		}
	}
}
