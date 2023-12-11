using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Interfaces
{
	/// <summary>
	/// Provides a service configurator definition to allow plugins to dynamically register services to the container before the container runtime
	/// </summary>
	public interface IServiceConfigurator
	{
		/// <summary>
		/// Configures services to the container
		/// </summary>
		/// <param name="services">The service collection that will be used for the bot runtime</param>
		void ConfigureService(IServiceCollection services);
	}
}
