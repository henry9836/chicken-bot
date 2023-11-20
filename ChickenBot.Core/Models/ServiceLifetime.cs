using System.Reflection;
using ChickenBot.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChickenBot.Core.Models
{
	public class ServiceLifetime
	{
		public bool IsRoot { get; }
		public bool IsShuttingDown { get; private set; }
		private IServiceProvider m_Provider { get; }

		private readonly CancellationTokenSource m_Source = new CancellationTokenSource();

		private readonly TaskCompletionSource m_Completion = new TaskCompletionSource();

		public ServiceLifetime(IServiceProvider provider, bool root)
		{
			m_Provider = provider;
			IsRoot = root;
		}

		public async Task StartAsync()
		{
			var hostedServices = m_Provider.GetServices<IHostedService>();

			foreach (var service in hostedServices)
			{
				if (!IsRoot && service.GetType().GetCustomAttribute<RootServiceAttribute>() != null)
				{
					continue;
				}

				await service.StartAsync(m_Source.Token);
			}
		}

		public async Task StopAsync()
		{
			IsShuttingDown = true;
			var hostedServices = m_Provider.GetServices<IHostedService>().Reverse();

			foreach (var service in hostedServices)
			{
				if (!IsRoot && service.GetType().GetCustomAttribute<RootServiceAttribute>() != null)
				{
					continue;
				}

				await service.StopAsync(m_Source.Token);
			}
		}

		public void Shutdown()
		{
			m_Completion.SetResult();
		}

		public async Task RunLifetime()
		{
			await StartAsync();
			await m_Completion.Task;
			await StopAsync();
			m_Source.Cancel();
		}
	}
}
