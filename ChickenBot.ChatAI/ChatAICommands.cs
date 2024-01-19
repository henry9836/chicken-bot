using ChickenBot.API.Attributes;
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
        
    [Command("MagicCorn"), Description("Overrides the main cooldown to start the next AI session"), RequireBotManager, Category("Developer")]
    public async Task MagicCornCommand(CommandContext ctx)
    {
        m_MyService.m_MainCooldownThreshold = DateTime.MinValue;
        
        // Stealth :3
        await ctx.Message.DeleteAsync();
    }

    [Command("Ask"), Description("Overrides all cooldowns to ask a debug question to the AI"), RequireBotManager, Category("Developer")]
    public async Task AskCommand(CommandContext ctx)
    {
        return;
    }

    [Command("Shut"), Description("Stops an AI Session"), RequireBotManagerOrStaff, Category("Admin")]
    public Task ShutCommand(CommandContext ctx)
    {
        m_MyService.UpdateMainCooldown();
        return Task.CompletedTask;
    }
}