using System.Text;
using ChickenBot.AdminCommands.Models;
using ChickenBot.AdminCommands.Models.Data;
using ChickenBot.API;
using ChickenBot.API.Attributes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands.Commands
{
    [Group("message-alert"), RequireBotManagerOrAdmin, Category("Admin")]
    public class MessageAlertCommands : BaseCommandModule
    {
        private readonly MessageAlertBroker m_Broker;
        private readonly ILogger<MessageAlertCommands> m_Logger;
        private readonly DiscordClient m_Client;

        public MessageAlertCommands(MessageAlertBroker broker, ILogger<MessageAlertCommands> logger, DiscordClient client)
        {
            m_Broker = broker;
            m_Logger = logger;
            m_Client = client;
        }

        [GroupCommand, RequireBotManagerOrAdmin]
        public async Task Base(CommandContext ctx, [RemainingText] string? _)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Message Alerts")
                .WithDescription("Allows for the creation of rules which match words/phrases in messages, to alert specific users/roles\n" +
                "Options:\n" +
                "`message-alert list`: List current rules\n" +
                "`message-alert show [ID]`: Gets more information on a rule\n" +
                "`message-alert create {...}`: Creates a rule, see example for details\n" +
                "`message-alert example`: Displays an example rule creation command\n" +
                "`message-alert delete [ID]`: Deletes a rule\n" +
                "`message-alert show-create [id]`: Shows the creation command for a rule, to delete and re-create it with changes\n")

                .WithRequestedBy(ctx.User);
            await ctx.RespondAsync(embed);
        }

        [Command("list"), RequireBotManagerOrAdmin]
        public async Task List(CommandContext ctx)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < m_Broker.Alerts.Count; i++)
            {
                var alert = m_Broker.Alerts[i];
                sb.AppendLine($"[`{alert.ID}`] {alert.Name} ({alert.MatchFor.Count} phrase/s, {(alert.MatchUsers.Count == 0 ? "any user" : $"{alert.MatchUsers.Count} users")})");
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("No message alerts.");
            }

            var embed = new DiscordEmbedBuilder()
                .WithRequestedBy(ctx.User)
                .WithTitle("Message Alerts")
                .WithDescription(sb.ToString());

            await ctx.RespondAsync(embed);
        }

        [Command("show"), RequireBotManagerOrAdmin]
        public async Task Show(CommandContext ctx, int id)
        {
            var alert = m_Broker.Alerts.FirstOrDefault(x => x.ID == id);

            if (alert is null)
            {
                await ctx.RespondAsync("Alert rule not found.");
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithRequestedBy(ctx.User)
                .WithTitle("Message Alert")
                .AddField("ID", alert.ID.ToString(), true)
                .AddField("Name", alert.Name, true)
                .AddField("Created By", $"<@{alert.CreatedBy}>", true)
                .AddField("Allows Bots", alert.AllowBots.ToString(), true)
                .AddField("Skip", alert.Skip.ToString(), true)
                .AddField("Match Text", string.Join(", ", alert.MatchFor.Select(x => $"`{x.Replace("\n", "\\n").Replace("`", "\\`")}`")))
                .AddField("Match Users", alert.MatchUsers.Any() ? string.Join(", ", alert.MatchUsers.Select(x => $"<@{x}>")) : "Any User")
                .AddField("Alert Users", alert.AlertUsers.Any() ? string.Join(", ", alert.AlertUsers.Select(x => $"<@{x}>")) : "N/A", true)
                .AddField("Alert Roles", alert.AlertRoles.Any() ? string.Join(", ", alert.AlertRoles.Select(x => $"<&{x}>")) : "N/A", true);

            await ctx.RespondAsync(embed);
        }

        [Command("show-create"), RequireBotManagerOrAdmin]
        public async Task ShowCreate(CommandContext ctx, int id)
        {
            var alert = m_Broker.Alerts.FirstOrDefault(x => x.ID == id);

            if (alert is null)
            {
                await ctx.RespondAsync("Alert rule not found.");
                return;
            }

            var sb = new StringBuilder();
            var extended = new StringBuilder();
            sb.AppendLine($"Name = {alert.Name}");
            sb.AppendLine($"Skip = {alert.Skip}");
            sb.AppendLine($"AllowBots = {alert.AllowBots}");

            // Alert Users
            if (alert.AlertUsers.Count == 1)
            {
                sb.AppendLine();
                sb.AppendLine($"\\# {await GetUsername(alert.AlertUsers[0])}");
                sb.AppendLine($"AlertUsers = {alert.AlertUsers[0]}");
            }
            else if (alert.AlertUsers.Count > 1)
            {
                extended.AppendLine("[AlertUsers]");

                foreach (var user in alert.AlertUsers)
                {
                    extended.AppendLine($"\\# {await GetUsername(user)}");
                    extended.AppendLine(user.ToString());
                    extended.AppendLine();
                }
            }

            // Alert Roles
            if (alert.AlertRoles.Count == 1)
            {
                sb.AppendLine();
                sb.AppendLine($"\\# {await GetRoleName(alert.AlertRoles[0], ctx)}");
                sb.AppendLine($"AlertRoles = {alert.AlertRoles[0]}");
            }
            else if (alert.AlertRoles.Count > 1)
            {
                extended.AppendLine("[AlertRoles]");

                foreach (var role in alert.AlertRoles)
                {
                    extended.AppendLine($"\\# {await GetRoleName(role, ctx)}");
                    extended.AppendLine(role.ToString());
                    extended.AppendLine();
                }
            }

            //sb.AppendLine($"AlertUsers = {string.Join(", ", alert.AlertUsers)}");
            //sb.AppendLine($"AlertRoles = {string.Join(", ", alert.AlertRoles)}");

            // Match Users
            if (alert.MatchUsers.Count == 1)
            {
                sb.AppendLine();
                sb.AppendLine($"\\# {await GetRoleName(alert.MatchUsers[0], ctx)}");
                sb.AppendLine($"MatchUsers = {alert.MatchUsers[0]}");
            }
            else if (alert.MatchUsers.Count > 1)
            {
                extended.AppendLine("[MatchUsers]");

                foreach (var user in alert.MatchUsers)
                {
                    extended.AppendLine($"\\# {await GetUsername(user)}");
                    extended.AppendLine(user.ToString());
                    extended.AppendLine();
                }
            }

            //if (alert.MatchUsers.Count > 0)
            //{
            //    sb.AppendLine();
            //    sb.AppendLine("[MatchUsers]");
            //    foreach (var user in alert.MatchUsers)
            //    {
            //        sb.AppendLine(user.ToString());
            //    }
            //}

            if (alert.MatchFor.Count > 0)
            {
                extended.AppendLine();
                extended.AppendLine("[Match]");
                foreach (var match in alert.MatchFor)
                {
                    extended.AppendLine(match);
                }
            }
            extended.AppendLine();
            extended.AppendLine($"\\# Auto-generated creation command, from rule ID: {alert.ID}");
            extended.Append($"\\# Delete existing rule with: `!message-alert delete {alert.ID}`");

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Rule Creation Command")
                .WithDescription($"Use this creation command to modify a rule, by deleting it, and re-creating it.\nCopy & Paste the text below\n\n!message-alert create \n \\`\\`\\`ini\n{sb}\n{extended}\\`\\`\\`\n\n")
                .WithRequestedBy(ctx.User);

            await ctx.RespondAsync(embed);
        }

        private async Task<string> GetUsername(ulong userID)
        {
            try
            {
                var user = await m_Client.GetUserAsync(userID);

                return string.IsNullOrWhiteSpace(user.GlobalName) ? user.Username : user.GlobalName;
            }
            catch (Exception)
            {
                return "Unknown User";
            }
        }

        private async Task<string> GetRoleName(ulong roleID, CommandContext ctx)
        {
            try
            {
                if (ctx.Guild is null)
                {
                    return "Unknown Role";
                }

                var role = await ctx.Guild.GetRoleAsync(roleID);

                return role.Name;

            }
            catch (Exception)
            {
                return "Unknown Role";
            }
        }

        [Command("example"), RequireBotManagerOrAdmin]
        public async Task Example(CommandContext ctx)
        {
            var examplePayload = @"```ini
# (Mandatory) The name provides a friendly title
Name = ExampleRule

# (Optional) Max number of character skips.
#  E.g., the text 'helo' will match the word 'hello'    
#  with a skip of 1, but not with a skip of 0.
Skip = 1

# (Optional) Specifies if bots are to be included.
#  Default is false.
AllowBots = false

# (Optional) Limits the rule to only certain users.
#  Can be user mentions, or just the user ID.
MatchUsers = 726453597167943720, 102606498860896256

# (Optional) Specifies users to ping on alert
#  (A message is sent in auto-mod with the alert)
AlertUsers = 269672239245295617

# (Optional) Specifies roles to ping on alert
AlertRoles = 1220618454579679253

# This specifies words/phrases to look for. 
#  For a rule to match, it must contain all the words.
#  This rule will look for messages that contain 'volt'
#  and 'stinky' in the same message.
#  Searching is fuzzy, avoid including punctuation.
#  Section is optional, but a filter for message content (this) or match users is required
[Match]
Volt
Stinky
```";

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithTitle("Message Alert Example")
                .WithRequestedBy(ctx.User)
                .WithDescription(
                $"The creation command is multi-line, and cam be submitted in a code block.\n\n" +
                $"**Example Command:**\n\n" +
                $"!message-alert create\n" +
                $"{examplePayload}"));
        }

        [Command("delete"), RequireBotManagerOrAdmin]
        public async Task Delete(CommandContext ctx, int id)
        {
            var deleted = m_Broker.DeleteAlert(id);

            if (deleted)
            {
                await ctx.RespondAsync("Alert rule deleted.");
                return;
            }

            await ctx.RespondAsync("Failed to find an alert rule with that ID.");
        }

        [Command("create"), RequireBotManagerOrAdmin]
        public async Task Create(CommandContext ctx, [RemainingText] string cmd)
        {
            try
            {
                var lines = cmd.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var args = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

                string? section = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith('#'))
                        continue;

                    if (line.StartsWith("```"))
                        continue;

                    if (line.Contains('='))
                    {
                        var key = line.Split('=')[0];
                        args.AddOrAppend(key.Trim(), line.Substring(key.Length + 1).Trim());
                    }
                    else if (line.StartsWith('[') && line.EndsWith(']'))
                    {
                        section = line.Substring(1, line.Length - 2);
                    }
                    else if (section != null)
                    {
                        args.AddOrAppend(section, line);
                    }
                }

                if (!args.TryGetValue("Name", out var names) || names.Count == 0 || string.IsNullOrWhiteSpace(names[0]))
                {
                    await ctx.RespondAsync("Missing mandatory value: `Name`, or value is empty. Value specifies a friendly name for this rule, to easily identify it.");
                    return;
                }

                if (names.Count > 1)
                {
                    await ctx.RespondAsync("Ambiguous Value `Name` specified multiple times.");
                    return;
                }

                var skip = 0;

                if (args.TryGetValue("skip", out var skips) && skips.Count != 0)
                {

                    if (skips.Count > 1)
                    {
                        await ctx.RespondAsync("Ambiguous Value `Skip` specified multiple times.");
                        return;
                    }

                    if (!int.TryParse(skips[0].Trim(), out skip))
                    {
                        await ctx.RespondAsync("Invalid Value for 'Skip', value must be a whole number");
                        return;
                    }
                }

                args.TryGetValue("match", out var matchFor);
                matchFor ??= new List<string>();

                var matchUsers = new List<ulong>();

                if (args.TryGetValue("matchUsers", out var inputMatchUsers))
                {
                    foreach (var usr in inputMatchUsers)
                    {
                        var cleaned = usr.Replace("<@", "").Replace(">", "").Trim();

                        if (!ulong.TryParse(cleaned, out var usrl))
                        {
                            await ctx.RespondAsync($"Invalid value for `MatchUsers`: '{usr}'. Value must be User ID or Mention");
                            return;
                        }
                        matchUsers.Add(usrl);
                    }
                }

                var alertUsers = new List<ulong>();
                if (args.GetOrExpand("alertUsers", out var inputAlertUsers))
                {
                    foreach (var usr in inputAlertUsers)
                    {
                        var cleaned = usr.Replace("<@", "").Replace(">", "").Trim();

                        if (!ulong.TryParse(cleaned, out var usrl))
                        {
                            await ctx.RespondAsync($"Invalid value for `AlertUsers`: '{usr}'. Value must be User ID or Mention");
                            return;
                        }
                        alertUsers.Add(usrl);
                    }
                }

                var alertRoles = new List<ulong>();
                if (args.GetOrExpand("alertRoles", out var inputAlertRoles))
                {
                    foreach (var role in inputAlertRoles)
                    {
                        var cleaned = role.Replace("<&", "").Replace(">", "").Trim();

                        if (!ulong.TryParse(cleaned, out var usrl))
                        {
                            await ctx.RespondAsync($"Invalid value for `AlertRoles`: '{role}'. Value must be Role ID or Mention");
                            return;
                        }
                        alertRoles.Add(usrl);
                    }
                }

                var allowBots = false;

                if (args.TryGetValue("AllowBots", out var allowBotsStr) && allowBotsStr.Count == 1)
                {
                    if (!bool.TryParse(allowBotsStr[0], out allowBots))
                    {
                        await ctx.RespondAsync($"Invalid value for `AllowBots`: '{allowBotsStr[0]}'");
                        return;
                    }
                }

                var serialized = new SerializedMessageAlert()
                {
                    ID = m_Broker.GetNextID(),
                    Name = names[0],
                    CreatedBy = ctx.User.Id,
                    Skip = skip,
                    MatchFor = matchFor,
                    MatchUsers = matchUsers,
                    AlertRoles = alertRoles,
                    AlertUsers = alertUsers,
                    AllowBots = allowBots,
                };

                if (serialized.MatchFor.Count == 0 && serialized.MatchUsers.Count == 0)
                {
                    await ctx.RespondAsync("No filters specified, both `Match` and `MatchUsers` empty. Rule would target all messages!");
                    return;
                }

                m_Broker.CreateAlert(serialized);

                await ctx.RespondAsync($"Created message alert rule with ID {serialized.ID}.");
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync("Sorry, something went wrong. Check bot logs for details.");
                m_Logger.LogInformation(ex, "Error creating alert rule: ");
                throw;
            }
        }
    }
}
