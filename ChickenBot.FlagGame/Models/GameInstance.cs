namespace ChickenBot.FlagGame.Models
{
	public class GameInstance
	{
		public ulong ChannelID { get; }
		public ulong MessageID { get; }
		public string Answer { get; }
		public DateTime Posted { get; }
		public GameInstance(ulong channelID, ulong messageID, string answer)
		{
			ChannelID = channelID;
			MessageID = messageID;
			Answer = answer;
			Posted = DateTime.Now;
		}
	}
}
