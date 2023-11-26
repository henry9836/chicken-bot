namespace ChickenBot.FlagGame.Models
{
	public delegate Task UpdateMessageArgs(string footer);
	public class GameInstance
	{
		public ulong ChannelID { get; }
		public ulong MessageID { get; }
		public string Answer { get; }
		public DateTime Posted { get; }
		public UpdateMessageArgs UpdateMessage { get; }

		public GameInstance(ulong channelID, ulong messageID, string answer, UpdateMessageArgs updateMessage)
		{
			ChannelID = channelID;
			MessageID = messageID;
			Answer = answer;
			Posted = DateTime.Now;
			UpdateMessage = updateMessage;
		}
	}
}
