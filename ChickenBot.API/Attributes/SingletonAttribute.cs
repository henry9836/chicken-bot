﻿namespace ChickenBot.API.Attributes
{
	/// <summary>
	/// Specifies that a class should be automatically registered to the dependency injection container as a Singleton
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class SingletonAttribute : Attribute
	{
		/// <summary>
		/// The service type, or <see langword="null"/> if it should be registered as it's implementation type
		/// </summary>
		public Type? ServiceType { get; }

		/// <summary>
		/// Registers a service as Singleton, with the same service and implementation type
		/// </summary>
		public SingletonAttribute()
		{
		}

		/// <summary>
		/// Registers a service as a Singleton, with the specified service type
		/// </summary>
		/// <param name="serviceType"></param>
		public SingletonAttribute(Type? serviceType)
		{
			ServiceType = serviceType;
		}
	}

	/// <summary>
	/// Specifies that a class should be automatically registered to the dependency injection container as a Singleton
	/// </summary>
	/// <typeparam name="T">The registered service type</typeparam>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class SingletonAttribute<T> : SingletonAttribute
	{
		/// <summary>
		/// Registers a service as Singleton, with the same service and implementation type
		/// </summary>
		public SingletonAttribute() : base(typeof(T))
		{
		}
	}
}
