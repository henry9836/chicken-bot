using System.Reflection;

namespace ChickenBot.API.Models
{
	/// <summary>
	/// A container that can be used to dynamically inject services into method parameters. Can be used as a service provider for dynamically activating a type, or dynamically invoking a <seealso cref="MethodInfo"/>
	/// </summary>
	public class ArgumentContainer
	{
		/// <summary>
		/// Service provider, to fetch services from if a required service is not present in named or unnamed parameters
		/// </summary>
		public IServiceProvider? ServiceProvider { get; }

		/// <summary>
		/// Named objects, injected by assignable object type, and parameter name
		/// </summary>
		public Dictionary<string, object> NamedArguments { get; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Unnamed objects, injected by assignable object type
		/// </summary>
		public List<object> UnnamedArguments { get; } = new List<object>();

		/// <summary>
		/// Creates a new argument container, backed by the specified service provider
		/// </summary>
		public ArgumentContainer(IServiceProvider? provider = null)
		{
			ServiceProvider = provider;
		}

		/// <summary>
		/// Adds named arguments to the container
		/// </summary>
		/// <typeparam name="T">The base type of the arguments</typeparam>
		/// <param name="arguments">Key-value mapping of arguments to add</param>
		/// <returns>Current Instance</returns>
		public ArgumentContainer WithNamedArguments<T>(IEnumerable<KeyValuePair<string, T>> arguments)
		{
			foreach (var pair in arguments)
			{
				if (pair.Value is null)
				{
					continue;
				}

				NamedArguments[pair.Key] = pair.Value;
			}
			return this;
		}

		/// <summary>
		/// Adds unnamed arguments to the container
		/// </summary>
		/// <param name="arguments">Arguments to add</param>
		/// <returns>Current instance</returns>
		public ArgumentContainer WithUnnamedArguments(params object[] arguments)
		{
			UnnamedArguments.AddRange(arguments);
			return this;
		}

		/// <summary>
		/// Attempts to create an object array to be used when invoking the specified method
		/// </summary>
		/// <param name="info">The method info to inject services into</param>
		/// <param name="arguments">The resulting services to execute the method</param>
		/// <returns><see langword="true"/> if all parameters of the method could be met</returns>
		public bool TryFormatArguments(MethodInfo info, out object[] arguments)
		{
			var parameters = info.GetParameters();
			arguments = new object[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];

				if (!string.IsNullOrEmpty(parameter.Name)
					&& NamedArguments.TryGetValue(parameter.Name, out var value)
					&& parameter.ParameterType.IsAssignableFrom(value.GetType()))
				{
					arguments[i] = value;
					continue;
				}

				var unnamed = UnnamedArguments.FirstOrDefault(x => parameter.ParameterType.IsAssignableFrom(x.GetType()));

				if (unnamed != null)
				{
					arguments[i] = unnamed;
					continue;
				}

				if (ServiceProvider != null)
				{
					var service = ServiceProvider.GetService(parameter.ParameterType);
					if (service != null)
					{
						arguments[i] = service;
						continue;
					}
				}

				return false;
			}
			return true;
		}
	}
}
