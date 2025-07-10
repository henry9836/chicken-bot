using System.Reflection;
using ChickenBot.API.Attributes;
using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.Services
{
    [RootService]
    public class DiscordSetupService : IHostedService
    {
        private readonly SubcontextBuilder m_Subcontext;

        private readonly PluginRegistry m_Registry;

        private readonly ILogger<DiscordSetupService> m_Logger;

        private readonly IConfiguration m_Configuration;

        public DiscordSetupService(SubcontextBuilder subcontext, PluginRegistry registry, ILogger<DiscordSetupService> logger, IConfiguration configuration)
        {
            m_Subcontext = subcontext;
            m_Registry = registry;
            m_Logger = logger;
            m_Configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //m_Subcontext.ChildServices.AddSingleton(DiscordClientFactory);
            //m_Subcontext.ChildServices.AddSingleton(CommandsNextFactory);

            // Init discord

            var token = m_Configuration["Token"] ?? throw new Exception("Bot token not set in config!");

            var builder = DiscordClientBuilder.CreateDefault(token, DiscordIntents.All, m_Subcontext.ChildServices);

            builder.UseCommandsNext((cmdNext) =>
            {
            }, new CommandsNextConfiguration()
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefixes = m_Configuration.GetSection("Prefixes")?.Get<string[]>() ?? new[] { "!" }
            });

            // Init event handlers

            builder.ConfigureEventHandlers((events) =>
            {
                var eventHandlerTypes = m_Registry.Plugins
                    .SelectMany(x => x.GetTypes())
                    .Where(x => typeof(IEventHandler).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract);

                var singletonEvents = eventHandlerTypes
                    .Where(x => x.GetCustomAttribute<TransientAttribute>() == null) // Default to singleton, as existing events were built on a singleton model
                    .ToList();

                var transientEvents = eventHandlerTypes
                    .Where(x => x.GetCustomAttribute<TransientAttribute>() != null)
                    .ToList();

                m_Logger.LogInformation("Registering {count} singleton event handlers...", singletonEvents.Count);
                events.AddEventHandlers(singletonEvents, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);

                m_Logger.LogInformation("Registering {count} transient event handlers...", transientEvents.Count);
                events.AddEventHandlers(transientEvents, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);

                foreach (var singletonType in singletonEvents)
                {
                    var aliases = singletonType.GetCustomAttribute<ServiceAliasAttribute>();

                    if (aliases == null)
                    {
                        continue;
                    }

                    foreach (var alias in aliases.AliasTypes)
                    {
                        m_Logger.LogInformation("Registering singleton alias {alias} for event handler {handler}", alias.Name, singletonType.Name);
                        m_Subcontext.ChildServices.Add(CreateServiceAlias(alias, singletonType, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton));
                    }
                }

                foreach (var transientType in transientEvents)
                {
                    var aliases = transientType.GetCustomAttribute<ServiceAliasAttribute>();

                    if (aliases == null)
                    {
                        continue;
                    }

                    foreach (var alias in aliases.AliasTypes)
                    {
                        m_Logger.LogInformation("Registering transient alias {alias} for event handler {handler}", alias.Name, transientType.Name);
                        m_Subcontext.ChildServices.Add(CreateServiceAlias(alias, transientType, Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient));
                    }
                }
            });

            return Task.CompletedTask;
        }

        private ServiceDescriptor CreateServiceAlias(Type alias, Type serviceType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(alias, (serviceProvider) =>
            {
                return serviceProvider.GetRequiredService(serviceType);
            }, lifetime);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
