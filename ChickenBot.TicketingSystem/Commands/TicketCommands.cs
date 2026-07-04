using ChickenBot.API.Attributes;
using ChickenBot.TicketingSystem.Models;
using ChickenBot.TicketingSystem.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.TicketingSystem.Commands
{
    [Category("Admin"), RequireBotManagerOrStaff, Group("ticket")]
    public class TicketCommands : BaseCommandModule
    {
        private readonly TicketDatabase m_Database;
        private readonly TicketManager m_TicketManager;

        public TicketCommands(TicketDatabase database, TicketManager ticketManager)
        {
            m_Database = database;
            m_TicketManager = ticketManager;
        }

        [GroupCommand, RequireBotManagerOrStaff]
        public async Task TicketCommand(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
               .WithDescription(
                    "### Tickets\n" +
                    "All messages sent in a ticket thread are forwarded to the user, except for messages prefixed with `!` or `>`.\n" +
                    "Forwarded messages to the ticket user are anonymous.\n" +
                    "### Commands:\n\n" +
                    "`Ticket close`\n" +
                    "> Closes the current ticket\n\n" +
                     "`Ticket reopen`\n" +
                    "> Reopens the current ticket\n\n")
               .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}");

            await ctx.RespondAsync(embed);
        }

        [Command("close"), RequireBotManagerOrStaff]
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

        [Command("reopen"), RequireBotManagerOrStaff]
        public async Task ReOpenCommand(CommandContext ctx)
        {
            var ticket = await m_Database.GetTicketByThread(ctx.Channel.Id);

            if (ticket is null)
            {
                await ctx.RespondAsync("This command must be used inside a valid ticket thread");
                return;
            }

            if (ticket.Closed is null)
            {
                await ctx.RespondAsync("This ticket isn't closed");
                return;
            }

            var activeTicket = await m_Database.GetActiveTicketByUser(ticket.UserID);

            if (activeTicket is not null)
            {
                await ctx.RespondAsync($"Cannot reopen this ticket because <@{ticket.UserID}> has another active ticket (#{activeTicket.ID})");
                return;
            }

            await m_Database.ReopenTicket(ticket);

            await m_TicketManager.DoTicketReopen(ticket);
        }

        [Command("Init"), RequireBotManager]
        public async Task Init(CommandContext ctx)
        {
            await ctx.RespondAsync("Running ticket system init...");
            await m_TicketManager.InitTicketSystem();
            await ctx.RespondAsync("Ticketing system init completed. Check console for details.");
        }

        [Command("Purge-System"), RequireBotManager]
        public async Task PurgeCommand(CommandContext ctx, [RemainingText] string? value)
        {
            await ctx.RespondAsync("This command has been purposely disabled in the source code.");
            //if (value != "'Yes, I am very sure I want to run this command, and know that it will nuke the ticketing database, and all current tickets will be lost'")
            //{
            //    await ctx.RespondAsync("You didn't say the secret phrase, so you mustn't know what this command does.");
            //    return;
            //}

            //await m_Database.PurgeDatabaseSystem();

            //await ctx.RespondAsync("Ticket database purged.");
        }
    }
}
