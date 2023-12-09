﻿using ChickenBot.API;
using DSharpPlus;
using DSharpPlus.Entities;
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
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.GuildMemberAdded -= UserJoined;
			m_Discord.GuildMemberRemoved -= UserLeft;
			return Task.CompletedTask;
		}

		private async Task UserJoined(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs args)
		{
			var created = DateTime.UtcNow - args.Member.CreationTimestamp;

			if (created < TimeSpan.FromDays(1))
			{
				m_Logger.LogWarning("{type} joined: {global} ({handle}). Account created {time} ago (!)", args.Member.IsBot ? "Bot" : "User", args.Member.GlobalName ?? args.Member.Username, args.Member.Username, FormatTime(created));
			}
			else
			{
				m_Logger.LogInformation("{type} joined: {global} ({handle}). Account created {time} ago", args.Member.IsBot ? "Bot" : "User", args.Member.GlobalName ?? args.Member.Username, args.Member.Username, FormatTime(created));
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
							.Replace("$user", args.Member.GlobalName);

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
							.Replace("$user", args.Member.GlobalName);

			var messageBuilder = new DiscordMessageBuilder()
				.WithContent(message)
				.WithAllowedMention(new UserMention(args.Member));

			await joinChannel.SendMessageAsync(messageBuilder);
		}

		private string FormatTime(TimeSpan time)
		{
			var years = Math.Floor(time.TotalDays / 365f);

			if (years >= 1)
			{
				return $"{years} year{years.Pluralize()}";
			}
			else if (time.Days >= 1)
			{
				return $"{time.Days} day{time.Days.Pluralize()}";
			}
			else if (time.Hours >= 1)
			{
				return $"{time.Hours} hour{time.Hours.Pluralize()}";
			}
			else if (time.Minutes >= 1)
			{
				return $"{time.Minutes} minute{time.Minutes.Pluralize()}";
			}
			else
			{
				return $"{time.Seconds} second{time.Seconds.Pluralize()}";
			}
		}
	}
}