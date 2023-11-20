using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;
using OpenAI_API.Chat;

namespace ChickenBot.ChatAI.Models
{
	public class ConversationAI
	{
		private readonly List<ChatMessage> m_Messages = new List<ChatMessage>();

		private readonly ChatSettings m_Settings;

		private readonly IChatEndpoint m_Endpoint;

		private readonly IMessageDiscriminator m_Discriminator;

		private readonly Dictionary<ulong, int> m_UserChatIDs = new Dictionary<ulong, int>();

		private int m_ChatIDIndex = 0;

		public ConversationAI(ChatSettings requestTemplate, IChatEndpoint endpoint, IMessageDiscriminator discriminator)
		{
			m_Settings = requestTemplate;
			m_Endpoint = endpoint;
			m_Discriminator = discriminator;
		}

		public void PushChatMessage(DiscordUser user, string message)
		{
			if (user.IsBot || !m_Discriminator.Discriminate(user, message))
			{
				// Ignore message
				return;
			}

			string userChatID;

			if (m_Settings.UseNumericNames)
			{
				if (!m_UserChatIDs.TryGetValue(user.Id, out var chatID))
				{
					chatID = m_ChatIDIndex++;
					m_UserChatIDs[user.Id] = chatID;
				}
				userChatID = chatID.ToString();
			} else
			{
				userChatID = user.Username;

				if (userChatID.Length > m_Settings.MaxUsernameLength)
				{
					userChatID = userChatID.Substring(0, m_Settings.MaxUsernameLength);
				}

			}

			var msg = new ChatMessage(ChatMessageRole.User, message)
			{
				Name = userChatID
			};

			AppendMessage(msg);
		}

		private ChatRequest CreateRequest()
		{
			return new ChatRequest()
			{
				FrequencyPenalty = m_Settings.FrequencyPenalty,
				LogitBias = m_Settings.LogitBiases,
				MaxTokens = m_Settings.MaxResponseTokens,
				Model = m_Settings.Model,
				MultipleStopSequences = m_Settings.StopSequences,
				NumChoicesPerMessage = 1,
				PresencePenalty = m_Settings.PresencePenalty,
				Temperature = m_Settings.Temerature,
				TopP = m_Settings.TopP,
				Messages = m_Messages
			};
		}

		public async Task<string?> GetResponseAsync()
		{
			var request = CreateRequest();

			var response = await m_Endpoint.CreateChatCompletionAsync(request);

			if (response.Choices.Count == 0)
			{
				return null;
			}

			var responseMessage = response.Choices[0].Message;

			AppendMessage(responseMessage);

			return responseMessage.Content;
		}

		private void AppendMessage(ChatMessage message)
		{
			if (m_Messages.Count >= m_Settings.WindowSize + 1)
			{
				// Index 0 is used for the prompt
				m_Messages.RemoveAt(1);
			}

			m_Messages.Append(message);
		}
	}
}
