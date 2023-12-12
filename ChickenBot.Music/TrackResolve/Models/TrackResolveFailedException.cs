namespace ChickenBot.Music.TrackResolve.Models
{
	public sealed class TrackResolveFailedException : Exception
	{
		public string? UserMessage { get; }
		public TrackResolveFailedException(string? userMessage = null) : base("The track resolver couldn't fulfil the resolve request")
		{
			UserMessage = userMessage;
		}
	}
}
