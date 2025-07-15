using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ChickenBot.TicketingSystem.Models
{
    [Singleton]
    public class TicketDatabase
    {
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<TicketDatabase> m_Logger;
        private readonly DatabaseContext m_Context;

        public TicketDatabase(IConfiguration configuration, ILogger<TicketDatabase> logger, DatabaseContext context)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Context = context;
        }

        public async Task CheckSchemaAsync()
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText =
@"CREATE TABLE IF NOT EXISTS `tickets` (
    `ID` INT NOT NULL AUTO_INCREMENT,
    `UserID` BIGINT UNSIGNED NOT NULL,
    INDEX `UserID_INDEX` (`UserID`),
    `Created` DATETIME NOT NULL,
    `LastActive` DATETIME NOT NULL,
    `Closed` DATETIME NULL,
    `ClosedBy` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `ThreadID` BIGINT UNSIGNED NOT NULL,
    PRIMARY KEY (`ID`)
) ENGINE = InnoDB;";

            await command.ExecuteNonQueryAsync();
        }

        public async Task<Ticket?> GetTicketAsync(int ID)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM `tickets` WHERE ID=@ID;";
            command.Parameters.AddWithValue("@ID", ID);

            using var reader = await command.ExecuteReaderAsync();

            return await ReadTickets(reader).FirstOrDefault();
        }

        public async Task<List<Ticket>> GetPendingAutoExpireTickets()
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            var expireCutOff = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3));

            command.CommandText = "SELECT * FROM `tickets` WHERE IsOpen='true' AND LastActive < @CutOff;";

            command.Parameters.AddWithValue("@CutOff", expireCutOff);

            using var reader = await command.ExecuteReaderAsync();

            return await ReadTickets(reader).CollectAsync();
        }

        public async Task MarkTicketActive(int ID)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE `tickets` SET LastActive=@LastActive WHERE ID = @ID;";

            command.Parameters.AddWithValue("@ID", ID);
            command.Parameters.AddWithValue("@LastActive", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        public async Task CreateTicket(Ticket ticket)
        {
            await InsertUpdate(ticket, true);
        }

        public async Task CloseTicket(Ticket ticket, ulong closedBy)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE `tickets` SET Closed=@Closed, ClosedBy=@ClosedBy WHERE ID = @ID;";

            command.Parameters.AddWithValue("@ID", ticket.ID);
            command.Parameters.AddWithValue("@Closed", DateTime.UtcNow);
            command.Parameters.AddWithValue("@ClosedBy", closedBy);

            await command.ExecuteNonQueryAsync();
            ticket.ClosedBy = closedBy;
        }

        public async Task ReopenTicket(Ticket ticket)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE `tickets` SET Closed=null, ClosedBy=0 WHERE ID = @ID;";

            command.Parameters.AddWithValue("@ID", ticket.ID);

            await command.ExecuteNonQueryAsync();
        }

        #region "Internal APIs"
        private async IAsyncEnumerable<Ticket> ReadTickets(MySqlDataReader dataReader)
        {
            while (await dataReader.NextResultAsync())
            {
                yield return new Ticket()
                {
                    ID = dataReader.GetInt32(0),
                    UserID = dataReader.GetUInt64(1),
                    Created = dataReader.GetDateTime(2),
                    LastActive = dataReader.GetDateTime(3),
                    Closed = await dataReader.IsDBNullAsync(4) ? null : dataReader.GetDateTime(4),
                    ClosedBy = dataReader.GetUInt64(5),
                    ThreadID = dataReader.GetUInt64(6)
                };
            }
        }

        private async Task InsertUpdate(Ticket ticket, bool updateKey = false)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO `tickets` (ID, UserID, Created, LastActive, Closed, ClosedBy, ThreadID) VALUES (@ID, @UserID, @Created, @LastActive, @Closed, @ClosedBy, @ThreadID) ON DUPLICATE KEY UPDATE `ID`=@ID, `UserID`=@UserID, `Created`=@Created, `LastActive`=@LastActive, `Closed`=@Closed, `ClosedBy`=@ClosedBy, `ThreadID`=@ThreadID;";

            command.Parameters.AddWithValue("@ID", ticket.ID);
            command.Parameters.AddWithValue("@UserID", ticket.UserID);
            command.Parameters.AddWithValue("@Created", ticket.Created);
            command.Parameters.AddWithValue("@LastActive", ticket.LastActive);
            command.Parameters.AddWithValue("@ThreadID", ticket.ThreadID);
            command.Parameters.AddWithValue("@ClosedBy", ticket.ClosedBy);

            if (ticket.Closed.HasValue)
            {
                command.Parameters.AddWithValue("@Closed", ticket.Closed.Value);
            }
            else
            {
                command.Parameters.AddWithValue("@Closed", null);
            }


            await command.ExecuteNonQueryAsync();

            if (updateKey)
            {
                ticket.ID = (int)command.LastInsertedId;
            }
        }

        #endregion
    }
}
