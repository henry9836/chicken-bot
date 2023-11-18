using Serilog.Core;
using Serilog.Events;

namespace ChickenBot.Core.Models
{
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
