namespace ChickenBot.API.Attributes
{
	/// <summary>
	/// Specifies that a class should be automatically registered to the dependency injection container as Transient
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class TransientAttribute : Attribute
	{
		/// <summary>
		/// The service type, or <see langword="null"/> if it should be registered as it's implementation type
		/// </summary>
		public Type? ServiceType { get; }

		/// <summary>
		/// Registers a service as Transient, with the same service and implementation type
		/// </summary>
		public TransientAttribute()
		{
		}

		/// <summary>
		/// Registers a service as Transient, with the specified service type
		/// </summary>
		/// <param name="serviceType"></param>
		public TransientAttribute(Type? serviceType)
		{
			ServiceType = serviceType;
		}
	}

	/// <summary>
	/// Specifies that a class should be automatically registered to the dependency injection container as Transient
	/// </summary>
	/// <typeparam name="T">The registered service type</typeparam>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class TransientAttribute<T> : TransientAttribute
	{
		/// <summary>
		/// Registers a service as Transient, with the specified service type
		/// </summary>
		public TransientAttribute() : base(typeof(T))
		{
		}
	}
}
