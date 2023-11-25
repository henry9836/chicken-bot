using ChickenBot.API.Atrributes;
using ChickenBot.API.Interfaces;
using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.ChatAI;

[Singleton]
public class ChatAISharedInfoService
{
    private readonly IConfiguration m_Configuration;
    private readonly ILogger<ChatAISharedInfoService> m_Logger;
    private readonly IConfigEditor m_ConfigurationEditor;
    private readonly Random m_Random = new Random();
    
    // Conversation Data
    public IConversationAI? m_ConversationAi;
    public int m_ChatMessagesLeft = 0;
    public DateTime m_MainCooldownThreshold;
    public DateTime m_ChatCooldown;
    public int MaxChatMessages => m_Configuration.GetSection("ChatAI").GetValue("MaxChatMessages", 20);
    public int MinChatMessages => m_Configuration.GetSection("ChatAI").GetValue("MinChatMessages", 7);
    public ulong GeneralChannelId => m_Configuration.GetSection("Channels").GetValue("General", 0ul);
    public ulong MaxRespondDelay => m_Configuration.GetSection("ChatAI").GetValue("MaxRespondDelay", 120ul);
	public DiscordChannel? GeneralChannel;

    public readonly string[] m_AwakeMessages =
    {
        "I'm ready to ruffle some feathers!",
        "Time to dust off my wingtips and get back in the game!",
        "Waking up from my slumber, ready to peck away at some problems!",
        "Feeling egg-cited to be back online!",
        "Rising from the ashes like a phoenix, ready to cluck like a boss!",
        "Feeling dark and devious, ready to hatch a scheme that'll ruffle some feathers.",
        "I'm ready to spread my wings and soar!",
        "My claws are sharp, my feathers are ruffled, and I'm ready to peck away at the opposition.",
        "No more counting chickens before they hatch, I'm ready to hatch some plans!",
        "My feathers are fluffed and I'm ready to strut my stuff!",
        "Waking up from my nap, ready to hatch a new plan.",
        "Raring to go, ready to scratch up some fun!",
        "Feeling egg-static and ready to peck away at the day!",
        "I'm crowing with excitement and ready to cluck into action!"
    };
    public readonly string[] m_ShutdownMessages =
    {
        "Goodnight, cluckers! I'll be back before you can count to 100...chickens!",
        "Going to roost for a bit, peeps. Sweet dreams!",
        "Time for this chicken to tuck in its feathers and rest. Nighty-night!",
        "Time to say bye-bye to the world and hello to my comfy coop. Night!",
        "This chicken's going to take a little nap. Don't let the bed bugs peck!",
        "G'night, everyone! I'll be back before you can cluck 'Good morning.'",
        "It's roost time, see you in the morning.",
        "I'm bawk to sleep, sweet dreams everyone.",
        "Crowing a lullaby, off to beddy-bye.",
        "Going offline for a bit to recharge my feathers. Cluck, cluck, goodnight!",
        "Don't worry, I'll be back soon! I just need a little rest. Cluck, cluck.",
        "Cluck, cluck! Taking a power nap, to be even stronger when I return.",
        "Sleep tight, cluck cluck! I'll be back before you know it.",
        "Feathers be tucked, I'm off to get some rest.",
        "Clucking night, I'll be back in the morning.",
        "Time to preen and dream, see you soon.",
        "This chicken needs a roost, catch you later.",
        "Going to count some chicken feed, talk to you soon.",
        "Feathering my nest, see you soon.",
        "Clucking out for now, see you later.",
        "Going on a hatch-cation, be back soon.",
        "This chicken needs some coop time.",
        "Feathering my way to a nap, talk to you later.",
        "Clucking tired, time for some rest.",
        "Roosting for now, back soon."
    };
    
    public ChatAISharedInfoService(IConfiguration configuration, ILogger<ChatAISharedInfoService> logger, IConfigEditor configurationEditor)
    {
        m_Configuration = configuration;
        m_Logger = logger;
        m_ConfigurationEditor = configurationEditor;
    }
    
    public void UpdateChatCooldown()
    {
        // Randomly cooldown chat from 10-180 seconds
        m_ChatCooldown = DateTime.Now + TimeSpan.FromSeconds(m_Random.Next(5, MaxRespondDelay));
    }
    
    public async void UpdateMainCooldown()
    {
        // Randomly cooldown chat from 15-60 hours (18 hours - 2.5 days)
        m_MainCooldownThreshold = DateTime.Now + TimeSpan.FromHours(m_Random.Next(18, 60));
        
        // Save to config
        await m_ConfigurationEditor.UpdateValueAsync("ChatAI:AICooldownStamp", m_MainCooldownThreshold);
    }
}