using ChickenBot.API;
using DSharpPlus;
using Microsoft.Extensions.Hosting;

namespace ChickenBot.Fun.Misc
{
	// Tek: Can I make Norex pregnant?
	// Tek: I should probably elaborate on that...

	public class ReactService : IHostedService
	{
		private readonly DiscordClient m_Discord;

		private readonly ReactState m_Reacts;

		public ReactService(DiscordClient discord, ReactState reacts)
		{
			m_Discord = discord;
			m_Reacts = reacts;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated += OnMessageCreated;
			return Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated -= OnMessageCreated;
			return Task.CompletedTask;
		}

		private async Task OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
		{
			if (m_Reacts.PregnantManReact.Contains(args.Message.Author.Id))
			{
				await args.Message.TryReactAsync(m_Discord, ":pregnant_man:");
			}
		}
	}
}
