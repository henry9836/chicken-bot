using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.Music.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Music.Commands
{
    [Category("Music")]
    public class MusicCommands : BaseCommandModule
    {
        private readonly IConfiguration m_Configuration;

        private readonly ILogger<MusicCommands> m_Logger;

        private readonly IServiceProvider m_Provider;

        private readonly IMusicClientRegistry m_ClientRegistry;

        public MusicCommands(IConfiguration configuration, ILogger<MusicCommands> logger, IServiceProvider provider, IMusicClientRegistry clientRegistry)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Provider = provider;
            m_ClientRegistry = clientRegistry;
        }

        [Command("Play"), Description("Plays music"), RequireVoiceOrBotSpam]
        public Task PlayAsync(CommandContext ctx, [RemainingText] string? query)
        {
            _ = Task.Run(async () =>
            {
                m_Logger.LogInformation("Starting play lookup...");
                try
                {
                    await PlayInternal(ctx, query);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error running play command");
                }
                finally
                {
                    m_Logger.LogInformation("Play lookup terminated");
                }
            });
            return Task.CompletedTask;
        }

        private async Task PlayInternal(CommandContext ctx, string? query)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: !string.IsNullOrWhiteSpace(query));
            if (client == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                if (client.IsPaused)
                {
                    await client.ResumeAsync();
                    var embed = new DiscordEmbedBuilder()
                                        .WithTitle("Paused Resumed")
                                        .WithRequestedBy(ctx.User);
                    await ctx.RespondAsync(embed);
                }
                else
                {
                    await ctx.RespondAsync($"Playback is not paused. If you meant to play something, use `!play [query or url]`");
                }
                return;
            }

            await ctx.TryReactAsync("toothless_think");
            try
            {
                await client.PlayAsync(ctx, query);
            }
            finally
            {
                await ctx.TryRemoveReactAsync("toothless_think");
            }
        }

        [Command("Skip"), Description("Skips the current track"), RequireVoiceOrBotSpam]
        public async Task SkipAsync(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }

            await client.RequestSkipAsync(ctx);
        }

        [Command("Queue"), Description("Displays the current queue"), RequireVoiceOrBotSpam]
        public async Task QueueCommand(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }
            await client.ShowQueue(ctx);
        }

        [Command("Shuffle"), Description("Shuffles the current queue"), RequireVoiceOrBotSpam]
        public async Task ShuffleCommand(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }
            await client.Shuffle(ctx);
        }

        [Command("Pause"), Description("Pauses playback"), RequireVoiceOrBotSpam]
        public async Task PauseCommand(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }

            if (client.IsPaused)
            {
                await ctx.RespondAsync("Playback is already paused. Resume it with `!Play`");
                return;
            }

            await client.PauseAsync();
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Paused Playback")
                .WithRequestedBy(ctx.User);
            await ctx.RespondAsync(embed);
        }

        [Command("Resume"), Description("Resumes playback"), RequireVoiceOrBotSpam]
        public async Task ResumeCommand(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }

            if (!client.IsPaused)
            {
                await ctx.RespondAsync("Playback is not paused.");
                return;
            }

            await client.ResumeAsync();
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Resumed Playback")
                .WithRequestedBy(ctx.User);
            await ctx.RespondAsync(embed);
        }

        [Command("Stop"), Description("Stops the music, and makes chicken leave the channel"), Aliases("leave"), RequireVerified, RequireVoiceOrBotSpam]
        public async Task StopCommand(CommandContext ctx)
        {
            var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
            if (client == null)
            {
                return;
            }

            await client.Destroy();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Music stopped")
                .WithRequestedBy(ctx.User);

            await ctx.RespondAsync(embed);
        }
    }
}
