using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Test
{
	// Commands are declared inside of a class that inherits `BaseCommandModule`
	// When the plugin is loaded, all classes that inherit `BaseCommandModule` will have their commands automatically loaded from
	public class TestCommand : BaseCommandModule 
	{
		private readonly DatabaseContext m_Database;
		private readonly MyService m_MyService; // Our own custom service, see MyService.cs


		// Dependency injection is also available for command modules
		public TestCommand(DatabaseContext database, MyService myService)
		{
			m_Database = database;
			m_MyService = myService;
		}


		// Commands can only be declared in a class that inherits `BaseCommandModule`, otherwise they will not be detected
		[Command("hello")] // <- commands are annotated with the `Command` attribute. This sets the command name, and also specifies this methid *is* a command
		public async Task HelloCommand(CommandContext ctx) // all commands must take a first parameter of `CommandContext`
		{
			/*  if a user runs '!hello', this command will be run */
			await ctx.RespondAsync("Hello World!"); // This is how we respond to a command

			m_MyService.DoSomething();
		}

		[Command("hello")] // We can also overload commands, and specify extra arguments, such as `usersName`. These will be parsed from the user's input
		public async Task HelloCommand(CommandContext ctx, string usersName)
		{
			/*   if a user runs '!hello MoreTextHere', this command will be run */
			await ctx.RespondAsync($"Hello, {usersName}!"); // This is how we respond to a command
		}




		[Command("PingDatabase")]
		public async Task PingDatabase(CommandContext ctx)
		{
			// using will call .Dispose() when the method exits.
			// You need to do this to close the connection when we're done with it
			// So we don't leave random MySQL errors open, or cause memory leaks
			//     Don't forget this Nitro!
			using var connection = await m_Database.GetConnectionAsync(); // Opens a new connection to the MySQL Server

			// We can now use this connection 
			if (await connection.PingAsync())
			{
				await ctx.RespondAsync("Connected to the database!");
			}
			else
			{
				await ctx.RespondAsync("Something went wrong...");
			}

			// If you're having issues, remember to reference ChickenBot.Test, and also reference `MySQLConnector` from Nuget
			// You oc also need to reference DSharpPlus, DSharpPlus.CommandsNext, and Microsoft.Extensions.Hosting.Abstractions from nuget


			// When this method returns, since connection was declared with `using`, it will be disposed/closed
			// Remember using is like using a try ... finally block, with the finally block calling .Dispose()
		}
	}

	// IHostedServices will be automatically detected by the bot when it loads the plugins
	// These can have their dependencies injected, so you can request services/objects from the service provider
	public class TestNormalChat : IHostedService
	{
		// Good practice to use `private readonly` for services we inject, and don't ever replace
		// also good practive to prefix our private fields with `_` or `m_`, I'm just used to using `m_` from my first job as a .NET dev

		private readonly DiscordClient m_Client;

		// For logging, we use `ILogger<Class>`, where Class is the class/type sending the log
		// It is so we can easily identify which class is sending log messages
		private readonly ILogger<TestNormalChat> m_Logger;

		public TestNormalChat(DiscordClient client, ILogger<TestNormalChat> logger)
		{
			// Store the servives we have injected so we can use them in StartAsync
			m_Client = client;
			m_Logger = logger;
		}

		/// <summary>
		/// This method is called when the bot is starting up.
		/// You can add whatever code you want to run here to setup the plugin
		/// E.g., starting background tasks, doing some setup, whatever you need.
		/// This can be treated as an entrypoint to the plugin
		/// </summary>
		/// <param name="cancellationToken">You probably won't need to use this. This is so the bot can cancel startup/shutdown</param>
		//      \/  Notice how this method returns `Task`, that means it is an async method.
		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Client.MessageCreated += OnMessageCreated;

			// While this method is supposdely async, we don't await anything, so it isn't actually async.
			// So we return `Task.CompletedTask`, to return a Task object that indicates execution as finished
			return Task.CompletedTask;
		}

		private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
		{
			m_Logger.LogInformation("Message Received: {Text}", args.Message.Content);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Will be called when the bot is being gracefully shut down
		/// </summary>
		public Task StopAsync(CancellationToken cancellationToken)
		{
			// Same ordeal, async method, but not actually async
			return Task.CompletedTask;
		}
	}
}
