using ChickenBot.FlagGame.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace ChickenBot.FlagGame
{
    /// <summary>
    /// Handles answers to the flag game, serves responses, and finalizes games
    /// </summary>
    public class FlagService : IEventHandler<MessageCreatedEventArgs>
    {
        private readonly FlagGameRegistry m_GameRegistry;
        private readonly ILogger<FlagService> m_Logger;
        private readonly DiscordClient m_Discord;
        private readonly Random m_Random;

        public FlagService(FlagGameRegistry gameRegistry, ILogger<FlagService> logger, DiscordClient discord)
        {
            m_GameRegistry = gameRegistry;
            m_Logger = logger;
            m_Discord = discord;
            m_Random = new Random();
        }



        /// <summary>
        /// Congradulate users on getting the flag correct
        /// </summary>
        private async Task SendCongratulatoryMessage(DiscordMessage message, GameInstance instance)
        {
            var responses = new string[]
            {
                $"{message.Author.Mention}, happy squawk That's the flag of {instance.Answer}",
                $"*does a small dance*, that's right {message.Author.Mention}, that's the flag of {instance.Answer}"
            };

            var response = responses[m_Random.Next(0, responses.Length)];

            await message.RespondAsync(response);

            m_Logger.LogInformation("User {user} guessed the flag correctly: {flag}", message.Author.Username, instance.Answer);

            await instance.UpdateMessage($"Winner: {message.Author.Username}");
        }

        /// <summary>
        /// This user sucks, laugh at them
        /// </summary>
        private async Task SendFailiureMessage(DiscordMessage message, GameInstance game, string input)
        {
            var responses = new string[]
            {
                $"{message.Author.Mention}, pecks your foot dejectedly That's the flag of {game.Answer}",
                $"*stares at you disappointedly*, that's the flag of {game.Answer}."
            };

            var response = responses[m_Random.Next(0, responses.Length)];

            await message.RespondAsync(response);

            // A little funny for bot logs
            var logComments = new string[]
            {
                "~Laugh at this user.",
                "~What a fool.",
                "~But it was so easy??",
                "~Not sure how they got it wrong.",
                "~Idiot.",
                "~Bumbling buffoon.",
                "~How!?",
                "~They fucked that one up.",
                "~They skipped one too many geography classes"
            };

            var comment = logComments[m_Random.Next(logComments.Length)];

            m_Logger.LogInformation("User {user} guessed the flag incorrectly: '{answer}', Correct: '{flag}'. {comment}", message.Author.Username, input, game.Answer, comment);

            await game.UpdateMessage($"Game Over; Answer: {game.Answer}");
        }

        private bool IsCountryName(string countryName)
        {
            return m_GameRegistry.Flags.Any(x => x.Country.Equals(countryName, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
        {
            var inputAnswer = args.Message.Content.Trim();

            if (inputAnswer.StartsWith('!'))
            {
                // Don't evaluate commands
                return;
            }

            var referencedID = args.Message.ReferencedMessage?.Id ?? 0;

            if (referencedID != 0)
            {
                // Get game via referenced message
                var game = m_GameRegistry.TryGetGame(args.Channel.Id, referencedID);

                if (game == null)
                {
                    // Not a game
                    return;
                }

                if (game.Answer.Equals(inputAnswer, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Correct!
                    await SendCongratulatoryMessage(args.Message, game);
                }
                else
                {
                    // Failure
                    await SendFailiureMessage(args.Message, game, inputAnswer);
                }

                m_GameRegistry.TryFinalizeGame(game);
                return;
            }

            // No message referenced, try get game via channel context

            var lastGame = m_GameRegistry.GetLastGame(args.Channel.Id);

            if (lastGame == null)
            {
                return;
            }

            var timeSinceSent = DateTime.Now - lastGame.Posted;

            if (timeSinceSent > TimeSpan.FromMinutes(2))
            {
                // Game timed out
                m_GameRegistry.TryFinalizeGame(lastGame);
                return;
            }

            // Evaluate answer, ignore incorrect answers that are not a country name (misc chat messages)
            if (lastGame.Answer.Equals(inputAnswer, StringComparison.InvariantCultureIgnoreCase))
            {
                await SendCongratulatoryMessage(args.Message, lastGame);
                m_GameRegistry.TryFinalizeGame(lastGame);
            }
            else if (IsCountryName(inputAnswer))
            {
                // Failure
                await SendFailiureMessage(args.Message, lastGame, inputAnswer);
                m_GameRegistry.TryFinalizeGame(lastGame);
            }
        }
    }
}
