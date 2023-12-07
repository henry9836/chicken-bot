using ChickenBot.API;
using ChickenBot.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Fun
{
	public class APICommands : BaseCommandModule
	{
		private readonly ILogger<APICommands> m_Logger;

		public APICommands(ILogger<APICommands> logger)
		{
			m_Logger = logger;
		}

		[Command("Inspire"), Aliases("Inspiration"), Description("Generates an inspirational quote"), RequireBotSpam]
		public async Task InspireCommand(CommandContext ctx)
		{
			using var client = new HttpClient();

			string? url = null;
			try
			{
				using var response = await client.GetAsync("https://inspirobot.me/api?generate=true");
				response.EnsureSuccessStatusCode();

				url = await response.Content.ReadAsStringAsync();
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Error generating inspiration");
			}

			if (url == null)
			{
				await ctx.RespondAsync("Sorry, something went wrong :(");
				return;
			}

			var titles = new[]
			{
				"An inspirational quote just for you",
				"Hope this brightens up your day!",
				"Squark! Here you go!",
				"*Does an inspirational dance*",
				"*Does an interpretive dance*"
			};

			var rng = new Random();
			var title = titles[rng.Next(titles.Length)];

			var embed = new DiscordEmbedBuilder()
				.WithTitle(title)
				.WithImageUrl(url)
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("Cat"), Description("Sends a random cat"), RequireBotSpam]
		public async Task CatCommand(CommandContext ctx)
		{
			var rng = new Random();
			await ctx.RespondAsync(new DiscordEmbedBuilder()
				.WithTitle("Random cat")
				.WithImageUrl($"https://cataas.com/cat?nonce={rng.Next(999999)}")); // random nonce to prevent discord from caching the image
		}

		// Had to be disabled since the API was flooded with random shit
		/*
		[Command("CatFact"), Description("Gets a random cat fact"), RequireBotSpam]
		public async Task CatFactCommand(CommandContext ctx)
		{
			var fact = await APIHelper.JsonGet<string>("https://cat-fact.herokuapp.com/facts/random", "text");
			if (string.IsNullOrEmpty(fact))
			{
				await ctx.RespondAsync(":(");
				return;
			}

			await ctx.RespondAsync(fact);
		}
		*/

		[Command("Dog"), Description("Sends a random dog"), RequireBotSpam]
		public async Task DogCommand(CommandContext ctx)
		{
			var url = await APIHelper.JsonGet<string>("https://dog.ceo/api/breeds/image/random", "message");
			if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out _))
			{
				await ctx.RespondAsync(":(");
				return;
			}

			await ctx.RespondAsync(new DiscordEmbedBuilder()
				.WithTitle("Random dog")
				.WithImageUrl(url));
		}

		[Command("Fox"), Description("Sends a random fox"), RequireBotSpam]
		public async Task FoxCommand(CommandContext ctx)
		{
			var rng = new Random();
			var url = $"https://img.foxes.cool/fox/{rng.Next(950)}.jpg";
			await ctx.RespondAsync(new DiscordEmbedBuilder()
				.WithTitle("Random fox")
				.WithImageUrl(url));
		}

		[Command("Owl"), Hidden, RequireBotSpam]
		public async Task OwlCommand(CommandContext ctx)
		{
			var responses = new[]
			{
				"*Disappointed squark*",
				"*looks at you with disappointment*",
				"*looks at you with disappointment*", // prefer this response
				"*looks at you with disappointment*",
				"*looks at you with disappointment*",
				$"*pecks and chases* {ctx.User.Mention}"
			};
			await ctx.RespondRandom(responses);
		}
	}
}
