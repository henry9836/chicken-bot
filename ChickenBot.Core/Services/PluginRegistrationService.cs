using System.Reflection;
using ChickenBot.API.Atrributes;
using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
using ChickenBot.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.Services
{
	/// <summary>
	/// Registers annotated types in plugin assemblies to the <seealso cref="SubcontextBuilder"/>
	/// </summary>
	[RootService]
	public class PluginRegistrationService : IHostedService
	{
		private readonly SubcontextBuilder m_Subcontext;

		private readonly PluginRegistry m_Registry;

		private readonly ILogger<PluginRegistrationService> m_Logger;

		public PluginRegistrationService(SubcontextBuilder subcontext, PluginRegistry registry, ILogger<PluginRegistrationService> logger)
		{
			m_Subcontext = subcontext;
			m_Registry = registry;
			m_Logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Logger.LogInformation("Registering plugin services...");
			foreach (var plugin in m_Registry.Plugins)
			{
				m_Logger.LogInformation("Registering services from {asm}...", plugin.GetName().Name);

				foreach (var type in plugin.GetExportedTypes())
				{
					if (type.IsAbstract)
					{
						continue;
					}

					if (typeof(IHostedService).IsAssignableFrom(type))
					{
						m_Subcontext.ChildServices.AddSingleton(typeof(IHostedService), implementationType: type);
					}
					else if (type.GetCustomAttribute<SingletonAttribute>() != null)
					{
						m_Logger.LogInformation("Registering singleton servive {service}", type.Name);
						m_Subcontext.ChildServices.AddSingleton(type);
					}
					else if (type.GetCustomAttribute<TransientAttribute>() != null)
					{
						m_Logger.LogInformation("Registering transient servive {service}", type.Name);
						m_Subcontext.ChildServices.AddTransient(type);
					}
				}
			}

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
