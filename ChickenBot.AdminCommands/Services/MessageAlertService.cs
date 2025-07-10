using System.Text;
using ChickenBot.AdminCommands.Models;
using ChickenBot.AdminCommands.Models.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands.Services
{
    public class MessageAlertService : IEventHandler<MessageCreatedEventArgs>
    {
        private readonly DiscordClient m_Client;
        private readonly ILogger<MessageAlertService> m_Logger;
        private readonly MessageAlertBroker m_AlertBroker;
        private readonly IConfiguration m_Configuration;
        private ulong m_ModeratorChannel => m_Configuration.GetSection("Channels").GetValue("moderation", 0ul);
        private ulong m_GuildID => m_Configuration.GetValue("GuildID", 0ul);

        private DiscordChannel? m_Channel;

        private readonly List<MatchingClient> m_Matchers = new List<MatchingClient>();

        private bool m_Loaded = false;

        private async Task<DiscordChannel?> GetAlertChannel()
        {
            if (m_Channel is not null)
            {
                return m_Channel;
            }

            try
            {
                var guild = await m_Client.GetGuildAsync(m_GuildID);

                m_Channel = await guild.GetChannelAsync(m_ModeratorChannel);
                return m_Channel;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error fetching message alert channel");
                return null;
            }
        }

        public MessageAlertService(DiscordClient client, ILogger<MessageAlertService> logger, MessageAlertBroker alertBroker, IConfiguration configuration)
        {
            m_Client = client;
            m_Logger = logger;
            m_AlertBroker = alertBroker;
            m_Configuration = configuration;
        }

        private void HandleMessageCreated(MessageCreatedEventArgs args)
        {
            if (args.Author is not DiscordMember member || member.IsCurrent)
            {
                return;
            }

            for (int i = 0; i < m_Matchers.Count; i++)
            {
                var matcher = m_Matchers[i];

                if (member.IsBot && !matcher.Alert.AllowBots)
                {
                    continue;
                }

                if (matcher.Alert.MatchUsers.Count != 0)
                {
                    if (!matcher.Alert.MatchUsers.Contains(args.Author.Id))
                    {
                        continue;
                    }
                }

                if (m_Matchers[i].MatchesMessage(args.Message))
                {
                    // Hit!
                    var alert = matcher.Alert;

                    m_Logger.LogWarning("Message alert matched! Rule: {ID}, {Title}, Message: {message}", alert.ID, alert.Name, args.Message.JumpLink);

                    Task.Run(async () =>
                    {
                        try
                        {
                            var channel = await GetAlertChannel();

                            if (channel is null)
                            {
                                m_Logger.LogWarning("Message alert channel is null!");
                                return;
                            }

                            var author = await m_Client.GetUserAsync(alert.CreatedBy);
                            string? avatarUrl = null;

                            if (author is not null)
                            {
                                avatarUrl = author.GetAvatarUrl(MediaFormat.Png);
                            }

                            var trimmedContent = args.Message.Content;

                            if (trimmedContent.Length > 509)
                            {
                                trimmedContent = trimmedContent.Substring(0, 509) + "...";
                            }
                            else if (trimmedContent.Length == 0)
                            {
                                trimmedContent = "N/A";
                            }

                            var alertEmbed = new DiscordEmbedBuilder()
                                    .WithTitle("Message Alert")
                                    .WithDescription($"[`[Jump to message]`]({args.Message.JumpLink})")
                                    .AddField("Rule Name", alert.Name, true)
                                    .AddField("Rule ID", alert.ID.ToString(), true)
                                    .AddField("Triggering User", $"{member.Mention}", false)
                                    .AddField("Match Users", alert.MatchUsers.Any() ? string.Join(", ", alert.MatchUsers.Select(x => $"<@{x}>")) : "All Users", true)
                                    .AddField("Match Text", string.Join(", ", alert.MatchFor.Select(x => $"'{x}'")))
                                    .AddField("Triggering Content", trimmedContent)
                                    .WithFooter(author is not null ? $"Rule created by: {author.GlobalName}" : $"Rule created by: {alert.CreatedBy}", avatarUrl!)
                                    .WithColor(DiscordColor.Yellow);

                            var alertMessage = new DiscordMessageBuilder().AddEmbed(alertEmbed);

                            var pingBuilder = new StringBuilder();

                            foreach (var mentionUser in alert.AlertUsers)
                            {
                                pingBuilder.Append($"<@{mentionUser}> ");
                                alertMessage.AddMention(new UserMention(mentionUser));
                            }

                            foreach (var mentionRole in alert.AlertRoles)
                            {
                                pingBuilder.Append($"<&{mentionRole}> ");
                                alertMessage.AddMention(new RoleMention(mentionRole));
                            }

                            alertMessage.WithContent(pingBuilder.ToString());

                            await channel.SendMessageAsync(alertMessage);

                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogError(ex, "Error sending message alert! ");
                        }
                    });
                }
            }
        }

        public async Task LoadAsync()
        {
            m_Loaded = true;
            await m_AlertBroker.LoadAlerts();

            foreach (var alert in m_AlertBroker.Alerts)
            {
                m_Matchers.Add(new MatchingClient(alert));
            }

            m_AlertBroker.OnAlertCreated += OnAlertCreated;
            m_AlertBroker.OnAlertDeleted += OnAlertDeleted;
        }

        private void OnAlertDeleted(SerializedMessageAlert alert)
        {
            m_Matchers.RemoveAll(x => x.Alert.ID == alert.ID);
        }

        private void OnAlertCreated(SerializedMessageAlert alert)
        {
            m_Matchers.Add(new MatchingClient(alert));
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
        {
            if (!m_Loaded)
            {
                await LoadAsync();
            }

            try
            {
                HandleMessageCreated(eventArgs);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error performing alert message match: ");
                throw;
            }
        }

        private class MatchingClient
        {
            public SerializedMessageAlert Alert { get; }

            private WordMatcher[] m_Matchers;

            public MatchingClient(SerializedMessageAlert alert)
            {
                Alert = alert;
                var matchers = new WordMatcher[alert.MatchFor.Count];

                for (int i = 0; i < matchers.Length; i++)
                {
                    matchers[i] = new WordMatcher(alert.MatchFor[i], new IgnoreCharacters(), alert.Skip);
                }

                m_Matchers = matchers;
            }

            public bool MatchesMessage(DiscordMessage message)
            {
                for (int i = 0; i < m_Matchers.Length; i++)
                {
                    if (!m_Matchers[i].Matches(message.Content))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
