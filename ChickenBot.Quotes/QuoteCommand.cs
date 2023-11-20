using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Quotes
{
	public class QuoteCommand
	{
		public ulong QuotesChannelID => m_Configuration.GetSection("Channels").GetValue("Quotes", 0ul);

		private readonly ILogger<QuoteCommand> m_Logger;
		private readonly IConfiguration m_Configuration;
		private readonly string[] m_AllowedTypes = new[] { "png", "jpg", "jpeg" };

		public QuoteCommand(ILogger<QuoteCommand> logger, IConfiguration configuration)
		{
			m_Logger = logger;
			m_Configuration = configuration;
		}

		public async Task AddQuoteCommand(CommandContext ctx, [RemainingText] string? text)
		{
			string? attachmentUrl = null;

			if (ctx.Message.Attachments.Count > 0)
			{
				// Quote from uploaded attachment
				attachmentUrl = ctx.Message.Attachments[0].Url;
			}
			else if (text != null)
			{
				// Quote from provided image URL
				var providedUrl = text.Split(' ').First();

				// Ensure it is a http or https URL
				if (Uri.TryCreate(providedUrl, UriKind.Absolute, out var parsedUri) &&
					(parsedUri.Scheme == "https" || parsedUri.Scheme == "http"))
				{
					attachmentUrl = parsedUri.AbsoluteUri;
				}

				// Remove link from description
				text = text.Substring(providedUrl.Length).Trim();
			}

			if (attachmentUrl == null)
			{
				await ctx.RespondAsync("Begawk! You need to attach an image to quote!");
				return;
			}

			var extension = Path.GetExtension(attachmentUrl);

			var isAllowed = m_AllowedTypes.Any(x => extension.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));

			if (!isAllowed)
			{
				await ctx.RespondAsync("Begawk! You have to attach an image!");
				return;
			}

			var quotesChannel = ctx.Guild.GetChannel(QuotesChannelID);

			if (quotesChannel == null)
			{
				await ctx.RespondAsync("Begawk! I can't seem to find the quotes channel");
				return;
			}

			var embed = new DiscordEmbedBuilder()
				.WithImageUrl(attachmentUrl)
				.WithTitle($"Quote by {ctx.Message.Author.Username}")
				.WithFooter($"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>")
				.WithDescription(text);

			m_Logger.LogInformation("Posted quote from user {user}: {url}", ctx.Message.Author.Username, attachmentUrl);
		}
	}
}
