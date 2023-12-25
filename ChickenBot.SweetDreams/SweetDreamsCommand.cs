﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.SweetDreams;

public class SweetDreamsCommand : BaseCommandModule
{
    private readonly ulong FuriousMemberId = 693042484619509760;
    private readonly Random RandomGenerator;
    private DateTime m_SweatDreamsTimeout;
    private DiscordMember discordMember;
    
    private readonly String[] SweetDreamMessages =
    {
        "https://media.discordapp.net/attachments/206875238066028544/970993761691766784/Untitled_Artwork.png",
        "GO TO BED! <@693042484619509760>", "Bedtime! <@693042484619509760> <:chicken_smile:236628343758389249>",
        "<:toothless_upright:955240038302613514> <@693042484619509760> *smothers you to sleep with wings*"
    };

    private bool bIsInsideBedtimeHours()
    {
        // Get the current time in UTC since furi is in there
        DateTime utcNow = DateTime.UtcNow;

        // Check if the current time is between 10 PM and 6 AM
        int currentHour = utcNow.Hour;
        return (currentHour >= 22 || currentHour < 6);
    }
    
    [Command("sweetdreams"), Description("bedtime")]
    public async Task SweetDreams(CommandContext ctx)
    {
        // Only allow the command to be used now and then
        if (m_SweatDreamsTimeout > DateTime.Now)
        {
            return;
        }

        // Only allow the command during when furi should be sleeping
        if (!bIsInsideBedtimeHours())
        {
            return;
        }
        
        // Assign the discord member if it is invalid
        if (discordMember.Id == 0)
        {
            discordMember = await ctx.Guild.GetMemberAsync(FuriousMemberId, true);
            // If it failed return
            if (discordMember.Id == 0)
            {
                return;
            }
        }

        // Only continue if furi is online
        if (!discordMember.Presence.Status.Equals(UserStatus.Online))
        {
            return;
        }
        
        // Generate a random value between 0-100
        var Coin = RandomGenerator.Next(0, 100);
        
        // 15% chance of a vc dc
        if (Coin >= 75)
        {
            // Assign the vc channel into null with causes a kick
            await discordMember.ModifyAsync(model => model.VoiceChannel = null);
        }

        await ctx.Channel.SendMessageAsync(SweetDreamMessages[RandomGenerator.Next(0, SweetDreamMessages.Length - 1)]);

        m_SweatDreamsTimeout = DateTime.Now.AddMinutes(10);
    }
}