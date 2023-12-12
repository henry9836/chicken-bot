namespace ChickenBot.Music.TrackResolve.Models
{
	public sealed class ResolveFailedException : Exception
	{
		public string? UserMessage { get; }
		public ResolveFailedException(string? userMessage = null) : base("The track resolver couldn't fulfil the resolve request")
		{
			UserMessage = userMessage;
		}
	}
}
