using ChickenBot.ChatAI.Interfaces;
using ChickenBot.ChatAI.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.ChatAI;

public class ChatAiService : IHostedService
{
    private readonly IConfiguration m_Configuration;
    private readonly ILogger<ChatAiService> m_Logger;
    private readonly DiscordClient m_Client;
    private readonly IConversationAIProvider m_AiProvider;
    private readonly Random m_Random = new Random();
    
    // Conversation Data
    private IConversationAI? m_ConversationAi;
    private int m_ChatMessagesLeft = 0;
    private DateTime m_MainCooldownThreshold;
    private DateTime m_ChatCooldown;
    private int MaxChatMessages => m_Configuration.GetSection("ChatAI").GetValue("MaxChatMessages", 20);
    private int MinChatMessages => m_Configuration.GetSection("ChatAI").GetValue("MinChatMessages", 10);
    private ulong GeneralChannelId => m_Configuration.GetSection("Channels").GetValue("general", 0ul);
    private DiscordChannel? GeneralChannel;
    
    
    public ChatAiService(DiscordClient client, IConfiguration configuration, ILogger<ChatAiService> logger, IConversationAIProvider aiProvider)
    {
        // Store the servives we have injected so we can use them in StartAsync
        m_Client = client;
        m_Logger = logger;
        m_Configuration = configuration;
        m_AiProvider = aiProvider;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        m_Client.MessageCreated += ClientOnMessageCreated;

        // Get the main cooldown, if can't find make a new one
        m_MainCooldownThreshold = m_Configuration.GetSection("ChatAI").GetValue("ChatCooldown", DateTime.MinValue);
        if (m_MainCooldownThreshold == DateTime.MinValue)
        {
            m_Logger.LogWarning("Couldn't find a cooldown value in the config file, generating a new one...");
            CreateNewMainCooldown();
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Dereference 
        m_Client.MessageCreated -= ClientOnMessageCreated;
        
        return Task.CompletedTask;
    }
    
    private async Task ClientOnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
    {
        // Filter out dms
        if (args.Author is not DiscordMember)
        {
            return;   
        }

        // If we are not in general
        if (args.Channel.Id != GeneralChannelId)
        {
            return;
        }
        
        // Get messages
        if (m_ConversationAi != null)
        {
            m_ConversationAi.PushChatMessage(args.Author, args.Message.Content);
        }
        
        // Main Cooldown expired?
            // No, then retuurn
        // Running Cooldown expired?
            // No, then return
        
        // Make a new conversation if there is none
            // Set a new threshold
        // Otherwise continue conversion 
        
        // Set a new running cooldown
        
        // Have we reached our max messages?
            // Set a new cooldown
            // Save to file
            // Remove conversionation from memory
            
        // If either of our cooldowns are active exit out
        if (!IsMainCooldownOver() || !IsMainCooldownOver())
        {
            return;
        }

        // Create a new conversation if one doesn't exist
        if (m_ConversationAi == null)
        {
            m_ConversationAi = await m_AiProvider.CreateConversation();
            CreateNewChatLimit();

            GeneralChannel = args.Channel;
            
            //TODO: Announce your alive-ment here
            await GeneralChannel.SendMessageAsync("PING FROM THE AI SERVER :3");

            // We don't really do anything except create what we need for a conversation so we will loop back round once we get a new message
            return;
        }

        if (GeneralChannel == null)
        {
            m_Logger.LogWarning("General Channel couldn't be set in AI Service");
            return;
        }
        
        // If our chat cooldown has expired send a new message :3
        if (m_ChatCooldown < DateTime.Now)
        {
            // Send a new message
            await GeneralChannel.SendMessageAsync("HEY FROM THE BOT :3");
            
            CreateNewChatCooldown();
            m_ChatMessagesLeft--;
        }

        if (m_ChatMessagesLeft <= 0)
        {
            await GeneralChannel.SendMessageAsync("GOOD BYE :)");
            m_ConversationAi = null;
            CreateNewMainCooldown();
        }
        
        return;
    }

    private void CreateNewChatCooldown()
    {
        // Randomly cooldown chat from 10-180 seconds
        m_ChatCooldown = DateTime.Now + TimeSpan.FromSeconds(m_Random.Next(10, 180));
        
        //TODO: REMOVE BELOW
        m_ChatCooldown = DateTime.Now + TimeSpan.FromSeconds(m_Random.Next(1, 10));
    }
    
    private void CreateNewMainCooldown()
    {
        // Randomly cooldown chat from 15-60 hours (18 hours - 2.5 days)
        m_MainCooldownThreshold = DateTime.Now + TimeSpan.FromHours(m_Random.Next(18, 60));
        
        //TODO: REMOVE BELOW
        m_MainCooldownThreshold = DateTime.Now + TimeSpan.FromSeconds(m_Random.Next(45, 60));
    }

    private void CreateNewChatLimit()
    {
        m_ChatMessagesLeft = m_Random.Next(MinChatMessages, MaxChatMessages);
    }
    
    private bool IsMainCooldownOver()
    {
        return true;
    }
    
    private bool IsChatCooldownOver()
    {
        return true;
    }
}