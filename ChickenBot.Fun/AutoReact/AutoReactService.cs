using ChickenBot.API;
using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Fun.AutoReact
{
	public class AutoReactService : IHostedService
	{

		private readonly DiscordClient m_Discord;

		private readonly ReactProvider m_Reacts;

		private readonly Random m_Random = new Random();

		private readonly ILogger<AutoReactService> m_Logger;

		public AutoReactService(DiscordClient discord, ReactProvider reacts, ILogger<AutoReactService> logger)
		{
			m_Discord = discord;
			m_Reacts = reacts;
			m_Logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated += OnMessageCreated;
			try
			{
				await m_Reacts.LoadAsync();
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Error loading auto reacts");
			}
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated -= OnMessageCreated;
			return Task.CompletedTask;
		}

		private async Task OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
		{
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
