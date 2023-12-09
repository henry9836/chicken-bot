using System.Text;
using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands.Commands
{
	[Group("leave-message"), RequireBotManagerOrAdmin]
	public class LeaveMessageCommands : BaseCommandModule
	{
		private readonly IConfigEditor m_ConfigEditor;
		private readonly ILogger<BotManagementCommands> m_Logger;
		private readonly IConfiguration m_Configuration;

		public LeaveMessageCommands(IConfigEditor configEditor, ILogger<BotManagementCommands> logger, IConfiguration configuration)
		{
			m_ConfigEditor = configEditor;
			m_Logger = logger;
			m_Configuration = configuration;
		}

		[GroupCommand, RequireBotManagerOrAdmin]
		public async Task LeaveMessageCommand(CommandContext ctx, [RemainingText] string? _)
		{
			var sb = new StringBuilder();
			var leaveFormats = m_Configuration.Value("AutoMsg:LeaveFormats", Array.Empty<string>());

			for (int i = 0; i < leaveFormats.Length; i++)
			{
				sb.AppendLine($"{i}: `{leaveFormats[i]}`");
			}

			if (leaveFormats.Length == 0)
			{
				sb.Append($"*No formats to display.*");
			}

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Leave Messages")
				.WithDescription("Adds a leave message format\n" +
								 "`leave-message add [Leave message format...]`\n" +
								 "`leave-message remove [Message ID]`\n" +
								 "Available variables:\n" +
								 "* `@user` Mentions the user\n" +
								 "* `$user` Inserts the user's username\n" +
								 "### Current leave messages\n" +
								 $"{sb}")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("add"), RequireBotManagerOrAdmin]
		public async Task AddLeaveMessageCommand(CommandContext ctx, [RemainingText] string format)
		{
			if (string.IsNullOrWhiteSpace(format))
			{
				return;
			}
			await m_ConfigEditor.AppendValueAsync("AutoMsg:LeaveFormats", format);

			await ctx.RespondAsync($"Leave message format added");
		}

		[Command("remove"), RequireBotManagerOrAdmin]
		public async Task AddLeaveMessageCommand(CommandContext ctx, int index)
		{
			var leaveFormats = m_Configuration.Value("AutoMsg:LeaveFormats", Array.Empty<string>());

			if (index < 0 || index >= leaveFormats.Length)
			{
				await ctx.RespondAsync($"Invalid leave message ID");
				return;
			}

			var formats = leaveFormats.ToList();

			var format = formats[index];
			formats.RemoveAt(index);

			await m_ConfigEditor.UpdateValueAsync("AutoMsg:LeaveFormats", formats);

			await ctx.RespondAsync($"Removed format `{format}`");
		}
	}
}
