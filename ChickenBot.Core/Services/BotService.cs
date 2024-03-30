using ChickenBot.API.Interfaces;
using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
using ChickenBot.Core.Models;
using ChickenBot.Core.SubServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceLifetime = ChickenBot.Core.Models.ServiceLifetime;

namespace ChickenBot.Core.Services
{
    [RootService]
    public class BotService : IHostedService
    {
        private readonly SubcontextBuilder m_Subcontext;

        private readonly ILogger<BotService> m_Logger;

        private readonly IConfiguration m_Configuration;

        private readonly PluginRegistry m_Registry;

        private ServiceLifetime? m_Lifetime;

        public BotService(SubcontextBuilder subcontext, ILogger<BotService> logger, IConfiguration configuration, PluginRegistry registry)
        {
            m_Subcontext = subcontext;
            m_Logger = logger;
            m_Configuration = configuration;
            m_Registry = registry;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_Subcontext.ChildServices.AddHostedService<CommandsNextService>();
            m_Subcontext.ChildServices.AddHostedService<DiscordConnectionService>();
            m_Subcontext.ChildServices.AddHostedService<DiscordLoggerService>();
            m_Subcontext.ChildServices.AddHostedService<ProviderInitService>();
            m_Subcontext.ChildServices.AddSingleton<IUserFlagProvider, UserFlagService>();

            var subService = m_Subcontext.BuildSubservice();

            if (subService == null)
            {
                throw new InvalidOperationException("Failed to build subservice provider");
            }

            m_Lifetime = new ChildServiceLifetime(subService);

            m_Logger.LogInformation("Starting bot subservice...");
            await m_Lifetime.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

            if (m_Lifetime == null)
            {
                return;
            }

            m_Logger.LogInformation("Stopping bot subservice...");
            await m_Lifetime.StopAsync();
        }
    }
}
