using ChickenBot.API;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace ChickenBot.Core.Models
{
    public class ChickenHelpFormatter : BaseHelpFormatter
    {
        private DiscordEmbedBuilder? m_Embed;

        private readonly Dictionary<string, List<Command>> m_Categories = new Dictionary<string, List<Command>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IConfiguration m_Configuration;

        private ulong BotSpamID => m_Configuration.GetSection("Channels").GetValue("bot-spam", 0ul);

        public ChickenHelpFormatter(CommandContext ctx, IConfiguration configuration) : base(ctx)
        {
            m_Configuration = configuration;
        }

        public override CommandHelpMessage Build()
        {
            if (m_Embed != null)
            {
                return new CommandHelpMessage(null, m_Embed.Build());
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Help")
                .WithRequestedBy(Context.User)
                .WithColor(DiscordColor.CornflowerBlue);

            if (Context.Channel.Id != BotSpamID)
            {
                embed.WithDescription($"-# Hint: Some commands only show in the channels they can be used in.\n" +
                    $"-# Try this command in <#{BotSpamID}>!");
            }

            foreach (var category in m_Categories)
            {
                embed.AddField(category.Key, string.Join(", ", category.Value.Select(x => $"`{x.Name}`")));
            }

            return new CommandHelpMessage(null, embed.Build());
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            if (command.IsHidden)
            {
                m_Embed = new DiscordEmbedBuilder()
                    .WithDescription("Command not found.")
                    .WithRequestedBy(Context.User);
                return this;
            }

            m_Embed = new DiscordEmbedBuilder()
                .WithTitle($"Command Info: {command.Name}")
                .AddField("Category", command.Category ?? "Misc")
                .WithColor(DiscordColor.CornflowerBlue);

            if (command.Description is not null)
            {
                m_Embed.AddField("Description", command.Description);
            }

            if (command.Aliases.Any())
            {
                m_Embed.AddField("Aliases", string.Join(", ", command.Aliases.Select(x => $"`{x}`")));
            }

            if (command.Parent is not null)
            {
                m_Embed.AddField("Parent", command.Parent.Name);

            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            foreach (var command in subcommands)
            {
                if (command.IsHidden)
                {
                    continue;
                }

                if (command.Parent is not null)
                {
                    continue;
                }

                var checksFailed = command.RunChecksAsync(this.Context, true).Result.Any();

                if (checksFailed)
                {
                    continue;
                }

                if (command.Category != null)
                {
                    PushCommand(command, command.Category);
                }
                else if (command.Name.Equals("help", StringComparison.InvariantCultureIgnoreCase))
                {
                    PushCommand(command, "Utility");
                }
                else
                {
                    PushCommand(command, "Misc");
                }
            }

            return this;
        }

        private void PushCommand(Command command, string category)
        {
            if (m_Categories.TryGetValue(category, out var commands))
            {
                commands.Add(command);
                return;
            }
            commands = new List<Command>() { command };

            m_Categories[category] = commands;
        }
    }
}
