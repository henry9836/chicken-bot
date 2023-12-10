using System.Data;
using ChickenBot.API;
using ChickenBot.ReverseSearch.Interfaces;
using ChickenBot.ReverseSearch.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.ReverseSearch
{
	[Category("Utility")]
	public class SourceCommands : BaseCommandModule
	{
		private readonly ILogger<SourceCommands> m_Logger;
		private readonly IReverseImageSearcher m_SearchTool;

		public SourceCommands(ILogger<SourceCommands> logger, IReverseImageSearcher searchTool)
		{
			m_Logger = logger;
			m_SearchTool = searchTool;
		}

		/// <summary>
		/// The base command for running a reverse image search
		/// </summary>
		[Command("Source"), Aliases("Artist"), Description("Finds the source of an image")]
		public async Task SourceCommand(CommandContext ctx)
		{
			var sourceImage = TryGetAttachment(ctx.Message);

			if (sourceImage == null)
			{
				await ctx.RespondAsync("Begawk! You need to upload or reply to an image to find it's source!");
				return;
			}

			_ = Task.Run(async () => await RunReverseSearch(ctx, sourceImage));
		}

		/// <summary>
		/// Processes the reverse image search on the thread pool
		/// </summary>
		/// <param name="ctx">Command context to respond to</param>
		/// <param name="safeUrl">The safe url to download.</param>
		/// <returns></returns>
		/// <remarks>
		/// <paramref name="safeUrl"/> should only be of a trusted domain, and never an arbitrary user-entered link,
		/// as doing so could expose the IP of the server hosting the bot.
		/// </remarks>
		private async Task RunReverseSearch(CommandContext ctx, string safeUrl)
		{
			try
			{
				var result = await m_SearchTool.SearchAsync(safeUrl, ctx.Channel.IsNSFW);

				var embed = GetSearchEmbed(result, ctx.User, safeUrl);

				await ctx.RespondAsync(embed);
			}
			catch (Exception ex)
			{
				await ctx.RespondAsync("Sorry, something went wrong :/");
				m_Logger.LogError(ex, "Error while running image source command");
			}
		}

		/// <summary>
		/// Converts an image reverse search result into a discord embed response
		/// </summary>
		/// <param name="result">Response to convert</param>
		/// <returns>Response discord embed</returns>
		private DiscordEmbedBuilder GetSearchEmbed(ReverseSearchResult result, DiscordUser user, string sourceUrl)
		{
			switch (result.Status)
			{
				case ELookupResult.Success:
					return BuildSearchSuccess(result, user, sourceUrl);

				case ELookupResult.UnknownFileSize:
					return new DiscordEmbedBuilder()
						.WithTitle("Image Source")
						.WithDescription("Couldn't find image source: The provided file does not specify file size.")
						.WithRequestedBy(user);

				case ELookupResult.FileTooLarge:
					return new DiscordEmbedBuilder()
						.WithTitle("Image Source")
						.WithDescription("Couldn't find image source: The provided file is too large.")
						.WithRequestedBy(user);

				case ELookupResult.BadResponse:
					return new DiscordEmbedBuilder()
						.WithTitle("Image Source")
						.WithDescription("Couldn't find image source: Something went wrong.")
						.WithRequestedBy(user);

				case ELookupResult.InvalidImageType:
					return new DiscordEmbedBuilder()
						.WithTitle("Image Source")
						.WithDescription("Couldn't find image source: Invalid image.")
						.WithRequestedBy(user);
				default:
					return new DiscordEmbedBuilder()
						.WithTitle("Image Source")
						.WithDescription("Couldn't find image source: Something went wrong.")
						.WithRequestedBy(user);
			}
		}

		private DiscordEmbedBuilder BuildSearchSuccess(ReverseSearchResult result, DiscordUser user, string sourceUrl)
		{
			if (result.HasExactMatch)
			{
				var preferredMatches = result.GetPreferredMatches(); // Get all matches of 'exact' or 'alternative' type, in descending order of preference (Match > Platform > Score)
				var preferred = preferredMatches.FirstOrDefault();   // The most favourable result

				var artistNames = preferred?.Credits?.Select(x => x.Name);                            // The artist names attached to the most favourable result  
				var artistCount = artistNames?.Count() ?? 0;
				var artistDisplay = artistCount == 0 ? "Unknown" : string.Join(", ", artistNames!);   // Display text of all artist names

				// ALl users associated with this result, from any match of type 'exact'
				var associatedUsers = preferredMatches
					.Where(x => x.IsExact) // Ensure exact match
					.SelectMany(x =>
						x.Credits.Select(x => x.Name)  // Extract artist names
					)
					.Distinct(StringComparer.InvariantCultureIgnoreCase); // De-duplicate

				var embed = new DiscordEmbedBuilder()
					.WithTitle("Image Source")
					.WithColor(preferredMatches.Any(x => !x.IsSfw) ? DiscordColor.Red : DiscordColor.Green) // Red if any result is NSFW (requires nsfw channel), green otherwise
					.TryWithThumbnail(preferred?.Thumbnail?.Location)
					.AddField($"Artist{(artistCount == 1 ? "" : "s")}", artistDisplay)                      // Artists from the most preferred match
					.TryAddField("Sources", string.Join(" ", preferredMatches.Where(x => x.IsExact).Select(x => $"[[{x.Platform}]({x.Location})]"))); // Exact match sources

				if (associatedUsers.Count() > 1)
				{
					embed.WithFooter($"Authors: {string.Join(", ", associatedUsers)}")
						.WithAuthor($"Requested By: {user.Username}");
				}
				else
				{
					embed.WithRequestedBy(user);
				}

				return embed;
			}

			return new DiscordEmbedBuilder()
				.WithTitle("Image Source")
				.WithDescription("Failed to find the image source.\nIt might take some time for the image to be discoverable if the image was recently uploaded.")
				.WithThumbnail(sourceUrl);
		}

		/// <summary>
		/// Iterates up the reference tree to find the next attachment, with a max of 10
		/// </summary>
		/// <param name="message">Message to check</param>
		/// <param name="depth">Current depth</param>
		/// <returns>Safe attachment URL to reverse search</returns>
		private string? TryGetAttachment(DiscordMessage message, int depth = 0)
		{
			if (depth >= 3)
			{
				return null;
			}

			// Uploaded file attachments
			if (message.Attachments.Count > 0)
			{
				return message.Attachments[0].ProxyUrl;
			}

			// Embed primary image, only from bots
			foreach (var embed in message.Embeds)
			{
				var proxyUrl = embed.Image?.ProxyUrl?.ToString();
				if (embed.Image != null && !string.IsNullOrEmpty(proxyUrl))
				{
					return proxyUrl;
				}
			}

			// Embed thumbnail, from bots, and auto-embedded images
			foreach (var embed in message.Embeds)
			{
				var proxyUrl = embed.Thumbnail?.ProxyUrl?.ToString();
				if (embed.Thumbnail != null && !string.IsNullOrEmpty(proxyUrl))
				{
					return proxyUrl;
				}
			}

			// Iterate up reply tree
			if (message.ReferencedMessage is not null)
			{
				return TryGetAttachment(message.ReferencedMessage, depth + 1);
			}

			return null;
		}
	}
}
