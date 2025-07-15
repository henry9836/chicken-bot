namespace ChickenBot.TicketingSystem.Models
{
    public class Ticket
    {
        public int ID { get; set; }

        public ulong UserID { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastActive { get; set; }

        public DateTime? Closed { get; set; }

        public ulong ClosedBy { get; set; }

        public ulong ThreadID { get; set; }
    }
}
