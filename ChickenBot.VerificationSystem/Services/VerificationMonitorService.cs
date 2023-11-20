using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Timer = System.Timers.Timer;

namespace ChickenBot.VerificationSystem.Services
{
	/// <summary>
	/// Monitors the user verification system, assigning user roles, and flushing the cache when needed
	/// </summary>
	public class VerificationMonitorService : IHostedService
	{
		private readonly DiscordClient m_Discord;
		private readonly ILogger<VerificationMonitorService> m_Logger;
		private readonly IVerificationCache m_Cache;
		private readonly IUserVerifier m_Verifier;

		private readonly Timer m_Timer;

		public VerificationMonitorService(DiscordClient discord, ILogger<VerificationMonitorService> logger, IVerificationCache cache, IUserVerifier verifier)
		{
			m_Discord = discord;
			m_Logger = logger;
			m_Cache = cache;
			m_Verifier = verifier;

			m_Timer = new Timer(5000);
			m_Timer.AutoReset = true;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Ensure database table exists
			await m_Cache.Init();

			// Start Timer to flush cache
			m_Timer.Start();
			m_Timer.Elapsed += RunDatabaseSync;

			// Hook onto message created
			m_Discord.MessageCreated += OnMessageCreated;

			m_Logger.LogInformation("Verification Module Ready.");
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			// We need to stop the timer, or else it will continue to raise events
			m_Timer.Stop();

			m_Logger.LogInformation("Flushing cache to database...");
			await m_Cache.FlushCacheAsync();

			m_Logger.LogInformation("Verification Module Stopped.");
		}

		private void RunDatabaseSync(object? sender, System.Timers.ElapsedEventArgs e)
		{
			Task.Run(() => m_Cache.FlushCacheAsync());
		}

		private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
		{
			if (args.Author is not DiscordMember member)
			{
				// Do not increment messages from DMs to the bot
				return;
			}

			if (!m_Cache.IncrementUserMessages(args.Author.Id))
			{
				// User is not meant to be verified rn
				return;
			}

			if (m_Verifier.CheckUserVerified(member))
			{
				// User is already verified
				return;
			}

			// User is meant to be verified, but isn't

			await m_Verifier.VerifyUserAsync(member);
			await m_Verifier.AnnounceUserVerification(member);
		}
	}
}