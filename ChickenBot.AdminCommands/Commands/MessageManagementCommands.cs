using System.Diagnostics.CodeAnalysis;
using System.Text;
using ChickenBot.API;
using ChickenBot.API.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.AdminCommands.Commands
{
    [Category("Admin")]
    public class MessageManagementCommands : BaseCommandModule
    {
        [Command("PurgePeriod"), RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgePeriodCommand(CommandContext ctx)
        {
            await PurgeCommand(ctx);
        }

        [Command("Purge"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Purge Command")
                .WithDescription(
@"Deletes messages from the channel, by max number, or optionally by time period.
Does not delete pinned messages.

> `!Purge [Number of Messages] {User/UserID}`
Deletes the specified number of messages, optionally deleting that many messages by the specified user.
Cannot delete more than 400 messages without specifying a user.

> `!PurgePeriod [Time period] {User/UserID}`
Deletes messages based on how old they are
Example time period: '`2h`', '`20min`', '`5days`'
Cannot delete more than 6h of messages without specifying a user

> `!PurgeTo {User/UserID}`
Requires you to reply to a message.
Deletes messages up until the message you replied to.
Optionally only deletes messages from the specified user.
Cannot purge up to messages that are more than 24h old")
                .WithColor(DiscordColor.Red);
            await ctx.RespondAsync(embed);
        }

        [Command("Purge"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, int max)
        {
            if (ctx.Member is null)
            {
                return;
            }

            if (max > 400)
            {
                await ctx.RespondAsync("Max purge amount is 400");
                return;
            }

            var toDelete = AggregateMessages(ctx.Channel, max, exemptMessage: ctx.Message.Id);
            await DeleteMessagesAsync(toDelete, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {max} messages.");
        }

        [Command("Purge"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, int max, DiscordUser user)
        {
            if (ctx.Member is null)
                return;

            var toDelete = AggregateMessages(ctx.Channel, max, user.Id, ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(toDelete, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages from {user.GlobalName}");
        }

        [Command("Purge"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, int max, ulong userID)
        {
            if (ctx.Member is null)
                return;

            var toDelete = AggregateMessages(ctx.Channel, max, userID, ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(toDelete, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages from User ID: {userID}");
        }

        [Command("PurgePeriod"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, string time)
        {
            if (ctx.Member is null)
                return;

            if (!TryParseTime(time, out var timespan, out var error))
            {
                await ctx.RespondAsync($"Failed to parse time duration, {error}");
                return;
            }

            if (timespan.TotalHours > 6)
            {
                await ctx.RespondAsync("Max purge period without user filter is 6 hours!");
                return;
            }

            var messages = AggregateMessagesByTime(ctx.Channel, timespan, exemptMessage: ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages");
        }

        [Command("PurgePeriod"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, string time, DiscordUser user)
        {
            if (ctx.Member is null)
                return;

            if (!TryParseTime(time, out var timespan, out var error))
            {
                await ctx.RespondAsync($"Failed to parse time duration, {error}");
                return;
            }

            if (timespan.TotalHours > 14)
            {
                await ctx.RespondAsync("Max purge period is 14 days!");
                return;
            }

            var messages = AggregateMessagesByTime(ctx.Channel, timespan, user.Id, exemptMessage: ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages from {user.GlobalName}");
        }

        [Command("PurgePeriod"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeCommand(CommandContext ctx, string time, ulong userID)
        {
            if (ctx.Member is null)
                return;

            if (!TryParseTime(time, out var timespan, out var error))
            {
                await ctx.RespondAsync($"Failed to parse time duration, {error}");
                return;

            }

            if (timespan.TotalHours > 14)
            {
                await ctx.RespondAsync("Max purge period is 14 days!");
                return;
            }

            var messages = AggregateMessagesByTime(ctx.Channel, timespan, userID, exemptMessage: ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages from User ID: {userID}");
        }

        [Command("PurgeTo"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeToCommand(CommandContext ctx)
        {
            if (ctx.Message.ReferencedMessage is null)
            {
                await PurgeCommand(ctx);
                return;
            }

            if (ctx.Member is null)
            {
                return;
            }

            if ((DateTimeOffset.UtcNow - ctx.Message.ReferencedMessage.CreationTimestamp).TotalDays > 1)
            {
                await ctx.RespondAsync("That message is more than a day old");
                return;
            }

            var messages = AggregateMessagesTo(ctx.Channel, ctx.Message.ReferencedMessage.ReferencedMessage, 0, ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages");
        }

        [Command("PurgeTo"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeToCommand(CommandContext ctx, DiscordUser user)
        {
            if (ctx.Message.ReferencedMessage is null)
            {
                await PurgeCommand(ctx);
                return;
            }

            if (ctx.Member is null)
            {
                return;
            }

            if ((DateTimeOffset.UtcNow - ctx.Message.ReferencedMessage.CreationTimestamp).TotalDays > 1)
            {
                await ctx.RespondAsync("That message is more than a day old");
                return;
            }

            var messages = AggregateMessagesTo(ctx.Channel, ctx.Message.ReferencedMessage.ReferencedMessage, user.Id, ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages by {user.GlobalName}");
        }

        [Command("PurgeTo"),RequireBotPermissions(false, DiscordPermission.ManageRoles), RequireUserPermissions(false, DiscordPermission.ManageMessages)]
        public async Task PurgeToCommand(CommandContext ctx, ulong userID)
        {
            if (ctx.Message.ReferencedMessage is null)
            {
                await PurgeCommand(ctx);
                return;
            }

            if (ctx.Member is null)
            {
                return;
            }

            if ((DateTimeOffset.UtcNow - ctx.Message.ReferencedMessage.CreationTimestamp).TotalDays > 1)
            {
                await ctx.RespondAsync("That message is more than a day old");
                return;
            }

            var messages = AggregateMessagesTo(ctx.Channel, ctx.Message.ReferencedMessage.ReferencedMessage, userID, ctx.Message.Id);
            var deleted = await DeleteMessagesAsync(messages, ctx.Channel, ctx.Member);

            await ctx.Message.TryDeleteAsync();
            await SendEphemeral(ctx.Channel, $"Purged {deleted} messages by user ID: {userID}");
        }

        private async IAsyncEnumerable<DiscordMessage> AggregateMessagesTo(DiscordChannel channel, DiscordMessage aggregateTo, ulong targetUser = 0, ulong exemptMessage = 0)
        {
            var returned = 0;

            ulong targetMessage = 0;

            var exceededPeriod = false;

            DateTimeOffset purgeBefore = aggregateTo.CreationTimestamp;

            if ((DateTimeOffset.UtcNow - purgeBefore).TotalDays > 14)
            {
                purgeBefore = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(14));
            }

            while (!exceededPeriod)
            {
                IAsyncEnumerable<DiscordMessage> messages;

                if (targetMessage == 0)
                {
                    messages = channel.GetMessagesAsync(100);
                }
                else
                {
                    messages = channel.GetMessagesBeforeAsync(targetMessage, 100);
                }

                await foreach (var message in messages)
                {
                    if (targetUser != 0 && message.Author.Id != targetUser)
                    {
                        continue;
                    }

                    if (message.Pinned.HasValue && message.Pinned.Value)
                    {
                        continue;
                    }

                    if (message.Id == exemptMessage)
                    {
                        continue;
                    }

                    if (message.CreationTimestamp >= purgeBefore)
                    {
                        exceededPeriod = true;
                        continue;
                    }

                    yield return message;
                    returned++;
                }
            }
        }

        private async IAsyncEnumerable<DiscordMessage> AggregateMessagesByTime(DiscordChannel channel, TimeSpan maxAge, ulong targetUser = 0, int maxMessages = 400, ulong exemptMessage = 0)
        {
            var returned = 0;

            ulong targetMessage = 0;

            var exceededPeriod = false;

            while (!exceededPeriod)
            {
                IAsyncEnumerable<DiscordMessage> messages;

                if (targetMessage == 0)
                {
                    var remaining = maxMessages - returned;
                    messages = channel.GetMessagesAsync(Math.Min(remaining, 100));
                }
                else
                {
                    messages = channel.GetMessagesBeforeAsync(targetMessage, 100);
                }

                await foreach (var message in messages)
                {
                    if (targetUser != 0 && message.Author.Id != targetUser)
                    {
                        continue;
                    }

                    if (message.Pinned.HasValue && message.Pinned.Value)
                    {
                        continue;
                    }

                    if (message.Id == exemptMessage)
                    {
                        continue;
                    }

                    if ((DateTimeOffset.UtcNow - message.Timestamp) >= maxAge)
                    {
                        exceededPeriod = true;
                        continue;
                    }

                    yield return message;
                    returned++;

                    if (returned >= maxMessages)
                    {
                        break;
                    }
                }
            }
        }

        private async IAsyncEnumerable<DiscordMessage> AggregateMessages(DiscordChannel channel, int max, ulong user = 0, ulong exemptMessage = 0)
        {
            var returned = 0;

            ulong targetMessage = 0;

            var exceededPeriod = false;

            while (!exceededPeriod)
            {
                IAsyncEnumerable<DiscordMessage> messages;

                if (targetMessage == 0)
                {
                    messages = channel.GetMessagesAsync(100);
                }
                else
                {
                    messages = channel.GetMessagesBeforeAsync(targetMessage, 100);
                }

                await foreach (var message in messages)
                {
                    if (user != 0 && message.Author.Id != user)
                    {
                        continue;
                    }

                    if (message.Id == exemptMessage)
                    {
                        continue;
                    }

                    if (message.Pinned.HasValue && message.Pinned.Value)
                    {
                        continue;
                    }

                    if ((DateTimeOffset.UtcNow - message.Timestamp).TotalDays >= 14)
                    {
                        exceededPeriod = true;
                        continue;
                    }

                    yield return message;
                    returned++;

                    if (returned >= max)
                    {
                        break;
                    }
                }
            }
        }

        private async Task SendEphemeral(DiscordChannel channel, string message)
        {
            var ephemeral = await channel.SendMessageAsync(message);
            _ = Task.Delay(2000).ContinueWith(async _ =>
            {
                if (ephemeral is not null)
                {
                    await ephemeral.DeleteAsync("ephemeral message");
                }
            });
        }

        private async Task<int> DeleteMessagesAsync(IAsyncEnumerable<DiscordMessage> messages, DiscordChannel channel, DiscordMember caller)
        {
            var paginated = messages
                .Filter(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays < 14)
                .Paginate(100);

            var count = 0;

            await foreach (var page in paginated)
            {
                count += page.Count;
                await channel.DeleteMessagesAsync(page, $"Purged by {caller.Username}");
            }

            return count;
        }

        private bool TryParseTime(string input, [NotNullWhen(true)] out TimeSpan time, [NotNullWhen(false)] out string? error)
        {
            error = null;
            var numeric = new StringBuilder();
            var scale = new StringBuilder();
            var hadDecimal = false;

            var readingNumeric = true;

            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                if (readingNumeric)
                {
                    if (char.IsDigit(ch))
                    {
                        numeric.Append(ch);
                        continue;
                    }
                    else if (ch == '.' && !hadDecimal)
                    {
                        hadDecimal = true;
                        numeric.Append(ch);
                        continue;
                    }
                    else
                    {
                        readingNumeric = false;
                    }
                }

                scale.Append(ch);
            }

            var numericStr = numeric.ToString();

            if (!double.TryParse(numericStr, out var multiplier))
            {
                error = $"Invalid number: {numericStr}";
                time = TimeSpan.Zero;
                return false;
            }

            var scales = new (double seconds, string[] names)[]
            {
                (1, ["s", "sec", "second", "seconds"]),
                (60, ["m", "min", "minute", "minutes"]),
                (60 * 60, ["h", "hour", "hours"]),
                (60 * 60 * 24, ["d", "day", "days"]),
                (60 * 60 * 24 * 7, ["w", "week", "weeks"])
            };

            var targetScale = scale.ToString().ToLowerInvariant();
            var matchingScale = scales.FirstOrDefault(x => x.names.Contains(targetScale));

            if (matchingScale.seconds == 0)
            {
                error = $"Unknown scale: {scale}";
                time = TimeSpan.Zero;
                return false;
            }

            time = TimeSpan.FromSeconds(matchingScale.seconds * multiplier);
            return true;
        }
    }
}
