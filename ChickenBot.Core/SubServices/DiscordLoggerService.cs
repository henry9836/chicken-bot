using System.Text;
using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
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

		private readonly CommandsNextExtension m_CommandsNext;

		public DiscordLoggerService(DiscordLogger discordLogger, IConfiguration configuration, DiscordClient discord, ILogger<DiscordLogger> logger, CommandsNextExtension commandsNext)
		{
			m_DiscordLogger = discordLogger;
			m_Configuration = configuration;
			m_Discord = discord;
			m_Logger = logger;
			m_LogTimer = new Timer(5000);
			m_LogTimer.Elapsed += OnTimerElapsed;
			m_CommandsNext = commandsNext;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (BotLogChannelID == 0)
			{
				m_Logger.LogWarning("Bot log channel not setup.");
			}

			m_CommandsNext.CommandExecuted += OnCommandExecute;
			m_CommandsNext.CommandErrored += OnCommandedErrored;

			try
			{
				var guild = await m_Discord.GetGuildAsync(GuildID);

				if (guild is not null)
				{
					m_LogChannel = guild.GetChannel(BotLogChannelID);
					m_LogTimer.Start();
				}
				else
				{
					m_Logger.LogWarning("Couldn't find bot log channel!");
				}
			}
			catch (Exception ex)
			{
				m_Logger.LogCritical(ex, "Failed to get bot logging channel. Discord logging is inactive");
			}
		}

		private Task OnCommandedErrored(CommandsNextExtension sender, CommandErrorEventArgs args)
		{
			if (args.Exception is CommandNotFoundException   // Don't log errors for unknown commands
				|| args.Exception is ChecksFailedException)  // Don't log errors for when a user doesn't have perms for a command
			{
				return Task.CompletedTask;
			}

			m_Logger.LogError(args.Exception, "Command {command} ran by {user} errored", args.Command?.Name ?? "N/A", args.Context.User.Username);

			if (args.Exception is ArgumentException)
			{
				Task.Run(async () => await AddConfused(args.Context));
			}

			return Task.CompletedTask;
		}

		private async Task AddConfused(CommandContext ctx)
		{
			var emoji = DiscordEmoji.FromName(ctx.Client, ":question:", false);

			if (emoji is not null)
			{
				await ctx.Message.CreateReactionAsync(emoji);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_CommandsNext.CommandExecuted -= OnCommandExecute;
			m_CommandsNext.CommandErrored -= OnCommandedErrored;

			m_LogTimer.Stop();
			return Task.CompletedTask;
		}

		private Task OnCommandExecute(CommandsNextExtension sender, CommandExecutionEventArgs args)
		{
			m_Logger.LogInformation("User {user} executed command '{command}'", args.Context.User.Username, args.Context.Message.Content);
			return Task.CompletedTask;
		}

		private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
		{
			if (m_LogChannel is null)
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
			if (m_LogChannel is null)
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

		private async Task FireRaisedMessage(LogEvent log, LogEventLevel level)
		{
			if (m_LogChannel is null)
			{
				return;
			}

			var source = new StringWriter();
			log.Properties["rawsource"].Render(source);

			var embed = new DiscordEmbedBuilder()
				.WithTitle(level.ToString())
				.WithDescription(ReformatLog(log.RenderMessage()))
				.WithColor(DiscordColor.Red)
				.AddField("Log Level", log.Level.ToString(), true)
				.AddField("Source", ReformatLog(source, string.Empty), true);

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

					var logLevel = logEvent.Level;

					RunLogSuppressions(logEvent, ref logLevel);


					if (logLevel >= LogEventLevel.Error)
					{
						Task.Run(async () => await FireRaisedMessage(logEvent, logLevel));
					} else if (logLevel == LogEventLevel.Debug)
					{
						continue;
					}

					string level = GetLevelName(logLevel);

					if (logLevel > maxLevel)
					{
						maxLevel = logLevel;
					}

					var log = $"[`{level}`] {ReformatLog(logEvent.RenderMessage())}";
					if (sb.Length + log.Length > 500) // max of 4k characters in description, but limit it to 500 for readability
					{
						floatingLog = log;
						break;
					}

					sb.AppendLine(log);
				}

				if (sb.Length > 8)
				{
					builder.AddEmbed(new DiscordEmbedBuilder()
							.WithColor(GetLogColor(maxLevel))
							.WithDescription(sb.ToString()));
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

		/// <summary>
		/// Reformats some quotes into back ticks, for discord embedding
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private string ReformatLog(string input, string parenthesis = "`")
		{
			return input
				.Replace("`", "\\`")
				.Replace("\'\"", parenthesis)
				.Replace("\"\'", parenthesis)
				.Replace("\"", parenthesis);
		}

		private string ReformatLog(object input, string parenthesis = "`")
		{
			return ReformatLog(input.ToString() ?? string.Empty, parenthesis);
		}

		/// <summary>
		/// Checks for certain types of log messages, can be used to increase or decrease their log level. To suppress some messages or promote others
		/// </summary>
		/// <remarks>
		/// Used to suppress/downgrade certain system log messages, to prevent them from pinging devs
		/// </remarks>
		/// <param name="logEvent">Source log event</param>
		/// <param name="level">The level the log event is being evaluated at</param>
		private void RunLogSuppressions(LogEvent logEvent, ref LogEventLevel level)
		{
			var rendered = logEvent.RenderMessage();
			var error = logEvent.Exception?.Message ?? string.Empty;

			switch (logEvent.Level)
			{
				case LogEventLevel.Fatal:

					if (rendered.Contains("Connection terminated"))
					{
						level = LogEventLevel.Debug;
					}
					break;
				case LogEventLevel.Error:

					if (error.Contains("Could not find a suitable overload"))
					{
						level = LogEventLevel.Warning;
					}
					break;

				case LogEventLevel.Warning:
					if (rendered.Contains("Unknown event") || error.Contains("Unknown event"))
					{
						level = LogEventLevel.Debug;
					}
					break;

				//case LogEventLevel.Information:
				//	if (rendered.Contains("DSharpPlus, version"))
				//	{
				//		level = LogEventLevel.Debug;
				//	}
				//	break;
			}
		}

		#region "Experimental"

		private void CreateAnsiLogMessage(DiscordMessageBuilder builder)
		{
			builder.WithContent("dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd");
			string? floatingLog = null;

			string defaultLogFormat = "\u001b[0;36m";

			for (int i = 0; i < 10; i++) // max of 10 embeds per message
			{
				if (m_DiscordLogger.EventQueue.Count == 0)
				{
					return;
				}

				var sb = new StringBuilder();
				sb.AppendLine("```ansi");

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

					var logLevel = logEvent.Level;

					RunLogSuppressions(logEvent, ref logLevel);

					if (logEvent.Exception != null || logLevel >= LogEventLevel.Error)
					{
						Task.Run(async () => await FireRaisedMessage(logEvent, logLevel));
					}

					string level = GetAnsiLevelName(logLevel);

					if (logLevel > maxLevel)
					{
						maxLevel = logLevel;
					}

					var log = $"\u001b[0;33m[{level}\u001b[0;30m] {defaultLogFormat}{ReformatLogAnsi(logEvent.RenderMessage(), "\u001b[0;34m", defaultLogFormat)}";
					if (sb.Length + log.Length >= 500) // max of 4k characters in description, but limit it to 500 for readability
					{
						floatingLog = log;
						break;
					}

					sb.AppendLine(log);
				}

				if (sb.Length > 8)
				{
					sb.AppendLine("```");

					builder.AddEmbed(new DiscordEmbedBuilder()
							.WithColor(GetLogColor(maxLevel))
							.WithDescription(sb.ToString()));
				}
			}
		}

		private string GetAnsiLevelName(LogEventLevel level)
		{
			switch (level)
			{
				case LogEventLevel.Verbose:
					return "\u001b[0;37mVerbose";
				case LogEventLevel.Debug:
					return "\u001b[0;37mDebug";
				case LogEventLevel.Information:
					return "\u001b[0;37mInfo";
				case LogEventLevel.Warning:
					return "\u001b[0;33mWarn";
				case LogEventLevel.Error:
					return "\u001b[1;31mERROR";
				case LogEventLevel.Fatal:
					return "\u001b[1;31m\u001b[1;47mFATAL";
				default:
					return "?";
			}
		}

		private string ReformatLogAnsi(string input, string format1, string def)
		{
			return input
				.Replace("`", "\\`")
				.Replace("\'\"", format1)
				.Replace("\"\'", format1)
				.Replace("\"", "");
		}

		#endregion

	}
}

