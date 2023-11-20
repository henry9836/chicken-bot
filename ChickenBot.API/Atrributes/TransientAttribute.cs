namespace ChickenBot.API.Atrributes
{
	/// <summary>
	/// Specifies that a class should be automatically registered to the dependency injection container as Transient
	/// </summary>
	public sealed class TransientAttribute : Attribute
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
}
