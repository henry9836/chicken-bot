namespace ChickenBot.Core.Attributes
{
	/// <summary>
	/// Marks that an IHostedService should only be called in the root container, I.E., the one declared in ChickenBot:Prorgam.cs
	/// </summary>
	public sealed class RootServiceAttribute : Attribute
	{
	}
}
