﻿using System.Reflection;
using ChickenBot.API.Attributes;
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

					var singleton = type.GetCustomAttribute<SingletonAttribute>();
					var transient = type.GetCustomAttribute<TransientAttribute>();

					if (typeof(IHostedService).IsAssignableFrom(type))
					{
						m_Subcontext.ChildServices.AddSingleton(typeof(IHostedService), implementationType: type);
					}
					if (singleton != null)
					{
						if (singleton.ServiceType != null && !singleton.ServiceType.IsAssignableFrom(type))
						{
							m_Logger.LogError("Couldn't register Singleton service {service}: Service does not implement specified service type {type}", type.Name, singleton.ServiceType.Name);
							continue;
						}

						m_Logger.LogInformation("Registering singleton servive {service}", type.Name);
						m_Subcontext.ChildServices.AddSingleton(serviceType: singleton.ServiceType ?? type, implementationType: type);
					}
					else if (transient != null)
					{
						if (transient.ServiceType != null && !transient.ServiceType.IsAssignableFrom(type))
						{
							m_Logger.LogError("Couldn't register Transient service {service}: Service does not implement specified service type {type}", type.Name, transient.ServiceType.Name);
							continue;
						}

						m_Logger.LogInformation("Registering transient servive {service}", type.Name);
						m_Subcontext.ChildServices.AddTransient(serviceType: transient.ServiceType ?? type, implementationType: type);
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
