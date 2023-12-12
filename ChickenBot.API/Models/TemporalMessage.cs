using DSharpPlus.Entities;

namespace ChickenBot.API.Models
{
	public struct TemporalMessage : IAsyncDisposable
	{
		public DiscordMessage? Message { get; } = null;

		public TemporalMessage(DiscordMessage? message)
		{
			Message = message;
		}

		public async ValueTask DisposeAsync()
		{
			if (Message is not null)
			{
				await Message.DeleteAsync("Temporal message");
			}
		}
	}
}
