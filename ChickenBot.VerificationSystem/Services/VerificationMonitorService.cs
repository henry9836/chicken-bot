using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;

namespace ChickenBot.VerificationSystem.Services
{
    /// <summary>
    /// Monitors incoming messages to update the verification system, and apply verifications
    /// </summary>
    [Singleton]
    public class VerificationMonitorService : IEventHandler<MessageCreatedEventArgs>
    {
        private readonly IVerificationCache m_Cache;
        private readonly IUserVerifier m_Verifier;
        private readonly IUserFlagProvider m_FlagProvider;
        private readonly IConfiguration m_Configuration;

        private ulong m_BotChannel = 0;
        private ulong BotChannel
        {
            get
            {
                if (m_BotChannel == 0)
                {
                    m_BotChannel = m_Configuration.GetValue<ulong>("Channels:bot-spam", 0);
                }
                return m_BotChannel;
            }
        }

        public VerificationMonitorService(IVerificationCache cache, IUserVerifier verifier, IUserFlagProvider flagProvider, IConfiguration configuration)
        {
            m_Cache = cache;
            m_Verifier = verifier;
            m_FlagProvider = flagProvider;
            m_Configuration = configuration;
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
        {
            if (args.Author is not DiscordMember member)
            {
                // Do not increment messages from DMs to the bot
                return;
            }

            if (args.Channel.Id == BotChannel)
            {
                // Do not count messages from bot spam
                return;
            }

            if (!m_Cache.IncrementUserMessages(args.Author.Id))
            {
                // User is not meant to be verified rn
                return;
            }

            if (m_Verifier.CheckUserVerified(member))
            {
                // User is already verified
                return;
            }

            if (await m_FlagProvider.IsFlagSet(args.Author.Id, "NoVerify"))
            {
                // User isn't allowed to be verified
                return;
            }

            // User is meant to be verified, but isn't
            await m_Verifier.VerifyUserAsync(member);
            await m_Verifier.AnnounceUserVerification(member);
        }
    }
}
