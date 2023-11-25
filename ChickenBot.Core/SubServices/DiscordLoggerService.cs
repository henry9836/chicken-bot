﻿using System.Text;
using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Timer = System.Timers.Timer;

namespace ChickenBot.Core.SubServices
{
	public class DiscordLoggerService : IHostedService
	{
		private ulong BotLogChannelID => m_Configuration.GetSection("Channels")?.GetValue("bot-log", 0ul) ?? 0ul;
		private ulong BotDevRoleID => m_Configuration.GetSection("Roles")?.GetValue("bot-dev", 0ul) ?? 0ul;
		private ulong GuildID => m_Configuration.GetValue("GuildID", 0ul);

		private readonly DiscordLogger m_DiscordLogger;

		private readonly IConfiguration m_Configuration;

		private readonly DiscordClient m_Discord;

		private readonly ILogger<DiscordLogger> m_Logger;

		private DiscordChannel? m_LogChannel;

		private Timer m_LogTimer;

		private readonly SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);

		public DiscordLoggerService(DiscordLogger discordLogger, IConfiguration configuration, DiscordClient discord, ILogger<DiscordLogger> logger)
		{
			m_DiscordLogger = discordLogger;
			m_Configuration = configuration;
			m_Discord = discord;
			m_Logger = logger;
			m_LogTimer = new Timer(5000);
			m_LogTimer.Elapsed += OnTimerElapsed;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (BotLogChannelID == 0)
			{
				m_Logger.LogWarning("Bot log channel not setup.");
			}

			var guild = await m_Discord.GetGuildAsync(GuildID);

			if (guild != null)
			{
				m_LogChannel = guild.GetChannel(BotLogChannelID);
				m_LogTimer.Start();
			}
			else
			{
				m_Discord.GuildAvailable += OnGuildAvailable;
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.GuildAvailable -= OnGuildAvailable;
			m_LogTimer.Stop();

			return Task.CompletedTask;
		}

		private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
		{
			if (m_LogChannel == null)
			{
				return;
			}

			if (m_DiscordLogger.EventQueue.Count == 0)
			{
				return;
			}

			Task.Run(FlushLogsAsync);
		}

		private async Task FlushLogsAsync()
		{
			if (m_LogChannel == null)
			{
				return;
			}

			// Prevent logs being written out-of-order in the event sending messages takes too long
			await m_Semaphore.WaitAsync();
			try
			{
				var message = new DiscordMessageBuilder();
				CreateLogMessage(message);

				await m_LogChannel.SendMessageAsync(message);
			}
			finally
			{
				m_Semaphore.Release();
			}
		}

		private async Task FireRaisedMessage(LogEvent log)
		{
			if (m_LogChannel == null)
			{
				return;
			}

			var source = new StringWriter();
			log.Properties["source"].Render(source);

			var embed = new DiscordEmbedBuilder()
				.WithTitle(log.Level.ToString())
				.WithDescription(log.RenderMessage())
				.WithColor(DiscordColor.Red)
				.AddField("Log Level", log.Level.ToString(), true)
				.AddField("Source", source.ToString(), true);

			if (log.Exception != null)
			{
				embed.AddField("Error", log.Exception.GetType().Name)
									.AddField("Error Message", log.Exception.Message)
									.AddField("Stacktrace", log.Exception.StackTrace ?? "None available");

				if (log.Exception.InnerException != null)
				{
					embed.AddField("Inner Error", log.Exception.InnerException.GetType().Name)
						.AddField("Inner Error Message", log.Exception.InnerException.Message)
						.AddField("Inner Error Stacktrace", log.Exception.InnerException.StackTrace ?? "None available");
				}
			}

			var message = new DiscordMessageBuilder()
				.WithEmbed(embed)
				.WithContent($"<@&{BotDevRoleID}>")
				.WithAllowedMention(new RoleMention(BotDevRoleID));

			await m_LogChannel.SendMessageAsync(message);

		}

		private void CreateLogMessage(DiscordMessageBuilder builder)
		{
			string? floatingLog = null;

			for (int i = 0; i < 10; i++) // max of 10 embeds per message
			{

				if (m_DiscordLogger.EventQueue.Count == 0)
				{
					return;
				}

				var sb = new StringBuilder();

				if (floatingLog != null)
				{
					sb.AppendLine(floatingLog);
				}

				var maxLevel = LogEventLevel.Verbose;

				while (true)
				{
					if (!m_DiscordLogger.EventQueue.TryDequeue(out var logEvent))
					{
						// End of log
						break;
					}

					if (logEvent.Exception != null || logEvent.Level >= LogEventLevel.Error)
					{
						Task.Run(async () => await FireRaisedMessage(logEvent));
					}

					string level = GetLevelName(logEvent.Level);

					if (logEvent.Level > maxLevel)
					{
						maxLevel = logEvent.Level;
					}

					var log = $"[`{level}`] {logEvent.RenderMessage()}";
					if (sb.Length + log.Length > 500) // max of 4k characters in description, but limit it to 500 for readability
					{
						floatingLog = log;
						break;
					}

					sb.AppendLine(log);
				}

				if (sb.Length > 0)
				{

					builder.AddEmbed(new DiscordEmbedBuilder()
					{
						Color = GetLogColor(maxLevel),
						Description = sb.ToString()
					});
				}
			}
		}

		private DiscordColor GetLogColor(LogEventLevel level)
		{
			switch (level)
			{
				case LogEventLevel.Verbose:
					return DiscordColor.LightGray;
				case LogEventLevel.Information:
					return DiscordColor.Gray;
				case LogEventLevel.Warning:
					return DiscordColor.Yellow;
				case LogEventLevel.Error:
					return DiscordColor.Red;
				case LogEventLevel.Fatal:
					return DiscordColor.DarkRed;
				default:
					return DiscordColor.White;
			}
		}

		private string GetLevelName(LogEventLevel level)
		{
			switch (level)
			{
				case LogEventLevel.Verbose:
					return "Verbose";
				case LogEventLevel.Debug:
					return "Debug";
				case LogEventLevel.Information:
					return "Info";
				case LogEventLevel.Warning:
					return "Warn";
				case LogEventLevel.Error:
					return "ERROR";
				case LogEventLevel.Fatal:
					return "FATAL";
				default:
					return "?";
			}
		}

		private Task OnGuildAvailable(DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs args)
		{
			var channel = args.Guild.GetChannel(BotLogChannelID);

			if (channel == null)
			{
				return Task.CompletedTask;
			}

			m_Logger.LogInformation("Bot log channel is now available, starting discord logging service.");
			m_LogChannel = channel;
			m_LogTimer.Start();

			return Task.CompletedTask;
		}
	}
}