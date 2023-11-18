using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.Core.Models
{
	public class SubcontextBuilder
	{
		public ServiceCollection ChildServices { get; } = new ServiceCollection();

		private readonly ILifetimeScope m_Scope;

		public SubcontextBuilder(ILifetimeScope container)
		{
			m_Scope = container;
		}

		public IServiceProvider? BuildSubservice()
		{
			var scope = m_Scope.BeginLifetimeScope(RegisterSubservices);

			if (scope == null)
			{
				return null;
			}

			return new AutofacServiceProvider(scope);
		}

		private void RegisterSubservices(ContainerBuilder builder)
		{
			builder.Populate(ChildServices);
			ChildServices.MakeReadOnly();
		}
	}
}
