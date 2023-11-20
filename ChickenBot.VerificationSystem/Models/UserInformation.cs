namespace ChickenBot.VerificationSystem.Models
{
	public struct UserInformation
	{
		public int MaxCycle { get; } = 3;
		public ulong UserID { get; }
		public uint MessageCount { get; set; }
		public int Threshold { get; set; }
		public int CycleLevel { get; set; }

		public DateTime Eligible { get; set; }

		public UserInformation()
		{
			UserID = 0;
			MessageCount = 0;
			Threshold = 9999;
			CycleLevel = -1;
			Eligible = DateTime.UtcNow.AddDays(1);
		}

		public UserInformation(ulong userId, uint messageCount, int threshold, DateTime eligible)
		{
			UserID = userId;
			MessageCount = messageCount;
			Threshold = threshold;
			CycleLevel = MaxCycle;
			Eligible = eligible;
		}

		public UserInformation(ulong userId, uint messageCount, int threshold, int inCycle, DateTime eligible)
		{
			UserID = userId;
			MessageCount = messageCount;
			Threshold = threshold;
			CycleLevel = inCycle;
			Eligible = eligible;
		}

		public bool IsOutOfCycles()
		{
			return CycleLevel <= 0;
		}
	}
}
