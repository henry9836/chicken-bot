using System.Collections.Concurrent;
using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;
using OpenAI_API.Chat;

namespace ChickenBot.ChatAI.Models
{
	public class ConversationAI : IConversationAI
	{
		private readonly List<ChatMessage> m_Messages = new List<ChatMessage>();

		private readonly ChatSettings m_Settings;

		private readonly IChatEndpoint m_Endpoint;

		private readonly IMessageDiscriminator m_Discriminator;

		private readonly Dictionary<ulong, int> m_UserChatIDs = new Dictionary<ulong, int>();

		private readonly Random m_Random = new Random();

		private int m_ChatIDIndex = 0;

		private readonly ConcurrentQueue<(DiscordUser user, string message)> m_PushQueue = new();

		private bool m_WorkerActive = false;

		public ConversationAI(ChatSettings settings, IChatEndpoint endpoint, IMessageDiscriminator discriminator)
		{
			m_Settings = settings;
			m_Endpoint = endpoint;
			m_Discriminator = discriminator;

			SetPrompt(settings.Prompt);
		}

		/// <summary>
		/// Lazy method to push a chat message to the conversaiton
		/// </summary>
		/// <remarks>
		/// Chat messages pushed here are subject to message discriminators, and might be rejected from the chat context
		/// </remarks>
		public void PushChatMessage(DiscordUser user, string message)
		{
			if (user.IsBot)
			{
				return;
			}

			m_PushQueue.Enqueue((user, message));

			if (!m_WorkerActive)
			{
				m_WorkerActive = true;
				Task.Run(ProcessMessagePush);
			}
		}

		/// <summary>
		/// Processes and discriminates user messages in strict order, to push to the sliding window
		/// </summary>
		private async Task ProcessMessagePush()
		{
			try
			{
				while (m_PushQueue.TryDequeue(out var message))
				{
					if (!await m_Discriminator.Discriminate(message.user, message.message))
					{
						// Message was flagged, discard
						continue;
					}

					PushChatMessageInternal(message.user, message.message);
				}
			}
			finally
			{
				m_WorkerActive = false;
			}
		}

		/// <summary>
		/// Pushes a confirmed message onto the sliding window
		/// </summary>
		/// <param name="user"></param>
		/// <param name="message"></param>
		private void PushChatMessageInternal(DiscordUser user, string message)
		{
			string userChatID;

			if (m_Settings.UseNumericNames)
			{
				if (!m_UserChatIDs.TryGetValue(user.Id, out var chatID))
				{
					chatID = m_ChatIDIndex++;
					m_UserChatIDs[user.Id] = chatID;
				}
				userChatID = chatID.ToString();
			}
			else
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

		/// <summary>
		/// Creates a request for the current conversation state
		/// </summary>
		/// <returns></returns>
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
				Temperature = m_Settings.Temerature ?? DetermineTemperature(),
				TopP = m_Settings.TopP,
				Messages = m_Messages
			};
		}

		/// <summary>
		/// Determines the next message's temperature
		/// </summary>
		/// <remarks>
		/// Generates a number between 0.7 - 1.35
		/// </remarks>
		private double DetermineTemperature()
		{
			return 0.7 + (m_Random.NextDouble() * 0.65);
		}

		/// <summary>
		/// Gets a response as the chicken to the current chat feed
		/// </summary>
		/// <returns>Message, or null if the AI decided to stay silent</returns>
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

		/// <summary>
		/// Appends a message to the end of the sliding window
		/// </summary>
		/// <param name="message">Message to append</param>
		private void AppendMessage(ChatMessage message)
		{
			if (m_Messages.Count >= m_Settings.WindowSize + 1)
			{
				// Index 0 is used for the prompt
				m_Messages.RemoveAt(1);
			}

			m_Messages.Append(message);
		}

		/// <summary>
		/// Sets or replaces the prompt for this conversation
		/// </summary>
		public void SetPrompt(string prompt)
		{
			var message = new ChatMessage(ChatMessageRole.System, prompt);

			if (m_Messages.Count == 0)
			{
				m_Messages.Add(message);
			} else
			{
				m_Messages[0] = message;
			}
		}
	}
}
