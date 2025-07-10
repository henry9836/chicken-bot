using ChickenBot.API;
using ChickenBot.API.Attributes;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Fun.AutoReact
{
    [Singleton]
    public class AutoReactService : IEventHandler<MessageCreatedEventArgs>
    {

        private readonly DiscordClient m_Discord;

        private readonly ReactProvider m_Reacts;

        private readonly Random m_Random = new Random();

        private readonly ILogger<AutoReactService> m_Logger;

        private bool m_Initialized = false;

        public AutoReactService(DiscordClient discord, ReactProvider reacts, ILogger<AutoReactService> logger)
        {
            m_Discord = discord;
            m_Reacts = reacts;
            m_Logger = logger;
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
        {
            if (!m_Initialized)
            {
                m_Initialized = true;
                await m_Reacts.LoadAsync();
            }

            var autos = m_Reacts.PersistentReacts.Where(x => x.User == args.Author.Id);

            foreach (var auto in autos)
            {
                bool shouldActivate = true;
                // Check activation chance
                if (auto.ActivationChance != 1f)
                {
                    var rng = m_Random.NextDouble();

                    shouldActivate = rng <= auto.ActivationChance;
                }

                if (auto.Discriminator != null)
                {
                    // Custom activation discriminator
                    if (!auto.Discriminator(auto, m_Reacts, shouldActivate))
                    {
                        continue;
                    }
                }
                else if (!shouldActivate)
                {
                    continue;
                }

                await args.Message.TryReactAsync(m_Discord, auto.Emoji);
            }
        }
    }
}
