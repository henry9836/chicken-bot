using System.Diagnostics;
using ChickenBot.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = ChickenBot.Core.Models.ServiceLifetime;

namespace ChickenBot
{
	internal class Program
	{
		static Core.Models.ServiceLifetime? m_Lifetime;

		static async Task Main(string[] args)
		{
			// Handle CTRL+C
			Console.CancelKeyPress += OnShutdownRequested;

#if DEBUG
			// Run plugin update script if it exists, when running in debug mode
			var updateScript = Path.Combine("plugins", "update.bat");
			if (File.Exists(updateScript))
			{
				var startInfo = new ProcessStartInfo(Path.GetFullPath(updateScript))
				{
					UseShellExecute = false,
					WorkingDirectory = Path.GetFullPath("plugins")
				};

				var process = Process.Start(startInfo)!;

				await process.WaitForExitAsync();
			}
#endif

			var collection = new ServiceCollection();

			Core.Startup.ConfigureLogging(collection);
			Core.Startup.ConfigureServices(collection);

			collection.AddSingleton<ServiceLifetime, ParentServiceLifetime>();

			var services = Startup.ConfigureServices(collection);

			m_Lifetime = services.GetRequiredService<ServiceLifetime>();

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