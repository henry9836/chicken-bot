using ChickenBot.API.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Test
{
	[Singleton] // If you want to share an instance between classes, you can register a type as a Singleton to the dependency injection container. This means we can inject this as a service anywhere we want
	public class MyService
	{
		private readonly IConfiguration m_Configuration;
		private readonly ILogger<MyService> m_Logger;

		private int m_TimesCalled = 0;

		// We can also inject other services inside of a service
		public MyService(IConfiguration configuration, ILogger<MyService> logger)
		{
			m_Configuration = configuration;
			m_Logger = logger;
		}

		public void DoSomething()
		{
			m_TimesCalled++;

			m_Logger.LogInformation("This method has been called {count} times", m_TimesCalled);
		}
	}
}
