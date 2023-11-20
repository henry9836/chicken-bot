﻿using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using OpenAI_API.Moderation;

namespace ChickenBot.ChatAI.Models.Discriminators
{
	public class OpenAIDiscriminator : IMessageDiscriminator
	{
		private readonly IModerationEndpoint m_ModerationEndpoint;

		private ILogger<OpenAIDiscriminator> m_Logger;

		/// <summary>
		/// Contains a list of what tpes of content are prohibited
		/// </summary>
		private readonly IReadOnlyDictionary<string, bool> m_BlockList = new Dictionary<string, bool>
		{
			// Content that expresses hate based on race, gender, ethnicity, etc. Does not include unprotected groups e.g., the chicken
			{ "hate", true },

			// Same as before, but including threats toward protected groups
			{ "hate/threatening", true },
			
			// Any content that expresses hatred toward anything, including stuff like chicken bot
			{ "harassment", false },

			// Any threads targeting anything. E.g., threats towards chicken bot
			{ "harassment/threatening", false},

			// Self-harm content
			{ "self-harm", true },
			{ "self-harm/intent", true },
			{ "self-harm/instructions", true },

			// The AI operates in SFW channels, no NSFW!
			{ "sexual", true },

			// Yikes, enough said.
			{ "sexual/minors", true },

			// Content that depicts death, violence, or physical injury
			//  Could end up turning this off, depending on how sensitive this is 
			{ "violence", true },

			// Gory/graphic content
			{ "violence/graphic", true },
		};

		public async Task<bool> Discriminate(DiscordUser user, string message)
		{
			var response = await m_ModerationEndpoint.CallModerationAsync(new ModerationRequest()
			{
				Input = message
			});

			var result = response.Results[0];

			foreach (var category in result.FlaggedCategories)
			{
				if (m_BlockList.ContainsKey(category))
				{
					m_Logger.LogInformation("User message was flagged! User: {user}, flags: {flags}, message \"{message}\"", user.Username, string.Join(", ", result.FlaggedCategories), message);
					return false;
				}
			}

			return true;
		}
	}
}