using ChickenBot.API.Attributes;
using ChickenBot.API.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Quotes
{
	public class NsfwQuoteCommand : BaseCommandModule
	{
		private ulong NsfwQuotesChannelId => m_Configuration.GetSection("Channels").GetValue("NsfwQuotes", 0ul);
		private DiscordChannel? m_NsfwChannel;

		private readonly ILogger<NsfwQuoteCommand> m_Logger;
		private readonly IConfiguration m_Configuration;
		private readonly string[] m_AllowedTypes = new[] { "png", "jpg", "jpeg", "webp" };

		public NsfwQuoteCommand(ILogger<NsfwQuoteCommand> logger, IConfiguration configuration)
		{
			m_Logger = logger;
			m_Configuration = configuration;
		}

		[Command("nsfw-quote"), Aliases("quote-nsfw"), Description("Quote a funny screenshot from an nsfw channel"), RequireVerified]
		public async Task AddQuoteCommand(CommandContext ctx, [RemainingText] string? text)
		{
			// If we are in a sfw channel then delete the message
			if (!ctx.Channel.IsNSFW)
			{
				await ctx.Channel.SendMessageAsync("Bonk!");
				await ctx.Message.DeleteAsync();
				return;
			}

			string? attachmentUrl = null;

			if (ctx.Message.Attachments.Count > 0)
			{
				// Quote from uploaded attachment
				attachmentUrl = ctx.Message.Attachments[0].Url;
			}
			else if (text != null)
			{
				attachmentUrl = AttachmentUtils.ExtractAttachment(ref text);
			}

			if (attachmentUrl == null)
			{
				await ctx.RespondAsync("Begawk! You need to attach an image to quote!");
				return;
			}

			var isAllowed = AttachmentUtils.IsImage(attachmentUrl);

			if (!isAllowed)
			{
				await ctx.RespondAsync("Begawk! You have to attach an image!");
				return;
			}

			if (m_NsfwChannel is null)
			{
				m_NsfwChannel = ctx.Guild.GetChannel(NsfwQuotesChannelId);
				if (m_NsfwChannel is null)
				{
					await ctx.RespondAsync("Begawk! I can't seem to find the nsfw-quotes channel");
					return;
				}
			}

			var embed = new DiscordEmbedBuilder()
				.WithImageUrl(attachmentUrl)
				.WithTitle($"Quote by {ctx.Message.Author.Username}")
				.WithTimestamp(DateTime.Now)
				.WithDescription($"{text}\n<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>".Trim())
				.WithColor(new DiscordColor("#c4160a"));

			await m_NsfwChannel.SendMessageAsync(embed);

			m_Logger.LogInformation("Posted nsfw quote from user {user}: {url} '{message}'", ctx.Message.Author.Username, attachmentUrl, text);

			var emoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:", false);

			if (emoji is not null)
			{
				await ctx.Message.CreateReactionAsync(emoji);
			}
		}
	}
}
