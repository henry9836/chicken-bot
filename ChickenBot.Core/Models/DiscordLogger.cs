using Serilog.Core;
using Serilog.Events;

namespace ChickenBot.Core.Models
{
	public class DiscordLogger : ILogEventSink
	{
		public Queue<LogEvent> EventQueue { get; } = new Queue<LogEvent>();

		public void Emit(LogEvent logEvent)
		{
			EventQueue.Enqueue(logEvent);
		}
	}
}
