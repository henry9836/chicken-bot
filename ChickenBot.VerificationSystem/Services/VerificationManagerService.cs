using ChickenBot.VerificationSystem.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Timer = System.Timers.Timer;

namespace ChickenBot.VerificationSystem.Services
{
    /// <summary>
    /// Manages the verification system database, flushing verification data in batches
    /// </summary>
    public class VerificationManagerService : IHostedService
    {
        private readonly ILogger<VerificationManagerService> m_Logger;
        private readonly IVerificationCache m_Cache;

        private readonly Timer m_Timer;

        public VerificationManagerService(ILogger<VerificationManagerService> logger, IVerificationCache cache)
        {
            m_Logger = logger;
            m_Cache = cache;

            m_Timer = new Timer(60000);
            m_Timer.AutoReset = true;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Ensure database table exists
            await m_Cache.Init();

            // Start Timer to flush cache
            m_Timer.Start();
            m_Timer.Elapsed += RunDatabaseSync;

            m_Logger.LogInformation("Verification Module Ready.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // We need to stop the timer, or else it will continue to raise events
            m_Timer.Stop();

            m_Logger.LogDebug("Flushing cache to database...");
            await m_Cache.FlushCacheAsync();

            m_Logger.LogDebug("Verification Module Stopped.");
        }

        private void RunDatabaseSync(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(() => m_Cache.FlushCacheAsync());
        }
    }
}