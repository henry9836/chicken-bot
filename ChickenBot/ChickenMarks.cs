using ChickenBot.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.Extensions;
using Microsoft.Extensions.Hosting;

namespace ChickenBot;
public static class ChickenMarks
{
	public static Serilog.ILogger CreateLogger(IServiceCollection services)
	{
		var discordLogger = new DiscordLogger();

		services.AddSingleton(discordLogger);

		return new Serilog.LoggerConfiguration()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{CustomLevel}] [{source}] {Message:lj}{NewLine}{Exception}", theme: ChickenScratcher.Theme())
			.WriteTo.Sink(discordLogger, LogEventLevel.Information)
			.Enrich.FromLogContext()
			.Enrich.With<LogSourceEnricher>()
			.MinimumLevel.Debug()
			.Enrich.With<ChickenScratcher>()
			.WriteTo.File(path: Path.Combine("Logs", "_.log"), rollingInterval: RollingInterval.Day)
			.WriteTo.File(path: Path.Combine("Logs", "Errors", "error.log"), rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Warning)
			.CreateLogger();
	}
}

public class ChickenScratcher : ILogEventEnricher
{
	void ILogEventEnricher.Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		string msg;
		switch (logEvent.Level)
		{
			case LogEventLevel.Debug:
				msg = "\u001b[0;37m-";
				break;

			case LogEventLevel.Information:
				msg = "\u001b[0;37m+";
				break;

			case LogEventLevel.Warning:
				msg = "\u001b[1;33m!";
				break;

			case LogEventLevel.Error:
				msg = "\u001b[1;31mx";
				break;

			case LogEventLevel.Fatal:
				msg = "\u001b[1;31m\u001b[0;41mX";
				break;
			default:
				// Unknown log level
				msg = logEvent.Level.ToString();
				break;
		}
		logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CustomLevel", msg));
	}

	public static SystemConsoleTheme Theme()
	{
		Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle> customThemeStyles = new()
		{
			{
				ConsoleThemeStyle.Text, new SystemConsoleThemeStyle
				{
					Foreground = ConsoleColor.White,
				}
			},
			{
				ConsoleThemeStyle.SecondaryText, new SystemConsoleThemeStyle
				{
					Foreground = ConsoleColor.Red,
				}
			},
			{
				ConsoleThemeStyle.TertiaryText, new SystemConsoleThemeStyle
				{
					Foreground = ConsoleColor.DarkYellow,
				}
			},
		};

		return new SystemConsoleTheme(customThemeStyles);
	}
}