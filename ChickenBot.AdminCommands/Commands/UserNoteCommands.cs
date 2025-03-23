using ChickenBot.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.AdminCommands.Commands
{
    [Group("user-note"), RequireBotManagerOrAdmin, Category("Admin")]

    public class UserNoteCommands : BaseCommandModule
    {
        [GroupCommand, RequireBotManager]
        public async Task UserNote(CommandContext ctx, [RemainingText] string? _)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Notes")
                .WithDescription(
@"Allows you to attach notes and warnings to users

Options:
`user-note create [user] {Title...}`
 ­ ­ ­↪ `{Note Content...}`: Creates a user note. Note content begins on a new line
`user-note list [user]`: Lists all notes for a user
`user-note get [note ID]`: Gets the full user note
`user-note search [query]`: Searches through all notes
`user-note delete [note ID]`: Deletes a user note

### Remarks:
The create note command takes the title and note content on different lines.
To use the command, specify the target user, then the rest of the first line is taken as the title.
Note content can contain multiple lines, and begins on the second line of the command.
**You can edit the note by editing your message** for a period after creating it.

Any links to images included with the note will be included as note media.
The first attached image, or image link contained in the note will be used as the note thumbnail.");
            await ctx.RespondAsync(embed);
        }
    }
}
