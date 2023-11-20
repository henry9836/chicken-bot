using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.SubServices
{
	public class DiscordConnectionService : IHostedService
	{
		private readonly DiscordClient m_Discord;

		private readonly ILogger<DiscordConnectionService> m_Logger;

		public DiscordConnectionService(DiscordClient discord, ILogger<DiscordConnectionService> logger)
		{
			m_Discord = discord;
			m_Logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			m_Logger.LogInformation("Connecting to Discord...");
			await m_Discord.ConnectAsync();

			// DSharpPlus does not wait for the connection to be finalized before exiting ConnectAsync
			// Calling something right after can cause Unauthorized errors, if it runs before the socket is finalized
			await Task.Delay(5000);

			m_Logger.LogInformation("Initializing Discord Client...");
			await m_Discord.InitializeAsync();

			//m_Logger.LogInformation("Updating status...");
			await m_Discord.UpdateStatusAsync(new DSharpPlus.Entities.DiscordActivity("Clucking With Excitement", DSharpPlus.Entities.ActivityType.Playing));
			m_Logger.LogInformation("Done");
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			m_Logger.LogInformation("Disconnecting from Discord...");
			await m_Discord.DisconnectAsync();
		}
	}
}
