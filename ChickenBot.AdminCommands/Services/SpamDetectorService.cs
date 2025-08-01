﻿using System.Text;
using System.Text.RegularExpressions;
using ChickenBot.API;
using ChickenBot.API.Attributes;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands.Services
{
    [Singleton]
    public class SpamDetectorService : IEventHandler<MessageCreatedEventArgs>
    {
        private readonly DiscordClient m_Discord;
        private readonly ILogger<SpamDetectorService> m_Logger;
        private readonly IConfiguration m_Configuration;
        private readonly Regex m_DiscordInvite = new Regex(@"discord\.gg\/[a-zA-Z0-9]+");
        private readonly List<SuspiciousMessage> m_Messages = new List<SuspiciousMessage>();
        private readonly List<(ulong userID, DateTime active)> m_ActiveEnforcements = new List<(ulong userID, DateTime active)>();

        private ulong m_AlertRoleID => m_Configuration.GetSection("Roles").GetValue("staff", 0ul);
        private ulong m_ModeratorChannel => m_Configuration.GetSection("Channels").GetValue("moderation", 0ul);
        private ulong m_GuildID => m_Configuration.GetValue("GuildID", 0ul);

        public SpamDetectorService(DiscordClient discord, ILogger<SpamDetectorService> logger, IConfiguration configuration)
        {
            m_Discord = discord;
            m_Logger = logger;
            m_Configuration = configuration;
        }

        private bool CheckExcluded(DiscordMember member)
        {
            if (member.IsBot || member.Permissions.HasPermission(DiscordPermission.ManageMessages))
            {
                return true;
            }

            return false;
        }

        public async Task PushMessage(SuspiciousMessage message)
        {
            var lifetime = TimeSpan.FromMinutes(5);
            var enforcementLifetime = TimeSpan.FromMinutes(20);

            bool enforcementActive/*, enforcementRecent*/;

            lock (m_ActiveEnforcements)
            {
                m_ActiveEnforcements.RemoveAll(x => (x.active + enforcementLifetime) <= DateTime.Now);
                enforcementActive = m_ActiveEnforcements.Any(x => x.userID == message.Author.Id);
                //enforcementRecent = m_ActiveEnforcements.Any(x => x.userID == message.Author.Id && DateTime.Now.Subtract(x.active) <= lifetime);
            }

            if (enforcementActive)
            {
                await ReportMessages([message], "Messaged Deleted", enforcementActive, false);
                _ = Task.Run(message.DeleteAsync);
                return;
            }

            IEnumerable<SuspiciousMessage> authorMessages;
            HashSet<ulong> channels;
            HashSet<string> invites;
            int attemptedPing;
            var userID = message.Author.Id;

            lock (m_Messages)
            {
                // Purge
                m_Messages.RemoveAll(x => DateTime.Now - x.Posted >= lifetime);

                // Push
                m_Messages.Add(message);

                authorMessages = m_Messages.Where(x => x.Author.Id == message.Author.Id).ToArray(); // copy
                channels = new HashSet<ulong>(authorMessages.Select(x => x.Channel?.Id).Where(x => x != null).Select(x => x!.Value));
                invites = new HashSet<string>(authorMessages.Select(x => x.InviteLink));
                attemptedPing = authorMessages.Count(x => x.AttemptedPing);
            }

            var reason = $"AutoMod: Invite spam (in {channels.Count} channels, {attemptedPing} attempted mass pings, {invites.Count} unique invites";

            if (channels.Count >= 3 || enforcementActive)
            {
                if (attemptedPing >= 3) // Spamming invite links across multiple channels, while trying to @everyone or @here
                {
                    // Ban this fucker
                    await ReportMessages(authorMessages, "Timed out for 24 hours", enforcementActive, true);
                    PurgeMessages(authorMessages);
                    ActivateEnforcement(userID);

                    // For now, only time out
                    if (!enforcementActive)
                    {
                        await message.Author.TimeoutAsync(DateTimeOffset.UtcNow.AddHours(24), reason);
                    }
                }
                else // Spamming invite links across multiple channels, but not trying to ping
                {
                    // Timeout
                    await ReportMessages(authorMessages, "Timed out for 6 hours", enforcementActive, true);
                    PurgeMessages(authorMessages);
                    ActivateEnforcement(userID);

                    // For now, only time out
                    if (!enforcementActive)
                    {
                        await message.Author.TimeoutAsync(DateTimeOffset.UtcNow.AddHours(6), reason);
                    }
                }
            }
            else if (authorMessages.Count() >= 5)
            {
                if (attemptedPing >= 5) // spamming invite links in a single channel, while trying to @everyone or @here
                {
                    // Ban this fucker
                    await ReportMessages(authorMessages, "Timed out for 8 hours", enforcementActive, true);
                    PurgeMessages(authorMessages);
                    ActivateEnforcement(userID);

                    // For now, only time out
                    if (!enforcementActive)
                    {
                        await message.Author.TimeoutAsync(DateTimeOffset.UtcNow.AddHours(8), reason);
                    }
                }
                else if (invites.Count == 1) // spamming the same invite in a single channel
                {
                    // Timeout
                    await ReportMessages(authorMessages, "Timed out for 4 hours", enforcementActive, true);
                    PurgeMessages(authorMessages);
                    ActivateEnforcement(userID);

                    // For now, only time out
                    if (!enforcementActive)
                    {
                        await message.Author.TimeoutAsync(DateTimeOffset.UtcNow.AddHours(4), reason);
                    }
                }
                else
                {
                    // Timeout
                    await ReportMessages(authorMessages, "Timed out for 2 hours", enforcementActive, true);
                    PurgeMessages(authorMessages);
                    ActivateEnforcement(userID);

                    // For now, only time out
                    if (!enforcementActive)
                    {
                        await message.Author.TimeoutAsync(DateTimeOffset.UtcNow.AddHours(4), reason);
                    }
                }
            }
        }

        public void ActivateEnforcement(ulong userID)
        {
            lock (m_ActiveEnforcements)
            {
                m_ActiveEnforcements.Add((userID, DateTime.Now.AddMinutes(20)));
            }
        }

        public async Task ReportMessages(IEnumerable<SuspiciousMessage> messages, string actionTaken, bool enforcementActive, bool alert)
        {
            if (!messages.Any())
            {
                return;
            }

            var channel = await GetLogChannel();

            if (channel is not null)
            {
                var sb = new StringBuilder();
                foreach (var message in messages)
                {
                    var content = message.Content;
                    if (content.Length >= 200)
                    {
                        content = content.Substring(0, 200) + "...";
                    }

                    sb.AppendLine($"[#{message.Channel?.Name ?? "Unknown Channel"}] {content}");
                }

                var author = messages.First().Author;
                var joined = DateTime.UtcNow - author.JoinedAt;
                var created = DateTime.UtcNow - author.CreationTimestamp;

                var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Suspicious Messages Deleted")
                    .WithAuthor(author.Username, iconUrl: author.GetAvatarUrl(MediaFormat.WebP))
                    .WithDescription(sb.ToString())
                    .AddField("Action Taken", actionTaken, false)
                    .AddField("Username", author.Username, true)
                    .AddField("User ID", author.Id.ToString(), true)
                    .AddField("Account Joined", $"{joined.FormatTime()} ago", false)
                    .AddField("Account Created", $"{created.FormatTime()} ago", true)
                    .AddField("Account", $"<@{author.Id}>", true)
                    .WithColor(DiscordColor.Red);

                if (enforcementActive)
                {
                    embedBuilder.WithFooter("Enforcement against this user active");
                }

                var alertMessage = new DiscordMessageBuilder()
                    .AddEmbed(embedBuilder);

                if (m_AlertRoleID != 0 && alert)
                {
                    alertMessage
                        .WithContent($"<@&{m_AlertRoleID}>")
                        .WithAllowedMention(new RoleMention(m_AlertRoleID));
                }

                await channel.SendMessageAsync(alertMessage);
            }
        }

        public void PurgeMessages(IEnumerable<SuspiciousMessage> messages)
        {
            // purge messages
            _ = Task.Run(async () =>
            {
                foreach (var message in messages)
                {
                    lock (m_Messages)
                    {
                        if (!m_Messages.Remove(message))
                        {
                            continue;
                        }
                    }

                    await message.DeleteAsync();
                }
            });
        }
        private async Task<DiscordChannel?> GetLogChannel()
        {
            if (m_GuildID == 0 || m_ModeratorChannel == 0)
            {
                return null;
            }

            var guild = await m_Discord.GetGuildAsync(m_GuildID);

            var channel = await guild.GetChannelAsync(m_ModeratorChannel);

            return channel;
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
        {
            if (args.Author.IsBot || args.Author is not DiscordMember member)
            {
                return;
            }

            var content = args.Message.Content;
            var match = m_DiscordInvite.Match(content);

            var attemptedPing = content.Contains("@everyone") || content.Contains("@here");

            if (match.Success)
            {
                if (CheckExcluded(member))
                {
                    return;
                }

                var sus = new SuspiciousMessage(DateTime.Now, member, args.Message, attemptedPing, match.Value);
                try
                {
                    await PushMessage(sus);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error while trying to log auto-mod action");
                }
            }
        }
    }

    public class SuspiciousMessage
    {
        public DateTime Posted { get; }

        public DiscordMember Author { get; }

        public DiscordMessage Message { get; }

        public bool AttemptedPing { get; }

        public DiscordChannel? Channel { get; }

        public string InviteLink { get; }

        public string Content { get; }

        public bool Deleted { get; private set; } = false;

        public async Task DeleteAsync()
        {
            if (!Deleted)
            {
                Deleted = true;
                try
                {
                    await Message.DeleteAsync($"Message was flagged as spam");
                }
                catch (Exception)
                {
                }
            }
        }

        public SuspiciousMessage(DateTime posted, DiscordMember author, DiscordMessage message, bool attemptedPing, string link)
        {
            Posted = posted;
            Author = author;
            Message = message;
            AttemptedPing = attemptedPing;
            Channel = message.Channel;
            InviteLink = link;
            Content = message.Content;
        }
    }
}
