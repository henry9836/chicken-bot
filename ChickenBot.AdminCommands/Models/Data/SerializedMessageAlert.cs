namespace ChickenBot.AdminCommands.Models.Data
{
    public class SerializedMessageAlert
    {
        public int ID { get; set; }

        public string Name { get; set; } = string.Empty;

        public ulong CreatedBy { get; set; }

        public int Skip { get; set; }

        public bool AllowBots { get; set; }

        public List<string> MatchFor { get; set; } = new List<string>();

        public List<ulong> MatchUsers { get; set; } = new List<ulong>();


        public List<ulong> AlertUsers { get; set; } = new List<ulong>();

        public List<ulong> AlertRoles { get; set; } = new List<ulong>();
    }
}
