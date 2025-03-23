namespace ChickenBot.AdminCommands.Models
{
    public class AdminUserNote
    {
        public int ID { get; set; }

        public string Title { get; set; } = string.Empty;

        public ulong UserID { get; set; }

        public ulong Moderator { get; set; }

        public DateTime Created { get; set; }

        public string UserNote { get; set; } = string.Empty;

        public string AttachedMedia { get; set; } = string.Empty;
    }
}
