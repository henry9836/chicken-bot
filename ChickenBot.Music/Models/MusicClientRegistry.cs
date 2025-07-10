//using System.Collections.Concurrent;
//using ChickenBot.API;
//using ChickenBot.API.Attributes;
//using ChickenBot.Music.Interfaces;
//using DSharpPlus.CommandsNext;
//using DSharpPlus.Entities;
//using DSharpPlus.Lavalink;

//namespace ChickenBot.Music.Models
//{
//	[Singleton<IMusicClientRegistry>]
//	public class MusicClientRegistry : IMusicClientRegistry
//	{
//		private readonly LavalinkExtension m_Lavalink;

//		private readonly IServiceProvider m_Provider;

//		private readonly ConcurrentDictionary<ulong, ServerMusicClient> m_Clients = new ConcurrentDictionary<ulong, ServerMusicClient>();

//		public MusicClientRegistry(LavalinkExtension lavalink, IServiceProvider provider)
//		{
//			m_Lavalink = lavalink;
//			m_Provider = provider;
//		}

//		public bool TryGetClient(ulong clientId, out ServerMusicClient client)
//		{
//			return m_Clients.TryGetValue(clientId, out client!);
//		}

//		public async Task<ServerMusicClient?> GetOrOpenClient(CommandContext ctx, bool join = true)
//		{
//			if (ctx.User is not DiscordMember member)
//			{
//				return null;
//			}

//			if (TryGetClient(member.Guild.Id, out var existing))
//			{
//				if (existing.ConnectedChannelID == (member.VoiceState?.Channel?.Id ?? 1ul))
//				{
//					return existing;
//				}

//				// Client already exists, and is indicating it is connected to another channel

//				var state = ctx.Guild.CurrentMember.VoiceState;

//				if (state is null)
//				{
//					// Bot is not connected, something is wrong.
//					m_Clients.TryRemove(member.Guild.Id, out _);
//				}
//				else if (state.Channel.Id != member.VoiceState?.Channel?.Id)
//				{
//					// Double check the bot is actually connected to a different channel
//					await ctx.RespondAsync("The bot is currently in a different channel");
//					return null;
//				}
//			}

//			if (!join)
//			{
//				return null;
//			}

//			if (member.VoiceState?.Channel is null)
//			{
//				await ctx.RespondAsync("You are not connected to a voice channel.");
//				return null;
//			}

//			var node = m_Lavalink.GetIdealNodeConnection(member.VoiceState.Channel.RtcRegion);

//			if (node is null)
//			{
//				await ctx.RespondAsync("Music is currently unavailable");
//				return null;
//			}

//			var connection = await node.ConnectAsync(member.VoiceState.Channel);
//			await connection.StopAsync();

//			await ctx.RespondAsync($"Joined {member.VoiceState.Channel.Name}");

//			var client = m_Provider.ActivateType<ServerMusicClient>(member.Guild, ctx.Channel, node, connection);

//			m_Clients[member.Guild.Id] = client;

//			return client;
//		}

//		public void RaiseClientDisconnected(ulong guild)
//		{
//			m_Clients.TryRemove(guild, out _);
//		}
//	}
//}
