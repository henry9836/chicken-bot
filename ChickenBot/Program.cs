using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot
{
	internal class Program
	{
		static Core.Models.ServiceLifetime? m_Lifetime;

		static async Task Main(string[] args)
		{
			// Handle CTRL+C
			Console.CancelKeyPress += OnShutdownRequested;

			var collection = new ServiceCollection();

			Core.Startup.ConfigureLogging(collection);
			Core.Startup.ConfigureServices(collection);

			var services = Startup.ConfigureServices(collection);

			m_Lifetime = new Core.Models.ServiceLifetime(services, true);

			await m_Lifetime.RunLifetime();
		}

		private static void OnShutdownRequested(object? sender, ConsoleCancelEventArgs e)
		{
			if (m_Lifetime != null && !m_Lifetime.IsShuttingDown)
			{
				m_Lifetime.Shutdown();
				e.Cancel = true;
				return;
			}
		}
	}
}