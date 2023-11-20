namespace ChickenBot.ChatAI.Models
{
	public struct ChatSettings
	{
		public string Prompt { get; set; }

		public bool UseNumericNames { get; set; }

		public int MaxUsernameLength { get; set; }

		public int WindowSize { get; set; }

		public string Model { get; set; }

		public double? Temerature { get; set; }

		public double? TopP { get; set; }

		public string[] StopSequences { get; set; }

		public int? MaxResponseTokens { get; set; }

		public double? FrequencyPenalty { get; set; }

		public double? PresencePenalty { get; set; }

		public IReadOnlyDictionary<string, float> LogitBiases { get; set; }
	}
}
