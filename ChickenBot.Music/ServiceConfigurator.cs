//using ChickenBot.API.Interfaces;
//using DSharpPlus;
//using DSharpPlus.Lavalink;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using SpotifyAPI.Web;

//namespace ChickenBot.Music
//{
//	public class ServiceConfigurator : IServiceConfigurator
//	{
//		public void ConfigureService(IServiceCollection services)
//		{
//			services.AddSingleton(LavaLinkFactory);
//			services.AddTransient(SpotifyClientFactory);
//		}

//		private static LavalinkExtension LavaLinkFactory(IServiceProvider provider)
//		{
//			var discord = provider.GetRequiredService<DiscordClient>();
//			return discord.UseLavalink();
//		}

//		private static SpotifyClient SpotifyClientFactory(IServiceProvider provider)
//		{
//			var config = provider.GetRequiredService<IConfiguration>();

//			var clientID = config["Music:Spotify:ClientID"] ?? string.Empty;
//			var clientSecret = config["Music:Spotify:Secret"] ?? string.Empty;

//			var credentials = new ClientCredentialsAuthenticator(clientID, clientSecret);

//			var spotifyConfig = SpotifyClientConfig
//											.CreateDefault()
//											.WithAuthenticator(credentials);

//			return new SpotifyClient(spotifyConfig);
//		}
//	}
//}
