using ChickenBot.API.Interfaces;
using DSharpPlus;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.Music
{
	public class LavaLinkConfigurator : IServiceConfigurator
	{
		public void ConfigureService(IServiceCollection services)
		{
			services.AddSingleton(LavaLinkFactory);
		}

		private static LavalinkExtension LavaLinkFactory(IServiceProvider provider)
		{
			var discord = provider.GetRequiredService<DiscordClient>();
			return discord.UseLavalink();
		}
	}
}
