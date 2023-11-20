﻿using ChickenBot.API.Atrributes;
using ChickenBot.ChatAI.Interfaces;
using ChickenBot.ChatAI.Models.Discriminators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Moderation;

namespace ChickenBot.ChatAI.Models
{
	[Transient(typeof(IConversationAIProvider))]
	public class ConversationAIProvider : IConversationAIProvider
	{
		public const string DefaultPrompt = "This is a conversion between users, you are the bot (B:), the bot is a funny chicken that can make chicken sounds. Do not repeat yourself. You can only talk in English. You should prioritize responding to questions rather than short statements.";

		public string Prompt => m_Configuration.GetSection("ChatAI")?.GetValue("Prompt", DefaultPrompt) ?? DefaultPrompt;

		public string Model => m_Configuration.GetSection("ChatAI")?.GetValue("Model", "text-davinci-003") ?? "text-davinci-003";

		public int MaxTokens => m_Configuration.GetSection("ChatAI")?.GetValue("MaxTokens", 2048) ?? 2048;

		private string Token => m_Configuration.GetSection("ChatAI")?.GetValue("Token", string.Empty) ?? string.Empty;


		private readonly IConfiguration m_Configuration;

		private readonly IChatEndpoint m_Endpoint;

		private readonly IModerationEndpoint m_Moderation;

		private readonly IServiceProvider m_Provider;

		public ConversationAIProvider(IConfiguration configuration, IServiceProvider provider)
		{
			m_Configuration = configuration;
			m_Provider = provider;

			var apiBase = new OpenAIAPI(Token);
			m_Endpoint = apiBase.Chat;
			m_Moderation = apiBase.Moderation;
		}

		private IMessageDiscriminator GetDiscriminator()
		{
			var discriminators = new IMessageDiscriminator[]
			{
				new HiddenUserDiscriminator(new List<ulong>()),
				ActivatorUtilities.CreateInstance<OpenAIDiscriminator>(m_Provider, m_Moderation)
			};

			return new CompoundDiscriminator(discriminators);
		}

		public Task<IConversationAI> CreateConversation()
		{
			var settings = new ChatSettings()
			{
				MaxUsernameLength = 16,
				Model = Model,
				MaxResponseTokens = MaxTokens,
				Prompt = Prompt,
				Temerature = null,
				WindowSize = 20,
				UseNumericNames = false
			};

			return new ConversationAI(settings, m_Endpoint, GetDiscriminator());





		}
	}
}
