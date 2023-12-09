using System.Text;
using ChickenBot.API;
using ChickenBot.API.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChickenBot.API.Attributes;
namespace ChickenBot.AdminCommands.Commands
{
	[Group("join-message"), RequireBotManagerOrAdmin]
	public class WelcomeMessageCommands : BaseCommandModule
	{
		private readonly IConfigEditor m_ConfigEditor;
		private readonly ILogger<BotManagementCommands> m_Logger;
		private readonly IConfiguration m_Configuration;

		public WelcomeMessageCommands(IConfigEditor configEditor, ILogger<BotManagementCommands> logger, IConfiguration configuration)
		{
			m_ConfigEditor = configEditor;
			m_Logger = logger;
			m_Configuration = configuration;
		}

		[GroupCommand, RequireBotManagerOrAdmin]
		public async Task WelcomeMessageCommand(CommandContext ctx, [RemainingText] string? r)
		{
			var sb = new StringBuilder();
			var joinFormats = m_Configuration.Value("AutoMsg:JoinFormats", Array.Empty<string>());

			for (int i = 0; i < joinFormats.Length; i++)
			{
				sb.AppendLine($"{i}: `{joinFormats[i]}`");
			}

			if (joinFormats.Length == 0)
			{
				sb.Append($"*No formats to display.*");
			}

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Join Messages")
				.WithDescription("Adds a welcome message format\n" +
								 "`join-message add [Welcome message format...]`\n" +
								 "`join-message remove [Message ID]`\n" +
								 "Available variables:\n" +
								 "* `@user` Mentions the user\n" +
								 "* `$user` Inserts the user's username\n" +
								 "### Current join messages\n" +
								 $"{sb}")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("add"), RequireBotManagerOrAdmin]
		public async Task AddWelcomeMessageCommand(CommandContext ctx, [RemainingText] string format)
		{
			if (string.IsNullOrWhiteSpace(format))
			{
				return;
			}
			await m_ConfigEditor.AppendValueAsync("AutoMsg:JoinFormats", format);

			await ctx.RespondAsync($"Join message format added");
		}

		[Command("remove"), RequireBotManagerOrAdmin]
		public async Task AddWelcomeMessageCommand(CommandContext ctx, int index)
		{
			var joinFormats = m_Configuration.Value("AutoMsg:JoinFormats", Array.Empty<string>());

			if (index < 0 || index >= joinFormats.Length)
			{
				await ctx.RespondAsync($"Invalid join message ID");
				return;
			}

			var formats = joinFormats.ToList();

			var format = formats[index];
			formats.RemoveAt(index);

			await m_ConfigEditor.UpdateValueAsync("AutoMsg:JoinFormats", formats);

			await ctx.RespondAsync($"Removed format `{format}`");
		}
	}
}
