using System.Collections.Concurrent;
using System.Text;
using ChickenBot.API.Attributes;
using ChickenBot.TicketingSystem.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.TicketingSystem.Services
{
    [Singleton]
    public class TicketManager : IEventHandler<MessageCreatedEventArgs>
    {
        public ulong TicketsChannelID => m_Configuration.GetSection("Channels:tickets").Get<ulong>();

        public string? WebhookURL => m_Configuration["Tickets:WebhookUrl"];

        private readonly IConfiguration m_Configuration;
        private readonly ILogger<TicketManager> m_Logger;
        private readonly TicketDatabase m_Database;
        private readonly DiscordClient m_Discord;
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly DiscordWebhookClient m_WebhooksClient;

        private ulong m_TicketChannel = 0;

        private DiscordGuild? m_HomeGuild = null;

        private DiscordWebhook? m_Webhook = null;

        private readonly ConcurrentDictionary<ulong, DateTime> m_ClosedWarningsCooldown = new();

        public TicketManager(IConfiguration configuration, TicketDatabase database, DiscordClient client, ILogger<TicketManager> logger, ILoggerFactory loggerFactory)
        {
            m_Configuration = configuration;
            m_Database = database;
            m_Discord = client;
            m_Logger = logger;
            m_LoggerFactory = loggerFactory;

            m_WebhooksClient = new DiscordWebhookClient(loggerFactory: loggerFactory);
        }

        public async Task DoTicketClosure(Ticket ticket, bool automatic)
        {
            try
            {
                var user = await m_Discord.GetUserAsync(ticket.UserID);
                var userMessage = new DiscordEmbedBuilder()
                    .WithTitle("Ticket Closed")
                    .WithDescription(automatic ? "Your ticket was automatically closed, because it was inactive for 3 days" : "A moderator has marked your ticket as closed")
                    .WithColor(DiscordColor.Red);

                var dmChannel = await user.CreateDmChannelAsync();

                await dmChannel.SendMessageAsync(userMessage);
            }
            catch (Exception)
            {
                // Suppress errors due to user not being found, or them blocking the bot
            }

            try
            {
                if (m_HomeGuild is null)
                {
                    m_HomeGuild = await m_Discord.GetGuildAsync(m_Configuration.GetSection("GuildID").Get<ulong>());
                }

                var channel = await m_HomeGuild.GetChannelAsync(TicketsChannelID);

                if (channel is not DiscordForumChannel ticketsChannel)
                {
                    m_Logger.LogError("Configured channel '{channel}' is not a forums channel!", channel.Name);
                    throw new Exception();
                }

                var thread = ticketsChannel.Threads.FirstOrDefault(x => x.Id == ticket.ThreadID);

                if (thread is not null)
                {
                    var closureMessage = new DiscordEmbedBuilder()
                        .WithTitle("Ticket Closed")
                        .WithDescription(automatic ? "The ticket was automatically closed, because it was inactive for 3 days" : "Ticket closed")
                        .WithFooter("Messages sent in this thread will no longer be sent to the user")
                        .WithColor(DiscordColor.Red);

                    await thread.SendMessageAsync(closureMessage);

                    var closedTags = ticketsChannel.AvailableTags.Where(x => x.Name == "Closed").Select(x => x.Id);
                    await thread.ModifyAsync(x =>
                    {
                        x.AppliedTags = closedTags;
                    });
                }
            }
            catch (Exception)
            {
                // Suppress errors due to the thread being archived, or deleted
            }
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
        {
            if (args.Author.IsBot)
            {
                return;
            }

            if (args.Channel.ParentId == TicketsChannelID)
            {
                if (args.Message.Content.StartsWith('!'))
                {
                    return;
                }

                await HandleTicketsMessage(args);
                return;
            }

            if (args.Channel.IsPrivate)
            {
                await HandleDMMessage(args);
            }
        }

        private async Task HandleTicketsMessage(MessageCreatedEventArgs args)
        {
            var ticket = await m_Database.GetTicketByThread(args.Channel.Id);

            if (ticket == null)
            {
                return;
            }

            if (ticket.Closed is not null)
            {
                if (m_ClosedWarningsCooldown.TryGetValue(args.Channel.Id, out var expires))
                {
                    if (expires < DateTime.UtcNow)
                    {
                        return;
                    }
                    m_ClosedWarningsCooldown[args.Channel.Id] = DateTime.UtcNow.AddHours(2);
                }

                await args.Message.RespondAsync("Your message was undelivered because this ticket has already been closed.\nYou can reopen the ticket with `!ticket reopen`");
                return;
            }

            var targetUser = await m_Discord.GetUserAsync(ticket.UserID);

            var targetChannel = await targetUser.CreateDmChannelAsync();

            var sb = new StringBuilder();

            sb.Append(args.Message.Content);

            if (args.Message.Attachments.Any())
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine();

                foreach (var attachment in args.Message.Attachments)
                {
                    sb.AppendLine(attachment.Url);
                }
            }

            var message = new DiscordMessageBuilder()
                .WithContent(sb.ToString());

            await targetChannel.SendMessageAsync(message);

            await m_Database.MarkTicketActive(ticket.ID);
        }

        private async Task HandleDMMessage(MessageCreatedEventArgs args)
        {
            var ticket = await m_Database.GetActiveTicketByUser(args.Author.Id);

            var newTicket = false;

            if (ticket == null)
            {
                ticket = await CreateTicket(args);
                newTicket = true;
                if (ticket == null)
                {
                    return;
                }
            }

            await ForwardDMMessage(args, ticket);

            if (!newTicket)
            {
                await m_Database.MarkTicketActive(ticket.ID);
            }
        }

        private async Task<Ticket?> CreateTicket(MessageCreatedEventArgs args)
        {
            if (m_HomeGuild is null)
            {
                m_HomeGuild = await m_Discord.GetGuildAsync(m_Configuration.GetSection("GuildID").Get<ulong>());
            }

            var channel = await m_HomeGuild.GetChannelAsync(TicketsChannelID);

            if (channel is not DiscordForumChannel ticketsChannel)
            {
                m_Logger.LogError("Configured channel '{channel}' is not a forums channel!", channel.Name);
                return null;
            }

            var ticket = new Ticket()
            {
                UserID = args.Author.Id,
                Created = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                ThreadID = 0
            };

            await m_Database.CreateTicket(ticket);

            var forumMessage = new ForumPostBuilder()
                .WithName($"Ticket #{ticket.ID} ({args.Author.GlobalName})")
                .WithAutoArchiveDuration(DiscordAutoArchiveDuration.ThreeDays)
                .WithMessage(new DiscordMessageBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"Ticket #{ticket.ID}")
                        .AddField("User", args.Author.Mention, true)
                        .AddField("Created", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>", true)
                        .AddField("Username", args.Author.Username, true)
                        .AddField("Options", $"[`[DM User]`](<https://discordapp.com/channels/@me/{args.Message.Channel!.Id}>)")
                        .WithThumbnail(args.Author.AvatarUrl)
                 ));

            var openTag = ticketsChannel.AvailableTags.FirstOrDefault(x => x.Name == "Open");

            if (openTag is not null)
            {
                forumMessage.AddTag(openTag);
            }

            var ticketPost = await ticketsChannel.CreateForumPostAsync(forumMessage);

            ticket.ThreadID = ticketPost.Channel.Id;
            await m_Database.SetTicketThread(ticket.ID, ticketPost.Channel.Id);

            var userMessage = new DiscordEmbedBuilder()
                .WithTitle($"Ticket Created: #{ticket.ID}")
                .WithDescription("A Moderator will respond to your ticket.\nYou can post further messages and attachments here to add to your ticket.")
                .WithColor(DiscordColor.Green);

            await args.Message.RespondAsync(userMessage);
            return ticket;
        }

        private async Task ForwardDMMessage(MessageCreatedEventArgs args, Ticket ticket)
        {
            try
            {
                if (m_Webhook is null)
                {
                    if (!Uri.TryCreate(WebhookURL, UriKind.Absolute, out var webhookUri))
                    {
                        throw new InvalidOperationException("Invalid webhook URL configured in config");
                    }

                    m_Webhook = await m_WebhooksClient.AddWebhookAsync(webhookUri);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error initializing webhook");
                throw;
            }

            var userMessage = new DiscordWebhookBuilder()
                .WithAvatarUrl(args.Author.AvatarUrl)
                .WithUsername(args.Author.GlobalName)
                .WithThreadId(ticket.ThreadID);

            var sb = new StringBuilder();
            sb.AppendLine(args.Message.Content);

            if (args.Message.Attachments.Any())
            {

                foreach (var attachment in args.Message.Attachments)
                {
                    sb.AppendLine(attachment.Url);
                }
            }

            userMessage.WithContent(sb.ToString());

            await m_Webhook.ExecuteAsync(userMessage);
        }
    }
}
