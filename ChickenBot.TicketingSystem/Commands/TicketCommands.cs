using ChickenBot.TicketingSystem.Models;
using ChickenBot.TicketingSystem.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.TicketingSystem.Commands
{
    [Category("Admin"), Group("ticket")]
    public class TicketCommands : BaseCommandModule
    {
        private readonly TicketDatabase m_Database;
        private readonly TicketManager m_TicketManager;

        public TicketCommands(TicketDatabase database, TicketManager ticketManager)
        {
            m_Database = database;
            m_TicketManager = ticketManager;
        }

        [Command("close")]
        public async Task CloseCommand(CommandContext ctx)
        {
            var ticket = await m_Database.GetTicketByThread(ctx.Channel.Id);

            if (ticket is null)
            {
                await ctx.RespondAsync("This command must be used inside a valid ticket thread");
                return;
            }

            if (ticket.Closed is not null)
            {
                await ctx.RespondAsync("This ticket is already closed");
                return;
            }

            await m_TicketManager.DoTicketClosure(ticket, false);
            await m_Database.CloseTicket(ticket, ctx.Message.Author!.Id);
        }
    }
}
