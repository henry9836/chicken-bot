using ChickenBot.API;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Services
{
	public class WelcomeService : IHostedService
	{
		private ulong WelcomeChannel => m_Configuration.Value("Channels:join-leave", 0ul);
		private string[] WelcomeFormats => m_Configuration.Value("AutoMsg:JoinFormats", Array.Empty<string>());
		private string[] LeaveFormats => m_Configuration.Value("AutoMsg:LeaveFormats", Array.Empty<string>());

		private readonly DiscordClient m_Discord;

		private readonly IConfiguration m_Configuration;

		private readonly ILogger<WelcomeService> m_Logger;

		private readonly Random m_Random = new Random();

		public WelcomeService(DiscordClient discord, IConfiguration configuration, ILogger<WelcomeService> logger)
		{
			m_Discord = discord;
			m_Configuration = configuration;
			m_Logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Discord.GuildMemberAdded += UserJoined;
			m_Discord.GuildMemberRemoved += UserLeft;
			m_Logger.LogInformation("Welcome service started");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.GuildMemberAdded -= UserJoined;
			m_Discord.GuildMemberRemoved -= UserLeft;
			m_Logger.LogInformation("Welcome service stopped");
			return Task.CompletedTask;
		}

		private async Task UserJoined(DiscordClient sender, GuildMemberAddEventArgs args)
		{
			try
			{
				await HandleUserJoined(args);
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Error handling user joined");
			}
		}

		private async Task HandleUserJoined(GuildMemberAddEventArgs args)
		{
			var created = DateTime.UtcNow - args.Member.CreationTimestamp;

			if (created < TimeSpan.FromDays(1))
			{
				m_Logger.LogWarning("{type} joined: {global} ({handle}). Account created {time} ago (!)", args.Member.IsBot ? "Bot" : "User", args.Member.GlobalName ?? args.Member.Username, args.Member.Username, created.FormatTime());
			}
			else
			{
				m_Logger.LogInformation("{type} joined: {global} ({handle}). Account created {time} ago", args.Member.IsBot ? "Bot" : "User", args.Member.GlobalName ?? args.Member.Username, args.Member.Username, created.FormatTime());
			}

			if (WelcomeChannel == 0 || WelcomeFormats == null || WelcomeFormats.Length == 0)
			{
				return;
			}

			var joinChannel = args.Guild.GetChannel(WelcomeChannel);

			if (joinChannel is null)
			{
				return;
			}

			var format = WelcomeFormats[m_Random.Next(WelcomeFormats.Length)];

			var message = format
							.Replace("@user", args.Member.Mention)
							.Replace("$user", args.Member.GlobalName ?? args.Member.Username);

			var messageBuilder = new DiscordMessageBuilder()
				.WithContent(message)
				.WithAllowedMention(new UserMention(args.Member));

			await joinChannel.SendMessageAsync(messageBuilder);
		}

		private async Task UserLeft(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args)
		{
			m_Logger.LogInformation("{type} left: {global} ({handle})", args.Member.IsBot ? "Bot" : "User", args.Member.GlobalName ?? args.Member.Username, args.Member.Username);

			if (WelcomeChannel == 0 || LeaveFormats == null || LeaveFormats.Length == 0)
			{
				return;
			}

			var joinChannel = args.Guild.GetChannel(WelcomeChannel);

			if (joinChannel is null)
			{
				return;
			}

			var format = LeaveFormats[m_Random.Next(LeaveFormats.Length)];

			var message = format
							.Replace("@user", args.Member.Mention)
							.Replace("$user", args.Member.GlobalName ?? args.Member.Username);

			var messageBuilder = new DiscordMessageBuilder()
				.WithContent(message)
				.WithAllowedMention(new UserMention(args.Member));

			await joinChannel.SendMessageAsync(messageBuilder);
		}

		
	}
}
