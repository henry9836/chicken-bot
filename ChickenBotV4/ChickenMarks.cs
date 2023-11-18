using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace ChickenBotV4;

public static class ChickenMarks
{
    public static Serilog.ILogger CreateLogger()
    {
        return new Serilog.LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{CustomLevel}] {Message:lj}{NewLine}{Exception}", theme: ChickenScratcher.Theme())
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .Enrich.With<ChickenScratcher>()
            .WriteTo.File(path: Path.Combine("Logs", "_.log"), rollingInterval: RollingInterval.Day)
            .WriteTo.File(path: Path.Combine("Logs", "Errors", "error.log"), rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
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
                msg = "-";
                break;

            case LogEventLevel.Information:
                msg = "+";
                break;

            case LogEventLevel.Warning:
                msg = "!";
                break;

            case LogEventLevel.Error:
                msg = "*";
                break;
            
            case LogEventLevel.Fatal:
                msg = "X";
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