//using System.Data;
//using System.Net;
//using System.Reflection;
//using ChickenBot.API;
//using ChickenBot.API.Attributes;
//using ChickenBot.API.Models;
//using ChickenBot.Music.TrackResolve.Models;
//using DSharpPlus.CommandsNext;
//using DSharpPlus.Entities;
//using DSharpPlus.Lavalink;
//using Microsoft.Extensions.Logging;

//namespace ChickenBot.Music.TrackResolve
//{
//	[Singleton]
//	public class TrackResolver
//	{
//		private List<RegisteredResolver> m_Resolvers = new List<RegisteredResolver>();

//		private readonly IServiceProvider m_Provider;

//		private readonly ILogger<TrackResolver> m_Logger;

//		public TrackResolver(IServiceProvider provider, ILogger<TrackResolver> logger)
//		{
//			m_Provider = provider;
//			m_Logger = logger;
//		}

//		public void RegisterResolver<T>() where T : class
//		{
//			var instance = m_Provider.ActivateType<T>();

//			foreach (var methodInfo in typeof(T).GetMethods())
//			{
//				var attribute = methodInfo.GetCustomAttribute<TrackResolverAttribute>();

//				if (attribute == null)
//				{
//					continue;
//				}

//				if (attribute.Hosts.Length == 0)
//				{
//					m_Resolvers.Add(new RegisteredResolver(methodInfo, instance, null));
//					continue;
//				}

//				foreach (var host in attribute.Hosts)
//				{
//					m_Resolvers.Add(new RegisteredResolver(methodInfo, instance, host));
//				}
//			}
//		}

//		public IAsyncEnumerable<LavalinkTrack> ResolveTracks(string query, LavalinkNodeConnection node, LavalinkGuildConnection guild, CommandContext? ctx = null)
//		{
//			var container = new ArgumentContainer(m_Provider)
//				.WithUnnamedArguments(node, guild);

//			if (ctx is null)
//			{
//				container.WithUnnamedArguments(Optional.FromNoValue<CommandContext>());
//			}
//			else
//			{
//				container.WithUnnamedArguments(Optional.FromValue(ctx));
//			}

//			if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
//			{
//				return ResolveUri(uri, container);
//			}
//			return ResolveQuery(query, container);
//		}

//		public IAsyncEnumerable<LavalinkTrack> ResolveQuery(string query, ArgumentContainer container)
//		{
//			container.WithUnnamedArguments(query); // Register query to container

//			var miscHandlers = m_Resolvers.Where(x => x.ValidDomain == null);

//			return ExecuteHandlers(miscHandlers, container);
//		}

//		public IAsyncEnumerable<LavalinkTrack> ResolveUri(Uri uri, ArgumentContainer container)
//		{
//			container
//				.WithNamedArguments(GetUriParameters(uri)) // Add uri parameters to container
//				.WithUnnamedArguments(uri);

//			var host = uri.Host.Replace("www.", string.Empty);
//			var hostAndPath = host + uri.LocalPath;

//			var miscHandlers = m_Resolvers.Where(x => x.ValidDomain != null && x.ValidDomain.IsMatch(hostAndPath));

//			return ExecuteHandlers(miscHandlers, container);
//		}

//		private Dictionary<string, string> GetUriParameters(Uri uri)
//		{
//			var uriParts = uri.Query.Split('?', '&');
//			var parameters = new Dictionary<string, string>();

//			foreach (var part in uriParts)
//			{
//				var index = part.IndexOf('=');

//				if (index == -1)
//				{
//					continue;
//				}

//				var key = part.Substring(0, index);
//				var value = part.Substring(index + 1);

//				parameters[key] = WebUtility.UrlDecode(value);
//			}

//			return parameters;
//		}

//		private IAsyncEnumerable<LavalinkTrack> ExecuteHandlers(IEnumerable<RegisteredResolver> resolvers, ArgumentContainer container)
//		{
//			foreach (var resolver in resolvers)
//			{
//				if (!container.TryFormatArguments(resolver.Method, out var arguments))
//				{
//					continue;
//				}

//				object? result;

//				try
//				{
//					result = resolver.Method.Invoke(resolver.Instance, parameters: arguments);
//				}
//				catch (TrackResolveFailedException)
//				{
//					// Handler couldn't fulfil the request
//					continue;
//				}
//				catch (Exception ex)
//				{
//					// Something went wrong...
//					m_Logger.LogError(ex, "Error running track lookup");
//					continue;
//				}

//				if (result == null)
//				{
//					continue;
//				}

//				return ParseResponse(result);
//			}

//			return Empty();
//		}

//		/// <summary>
//		/// Workaround for creating an empty async enumerable
//		/// </summary>
//		/// <returns>Empty IAsyncEnumerable</returns>
//		private async IAsyncEnumerable<LavalinkTrack> Empty()
//		{
//			await Task.CompletedTask; // Satisfy await condition
//			yield break; // Satisfy enumerable condition
//		}

//		private async IAsyncEnumerable<LavalinkTrack> ParseResponse(object handlerResult)
//		{
//			// Get result object

//			var result = handlerResult;
//			if (result is Task task)
//			{
//				// await completion
//				await task;

//				var taskType = task.GetType();

//				var resultProperty = taskType.GetProperty("Result");

//				if (resultProperty == null)
//				{
//					// Task does not have a return type
//					yield break;
//				}

//				// Get return value of task
//				result = resultProperty.GetValue(task);
//			}

//			// Parse result object

//			if (result is null)
//			{
//				yield break;
//			}
//			else if (result is LavalinkTrack track)
//			{
//				yield return track;
//			}
//			else if (result is IEnumerable<LavalinkTrack> enumerable)
//			{
//				foreach (var value in enumerable)
//				{
//					yield return value;
//				}
//			}
//			else if (result is IAsyncEnumerable<LavalinkTrack> asyncEnumerable)
//			{
//				await foreach (var value in asyncEnumerable)
//				{
//					yield return value;
//				}
//			}
//		}
//	}
//}
