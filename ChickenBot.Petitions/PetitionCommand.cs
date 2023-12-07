using System.Text;
using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.API.Models;
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

			if (channel is null)
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

			foreach(var file in AttachmentUtils.ExtractAttachments(ref petitionText))
			{
				attachments.Add(file);
			}

			// Determine if the petition only has a single image attached, to embed inside the petition embed
			string? onlyImageUrl = null;

			if (attachments.Count > 0)
			{
				var images = attachments.Where(AttachmentUtils.IsImage).ToArray();
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
				.WithImageUrl(onlyImageUrl!)
				.Build();

			// Create parent message to also include additional attachments
			var message = new DiscordMessageBuilder()
				.WithEmbed(embed)
				.WithContent(string.Join('\n', attachments));

			var petition = await channel.SendMessageAsync(message);
			if (petition is null)
			{
				m_Logger.LogWarning("Could not post to petitions channel");
				return;
			}

			var thumbsUp = DiscordEmoji.FromName(ctx.Client, ":thumbsup:", includeGuilds: false);
			var thumbsDown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:", includeGuilds: false);

			if (thumbsUp is not null)
			{
				await petition.CreateReactionAsync(thumbsUp);
			}

			if (thumbsDown is not null)
			{
				await petition.CreateReactionAsync(thumbsDown);
			}

			m_Logger.LogInformation("Created petition, as requested by {user}", ctx.Message.Author.Username);
		}
	}
}
