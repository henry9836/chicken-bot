using System.Data.Common;
using ChickenBot.API.Attributes;
using ChickenBot.Core.Models;

namespace ChickenBot.AdminCommands.Models
{
    [Transient]
    public class AdminDBClient
    {
        private readonly DatabaseContext m_Context;

        public AdminDBClient(DatabaseContext context)
        {
            m_Context = context;
        }

        public async Task<AdminUserNote?> GetUserNote(int noteID)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM `user_notes` WHERE ID=@0 LIMIT 1;";
            command.Parameters.AddWithValue("@0", noteID);

            var reader = await command.ExecuteReaderAsync();

            await using var noteReader = ReadNotesAsync(reader);

            if (!await noteReader.MoveNextAsync())
            {
                return null;
            }

            return noteReader.Current;
        }

        public async Task<List<AdminUserNote>> GetNotesForUserAsync(ulong userID)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM `user_notes` WHERE UserID=@0;";
            command.Parameters.AddWithValue("@0", userID);

            var reader = await command.ExecuteReaderAsync();

            await using var noteReader = ReadNotesAsync(reader);
            return await noteReader.CollectListAsync();
        }

        public async Task<List<AdminUserNote>> SearchNotesAsync(string query)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM `user_notes` WHERE Title LIKE @0 OR UserNote LIKE @0;";

            var modifiedQuery = '%' + query + '%';
            command.Parameters.AddWithValue("@0", modifiedQuery);

            var reader = await command.ExecuteReaderAsync();

            await using var noteReader = ReadNotesAsync(reader);
            return await noteReader.CollectListAsync();
        }

        public async Task<bool> DeleteNoteAsync(int noteID)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM `user_notes` WHERE ID=@0;";
            command.Parameters.AddWithValue("@0", noteID);

            var modified = await command.ExecuteNonQueryAsync();
            return modified > 0;
        }

        public async Task InsertUserNote(AdminUserNote note)
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "INSERT INTO `user_notes` (Title, UserID, Moderator, Created, UserNote, AttachedMedia) VALUES (@0, @1, @2, @3, @4, @5);";
            command.Parameters.AddWithValue("@0", note.Title);
            command.Parameters.AddWithValue("@1", note.UserID);
            command.Parameters.AddWithValue("@2", note.Moderator);
            command.Parameters.AddWithValue("@3", note.Created);
            command.Parameters.AddWithValue("@4", note.UserNote);
            command.Parameters.AddWithValue("@5", note.AttachedMedia);

            await command.ExecuteNonQueryAsync();
        }

        public async Task CheckSchema()
        {
            using var connection = await m_Context.GetConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText =
@"CREATE TABLE IF NOT EXISTS `user_notes` (
    `ID` INT NOT NULL AUTO_INCREMENT,
    `Title` VarChar(128) NOT NULL,
    `UserID` BIGINT UNSIGNED NOT NULL,
    INDEX `UserID_INDEX` (`UserID`),
    `Moderator` BIGINT UNSIGNED NOT NULL,
    `Created` DATETIME NOT NULL,
    `UserNote` VarChar(4000) NOT NULL,
    `AttachedMedia` VarChar(2000) NOT NULL,
    PRIMARY KEY (`ID`)
) ENGINE = InnoDB;";

            await command.ExecuteNonQueryAsync();
        }

        private async IAsyncEnumerator<AdminUserNote> ReadNotesAsync(DbDataReader reader)
        {
            while (await reader.ReadAsync())
            {
                var note = new AdminUserNote()
                {
                    ID = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    UserID = reader.GetFieldValue<ulong>(2),
                    Moderator = reader.GetFieldValue<ulong>(3),
                    Created = reader.GetDateTime(4),
                    UserNote = reader.GetString(5),
                    AttachedMedia = reader.GetString(6),
                };

                yield return note;
            }
        }
    }
}
