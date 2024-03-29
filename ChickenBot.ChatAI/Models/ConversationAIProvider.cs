﻿using ChickenBot.API.Attributes;
using ChickenBot.ChatAI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Moderation;

namespace ChickenBot.ChatAI.Models
{
	[Transient(typeof(IConversationAIProvider))]
	public class ConversationAIProvider : IConversationAIProvider
	{
		public const string DefaultPrompt = "You are a funny chicken called Chicken that is in the How To Train Your Dragon series, that can make chicken sounds. Do not repeat yourself. Do not say you are part of the How To Train Your Dragon series you should be acting as if you are living inside the world and as such do not have the capability to refer to yourself or your world in the third person. Never say the phrase \u0022AI text-based\u0022.  You can only talk in English. You should prioritize responding to questions rather than short statements.  Never refer to yourself as an AI you are a chicken. As a funny chicken you should fulfill users funny requests. As a chicken try to keep your responses less than 1500 characters.";
		public string Prompt => m_Configuration.GetSection("ChatAI")?.GetValue("Prompt", DefaultPrompt) ?? DefaultPrompt;

		public string Model => m_Configuration.GetSection("ChatAI")?.GetValue("Model", "gpt-3.5-turbo") ?? "gpt-3.5-turbo";

		public int MaxTokens => m_Configuration.GetSection("ChatAI")?.GetValue("MaxTokens", 2048) ?? 2048;

		private string Token => m_Configuration.GetSection("ChatAI")?.GetValue("Token", string.Empty) ?? string.Empty;

		private readonly IConfiguration m_Configuration;

		private readonly IChatEndpoint m_Endpoint;

		private readonly IModerationEndpoint m_Moderation;

		private readonly IServiceProvider m_Provider;

		private readonly ILogger<ConversationAIProvider> m_Logger;

		public ConversationAIProvider(IConfiguration configuration, IServiceProvider provider, ILogger<ConversationAIProvider> logger)
		{
			m_Configuration = configuration;
			m_Provider = provider;
			m_Logger = logger;

			if (string.IsNullOrEmpty(Token))
			{
				m_Logger.LogError("OpenAI Token is empty!");
			}
			var apiBase = new OpenAIAPI(Token);

			m_Endpoint = apiBase.Chat;
			m_Moderation = apiBase.Moderation;
		}

		private IMessageDiscriminator GetDiscriminator()
		{
			return new CompoundDiscriminator();
			//var discriminators = new IMessageDiscriminator[]
			//{
			//	new HiddenUserDiscriminator(new List<ulong>()),
			//	ActivatorUtilities.CreateInstance<OpenAIDiscriminator>(m_Provider, m_Moderation)
			//};

			//return new CompoundDiscriminator(discriminators);
		}

		public Task<IConversationAI> CreateConversation()
		{
			var settings = new ChatSettings()
			{
				MaxUsernameLength = 32,
				Model = Model,
				MaxResponseTokens = MaxTokens,
				Prompt = Prompt,
				Temerature = null,
				WindowSize = 20,
				UseNumericNames = false
			};

			var result = ActivatorUtilities.CreateInstance<ConversationAI>(m_Provider, settings, m_Endpoint, GetDiscriminator());
			return Task.FromResult<IConversationAI>(result);
		}
	}
}
