using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;

namespace ChickenBot.ChatAI;

public class ChatAiCommands : BaseCommandModule
{
    private readonly ChatAISharedInfoService m_MyService;
    private readonly ILogger<ChatAiCommands> m_Logger;

    public ChatAiCommands(ChatAISharedInfoService aiService, ILogger<ChatAiCommands> logger)
    {
        m_MyService = aiService;
        m_Logger = logger;
    }
        
    [Command("MagicCorn"), Description("Overrides the main cooldown to start the next AI session")]
    public async Task MagicCornCommand(CommandContext ctx)
    {
        //TODO: Replace this check with a service thing for devs or something idk
        if ((ctx.User.Id != 102606498860896256) && (ctx.User.Id != 764761783965319189))
        {
            return;
        }

        m_MyService.m_MainCooldownThreshold = DateTime.MinValue;
        
        // Stealth :3
        await ctx.Message.DeleteAsync();
    }

    // Only for devs
    // TODO: Add admins and mods to be able to run this
    [Command("Shut"), Description("Stops and AI Session")]
    public async Task ShutCommand(CommandContext ctx)
    {
        //TODO: Replace this check with a service thing for devs or something idk
        if ((ctx.User.Id != 102606498860896256) && (ctx.User.Id != 764761783965319189))
        {
            return;
        }

        m_MyService.UpdateMainCooldown();
    }
}