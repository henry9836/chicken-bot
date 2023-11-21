﻿using System.Text;
using ChickenBot.API;
using ChickenBot.API.Atrributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Petitions
{
	public class PetitionCommand : BaseCommandModule
	{
		public ulong PetitionsChannelID => m_Configuration.GetSection("Channels").GetValue("Petitions", 0ul);

		private readonly IConfiguration m_Configuration;
		private readonly ILogger<PetitionCommand> m_Logger;

		private readonly string[] m_ImageExtensions = new[] { "png", "jpg", "jpeg", "gif", "tiff", "webp" };

		public PetitionCommand(IConfiguration configuration, ILogger<PetitionCommand> logger)
		{
			m_Configuration = configuration;
			m_Logger = logger;
		}

		[Command("Petition"), RequireVerified]
		public async Task PetitionHelpCommand(CommandContext ctx)
		{
			var embed = new DiscordEmbedBuilder()
				.WithTitle("Petition Command")
				.WithDescription("Usage: `petition [petition text]\nYou can also attach files and links")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("Petition"), Description("Starts a new petition"), RequireVerified]
		public async Task CreatePetitionCommand(CommandContext ctx, [RemainingText] string petitionText)
		{
			// Get petitions channel
			var channel = ctx.Guild.GetChannel(PetitionsChannelID);

			if (channel == null)
			{
				await ctx.RespondAsync("Begawk! I can't seem to find the petitions channel...");
				m_Logger.LogWarning("Cannot get petitions channel");
				return;
			}

			// Find all attachments provided in the message

			var attachments = new List<string>();

			foreach (var file in ctx.Message.Attachments)
			{
				attachments.Add(file.Url);
			}

			var sb = new StringBuilder();

			foreach (var word in petitionText.Split(' '))
			{
				if (Uri.IsWellFormedUriString(word, UriKind.Absolute))
				{
					// Remove urls from the petition text, to add to attachments list
					attachments.Add(word);
					continue;
				}

				sb.Append(word);
				sb.Append(' ');
			}

			petitionText = sb.ToString().Trim();

			// Determine if the petition only has a single image attached, to embed inside the petition embed
			string? onlyImageUrl = null;

			if (attachments.Count > 0)
			{
				var images = attachments.Where(IsImage).ToArray();
				if (images.Length == 1)
				{
					onlyImageUrl = images[0];
					attachments.Remove(onlyImageUrl);
				}
			}

			// Create petition embed
			var embed = new DiscordEmbedBuilder()
				.WithTitle($"Petition by {ctx.Message.Author.Username}")
				.WithDescription(petitionText)
				.WithImageUrl(onlyImageUrl)
				.Build();

			// Create parent message to also include additional attachments
			var message = new DiscordMessageBuilder()
				.WithEmbed(embed)
				.WithContent(string.Join('\n', attachments));

			var petition = await channel.SendMessageAsync(message);

			var thumbsUp = DiscordEmoji.FromName(ctx.Client, "thumbsup", includeGuilds: false);
			var thumbsDown = DiscordEmoji.FromName(ctx.Client, "thumbsdown", includeGuilds: false);

			if (thumbsUp != null)
			{
				await petition.CreateReactionAsync(thumbsUp);
			}

			if (thumbsDown != null)
			{
				await petition.CreateReactionAsync(thumbsDown);
			}

			m_Logger.LogInformation("Created petition, as requested by {user}", ctx.Message.Author.Username);
		}

		private bool IsImage(string url)
		{
			var ext = Path.GetExtension(url).Trim('.').ToLowerInvariant();
			return m_ImageExtensions.Contains(ext);
		}
	}
}
