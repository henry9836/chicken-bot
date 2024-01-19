using DSharpPlus.Entities;

namespace ChickenBot.ChatAI.Interfaces
{
	public interface IConversationAI
	{
		/// <summary>
		/// Lazy method to push a chat message to the conversaiton
		/// </summary>
		/// <remarks>
		/// Chat messages pushed here are subject to message discriminators, and might be rejected from the chat context
		/// </remarks>
		Task PushChatMessage(DiscordMember user, string message);

		/// <summary>
		/// Gets a response as the chicken to the current chat feed
		/// </summary>
		/// <returns>Message, or null if the AI decided to stay silent</returns>
		Task<string?> GetResponseAsync();

		/// <summary>
		/// Gets a response as the chicken to a custom message
		/// </summary>
		/// <returns>Message, or null if the AI decided to stay silent</returns>
		Task<string?> GetManualResponseAsync(DiscordMember user, string message);

		/// <summary>
		/// Sets or replaces the prompt for this conversation
		/// </summary>
		void SetPrompt(string prompt);
	}
}
