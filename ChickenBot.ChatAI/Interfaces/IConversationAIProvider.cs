namespace ChickenBot.ChatAI.Interfaces
{
	public interface IConversationAIProvider
	{
		Task<IConversationAI> CreateConversation();
	}
}
