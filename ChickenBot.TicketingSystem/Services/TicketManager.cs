using System.Collections.Concurrent;
using System.Text;
using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using ChickenBot.TicketingSystem.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.TicketingSystem.Services
{
    [Singleton]
    public class TicketManager : IEventHandler<MessageCreatedEventArgs>, IEventHandler<ThreadDeletedEventArgs>
    {
        public ulong TicketsChannelID => m_Configuration.GetSection("Channels:tickets").Get<ulong>();

        public string? WebhookURL => m_Configuration["Tickets:WebhookUrl"];

        private readonly IConfiguration m_Configuration;
        private readonly ILogger<TicketManager> m_Logger;
        private readonly TicketDatabase m_Database;
        private readonly DiscordClient m_Discord;
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly DiscordWebhookClient m_WebhooksClient;
        private readonly IConfigEditor m_ConfigEditor;

        private ulong m_TicketChannel = 0;

        private DiscordGuild? m_HomeGuild = null;

        private DiscordWebhook? m_Webhook = null;

        private readonly ConcurrentDictionary<ulong, DateTime> m_ClosedWarningsCooldown = new();

        public TicketManager(IConfiguration configuration, TicketDatabase database, DiscordClient client, ILogger<TicketManager> logger, ILoggerFactory loggerFactory, IConfigEditor configEditor)
        {
            m_Configuration = configuration;
            m_Database = database;
            m_Discord = client;
            m_Logger = logger;
            m_LoggerFactory = loggerFactory;
            m_ConfigEditor = configEditor;

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
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to send ticket closed message to user {UserID}. Did they leave or block the bot?", ticket);
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
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to update ticket thread on closure: ");
            }
        }

        public async Task DoTicketReopen(Ticket ticket)
        {
            try
            {
                var user = await m_Discord.GetUserAsync(ticket.UserID);
                var userMessage = new DiscordEmbedBuilder()
                    .WithTitle($"Ticket #{ticket.ID} Reopened")
                    .WithDescription("A moderator reopened your ticket")
                    .WithColor(DiscordColor.Green);

                var dmChannel = await user.CreateDmChannelAsync();

                await dmChannel.SendMessageAsync(userMessage);
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to send ticket reopened message to user {UserID}. Did they leave or block the bot?", ticket);
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
                    var openTags = ticketsChannel.AvailableTags.Where(x => x.Name == "Open").Select(x => x.Id);
                    await thread.ModifyAsync(x =>
                    {
                        x.AppliedTags = openTags;
                    });

                    await thread.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithTitle("Ticket Reopened")
                        .WithDescription("This ticket has been reopened. Any messages sent here will be forwarded to the user."));
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to update ticket thread on reopening: ");
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
                if (args.Message.Content.StartsWith('!') || args.Message.Content.StartsWith('>'))
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

        private async Task<bool> HandleTicketsMessage(MessageCreatedEventArgs args)
        {
            var ticket = await m_Database.GetTicketByThread(args.Channel.Id);

            if (ticket == null)
            {
                return true;
            }

            if (ticket.Closed is not null)
            {
                if (m_ClosedWarningsCooldown.TryGetValue(args.Channel.Id, out var expires))
                {
                    if (expires > DateTime.UtcNow)
                    {
                        return true;
                    }
                }
                m_ClosedWarningsCooldown[args.Channel.Id] = DateTime.UtcNow.AddHours(2);

                await args.Message.RespondAsync("Your message was undelivered because this ticket has already been closed.\nYou can reopen the ticket with `!ticket reopen`");
                return true;
            }

            try
            {

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
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to forward ticket message to user");
                await args.Message.RespondAsync("Failed to forward message to user. Did they block the bot?\nYou can close this ticket with `!ticket close`");
                return false;
            }

            await m_Database.MarkTicketActive(ticket.ID);
            return true;
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

            if (!await ForwardDMMessage(args, ticket))
            {
                m_Logger.LogWarning("DM message forwarding failed! Marking ticket as closed.");
                await m_Database.CloseTicket(ticket, 0);
                try
                {
                    await args.Message.RespondAsync("Something went wrong while trying to add your message to the ticket. The ticket has been automatically closed. You can re-send your message to open a new ticket.");
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Failed to send ticket closure message to user. Did they leave the server or block the bot?");
                }

                return;
            }

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

        private async Task<bool> ForwardDMMessage(MessageCreatedEventArgs args, Ticket ticket)
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

            try
            {
                await m_Webhook.ExecuteAsync(userMessage);
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Failed to dispatch DM message to ticket channel. Was it deleted?");
                return false;
            }
            return true;
        }

        public async Task InitTicketSystem()
        {
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

                var webhooks = await ticketsChannel.GetWebhooksAsync();
                var webhook = webhooks.FirstOrDefault();

                string webhookUrl;

                if (webhook is not null)
                {
                    webhookUrl = webhook.Url;
                }
                else
                {
                    var newWebhook = await ticketsChannel.CreateWebhookAsync("TicketingSystem");
                    webhookUrl = newWebhook.Url;
                }

                await m_ConfigEditor.UpdateValueAsync("Tickets:WebhookUrl", webhookUrl);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error initializing ticket system: ");
            }
        }

        public async Task HandleEventAsync(DiscordClient sender, ThreadDeletedEventArgs eventArgs)
        {
            var ticket = await m_Database.GetTicketByThread(eventArgs.Thread.Id);

            if (ticket == null)
            {
                return;
            }

            if (ticket.Closed is not null)
            {
                return;
            }

            await m_Database.CloseTicket(ticket, 0);

            try
            {

                var targetUser = await m_Discord.GetUserAsync(ticket.UserID);

                var targetChannel = await targetUser.CreateDmChannelAsync();
                var userMessage = new DiscordEmbedBuilder()
                  .WithTitle("Ticket Closed")
                  .WithDescription("A moderator has marked your ticket as closed")
                  .WithColor(DiscordColor.Red);

                await targetChannel.SendMessageAsync(userMessage);

            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to send user ticket closure message after tickets thread was deleted");
            }
        }
    }
}
