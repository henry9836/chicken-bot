using ChickenBot.API.Models;
using ChickenBot.Core.Models;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.SubServices
{
    public class CommandsNextService : IHostedService
    {
        private readonly CommandsNextExtension m_CommandsNext;

        private readonly PluginRegistry m_Registry;

        private readonly ILogger<CommandsNextService> m_Logger;

        public CommandsNextService(CommandsNextExtension commandsNext, PluginRegistry registry, ILogger<CommandsNextService> logger)
        {
            m_CommandsNext = commandsNext;
            m_Registry = registry;
            m_Logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Setting up CommandsNext...");

            foreach (var plugin in m_Registry.Plugins)
            {
                m_Logger.LogInformation("Registering commands from {asm}...", plugin.GetName().Name);
                m_CommandsNext.RegisterCommands(plugin);
            }

            m_Logger.LogInformation("Setting up custom help formatter...");
            m_CommandsNext.SetHelpFormatter<ChickenHelpFormatter>();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
