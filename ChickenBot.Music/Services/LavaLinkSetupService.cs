using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Music.Services
{
	public class LavaLinkSetupService : IHostedService
	{
		private readonly LavalinkExtension m_Lavalink;
		private readonly DiscordClient m_Discord;

		private readonly ILogger<LavaLinkSetupService> m_Logger;

		private readonly IConfiguration m_Configuration;

		private bool m_Configured = false;

		public LavaLinkSetupService(LavalinkExtension lavalink, DiscordClient discord, ILogger<LavaLinkSetupService> logger, IConfiguration configuration)
		{
			m_Lavalink = lavalink;
			m_Discord = discord;
			m_Logger = logger;
			m_Configuration = configuration;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Logger.LogInformation("Music module starting...");
			m_Discord.SessionCreated += OnSessionCreated;
			return Task.CompletedTask;
		}

		private async Task OnSessionCreated(DiscordClient sender, DSharpPlus.EventArgs.SessionReadyEventArgs args)
		{
			if (m_Configured)
			{
				return;
			}

			m_Logger.LogInformation("The music module has been discontinued, and will not work.");
			return;


			m_Logger.LogInformation("Setting up LavaLink...");

			var password = m_Configuration.GetValue<string>("Music:LavaLink:Password");
			var host = m_Configuration.GetValue<string>("Music:LavaLink:Host");
			var port = m_Configuration.GetValue<int>("Music:LavaLink:Port");
			var secure = m_Configuration.GetValue<bool>("Music:LavaLink:Secure");

			if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(host))
			{
				m_Logger.LogError("LavaLink is not configured, the music module will not work");
				return;
			}

			var endpoint = new ConnectionEndpoint(host, port, secure);

			var config = new LavalinkConfiguration()
			{
				Password = password,
				SocketAutoReconnect = true,
				RestEndpoint = endpoint,
				SocketEndpoint = endpoint
			};

			m_Logger.LogInformation("Connecting to the LavaLink server...");
			try
			{
				await m_Lavalink.ConnectAsync(config);
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Failed to connect to the Lavalink server. Music module is currently inoperable. Will retry connection later.");
				return;
			}

			m_Logger.LogInformation("LavaLink connected successfully established. Music module operational.");
			m_Configured = true;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Lavalink.Dispose();
			return Task.CompletedTask;
		}
	}
}
