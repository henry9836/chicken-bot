//using ChickenBot.Music.TrackResolve;
//using ChickenBot.Music.TrackResolve.Resolvers;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace ChickenBot.Music.Services
//{
//	public class ResolverSetupService : IHostedService
//	{
//		private readonly TrackResolver m_Resolver;

//		private readonly ILogger<ResolverSetupService> m_Logger;

//		public ResolverSetupService(TrackResolver resolver, ILogger<ResolverSetupService> logger)
//		{
//			m_Resolver = resolver;
//			m_Logger = logger;
//		}

//		public Task StartAsync(CancellationToken cancellationToken)
//		{
//			// Music module is disabled
//			return Task.CompletedTask;

//			m_Logger.LogInformation("Setting up track resolvers...");

//			// Register Resolvers here
//			m_Resolver.RegisterResolver<YouTubeResolver>();
//			m_Resolver.RegisterResolver<SoundCloudResolver>();
//			m_Resolver.RegisterResolver<BandCampResolver>();
//			m_Resolver.RegisterResolver<VimeoResolver>();
//			m_Resolver.RegisterResolver<TwitchResolver>();
//			m_Resolver.RegisterResolver<SpotifyResolver>();

//			return Task.CompletedTask;
//		}

//		public Task StopAsync(CancellationToken cancellationToken)
//		{
//			return Task.CompletedTask;
//		}
//	}
//}
