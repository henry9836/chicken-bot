using Serilog.Core;
using Serilog.Events;

namespace ChickenBot.Core.Models
{
	/// <summary>
	/// Not currently used. Adds a {soruce} log property to the logger
	/// </summary>
	public class LogSourceEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (!logEvent.Properties.ContainsKey("SourceContext"))
			{
				return;
			}

			if (logEvent.Properties["SourceContext"] is ScalarValue scalar && scalar.Value != null)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("source", scalar?.Value?.ToString()?.Split('.').Last()));
			}
		}
	}
}
