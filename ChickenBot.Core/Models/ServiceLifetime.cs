using System.Reflection;
using ChickenBot.Core.Attributes;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChickenBot.Core.Models
{
	// Bit hack-ey, bit it'll do for now
	public class ParentServiceLifetime : ServiceLifetime
	{
		public ParentServiceLifetime(IServiceProvider provider) : base(provider)
		{
		}

		public override bool IsRoot => true;
	}

	public class ChildServiceLifetime : ServiceLifetime
	{
		public ChildServiceLifetime(IServiceProvider provider) : base(provider)
		{
		}

		public override bool IsRoot => false;
	}


	public abstract class ServiceLifetime
	{
		public abstract bool IsRoot { get; }
		public bool IsShuttingDown { get; private set; }
		private IServiceProvider m_Provider { get; }

		private readonly CancellationTokenSource m_Source = new CancellationTokenSource();

		private readonly TaskCompletionSource m_Completion = new TaskCompletionSource();

		public ServiceLifetime(IServiceProvider provider)
		{
			m_Provider = provider;
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
