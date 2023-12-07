using System.Text;
using ChickenBot.API.Interfaces;
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
    private readonly IConfigEditor m_ConfigurationEditor;
    private readonly ILogger<ChatAiService> m_Logger;
    private readonly DiscordClient m_Client;
    private readonly IConversationAIProvider m_AiProvider;
    private readonly Random m_Random = new Random();
    private ChatAISharedInfoService m_ChatInfoService;
    
    public ChatAiService(DiscordClient client, IConfiguration configuration, ILogger<ChatAiService> logger, IConversationAIProvider aiProvider, IConfigEditor configurationEditor, ChatAISharedInfoService infoService)
    {
        // Store the servives we have injected so we can use them in StartAsync
        m_Client = client;
        m_Logger = logger;
        m_Configuration = configuration;
        m_AiProvider = aiProvider;
        m_ConfigurationEditor = configurationEditor;
        m_ChatInfoService = infoService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        m_Client.MessageCreated += ClientOnMessageCreated;

        // Get the main cooldown, if can't find make a new one
        m_ChatInfoService.m_MainCooldownThreshold = m_Configuration.GetSection("ChatAI").GetValue("AICooldownStamp", DateTime.MinValue);
        if (m_ChatInfoService.m_MainCooldownThreshold == DateTime.MinValue)
        {
            m_Logger.LogWarning("Couldn't find a cooldown value in the config file, generating a new one...");
            m_ChatInfoService.UpdateMainCooldown();
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
        if (args.Author is not DiscordMember member)
        {
            return;   
        }

        // Check that our main cooldown isn't still active
        if (m_ChatInfoService.m_MainCooldownThreshold > DateTime.Now)
        {
            // If our conversation is still active then someone used the !shut command and we need to say goodbye
            if (m_ChatInfoService.m_ConversationAi != null)
            {
                _ = Task.Run(() => SendGoodbyeMessage());
                m_ChatInfoService.m_ConversationAi = null;
            }
            
            return;
        }
        
        // Is a bot talking (or us?)
        if (args.Author.IsBot)
        {
            return;
        }
        
        // If we are not in general
        if (args.Channel.Id != m_ChatInfoService.GeneralChannelId)
        {
            return;
        }
        
        // Get messages
        if (m_ChatInfoService.m_ConversationAi != null)
        {
            string message = args.Message.Content + $" - {member.Nickname ?? member.DisplayName}";
            await m_ChatInfoService.m_ConversationAi.PushChatMessage(member, message);
        }
            
        // If either of our cooldowns are active exit out
        if (!IsMainCooldownOver() || !IsChatCooldownOver())
        {
            return;
        }

        // Create a new conversation if one doesn't exist
        if (m_ChatInfoService.m_ConversationAi == null)
        {
            m_ChatInfoService.m_ConversationAi = await m_AiProvider.CreateConversation();
            CreateNewChatLimit();

            m_ChatInfoService.GeneralChannel = args.Channel;
            
            var awakeMessage = m_ChatInfoService.m_AwakeMessages[m_Random.Next(0, m_ChatInfoService.m_AwakeMessages.Length)];
            await m_ChatInfoService.GeneralChannel.SendMessageAsync(awakeMessage);

            // We don't really do anything except create what we need for a conversation so we will loop back round once we get a new message
            return;
        }

        if (m_ChatInfoService.GeneralChannel == null)
        {
            m_Logger.LogWarning("General Channel couldn't be set in AI Service");
            return;
        }
        
        // If our chat cooldown has expired send a new message :3
        if (m_ChatInfoService.m_ChatCooldown < DateTime.Now)
        {
            // Generate new cooldowns and iterate countdown
            m_ChatInfoService.UpdateChatCooldown();
            m_ChatInfoService.m_ChatMessagesLeft--;
            
            // Run on threadpool so we don't hold up the bot waiting on openai
            _ = Task.Run(() => PokeAIBrain(m_ChatInfoService.m_ChatMessagesLeft <= 0));
        }

        if (m_ChatInfoService.m_ChatMessagesLeft <= 0)
        {
            m_ChatInfoService.UpdateMainCooldown();
        }
        
        return;
    }

    private async Task PokeAIBrain(bool shouldShutdownAfterResponse)
    {
        if (m_ChatInfoService.m_ConversationAi == null)
        {
            m_Logger.LogWarning("Conversation object was null when trying to send a response from ai");
            return;
        }

        if (m_ChatInfoService.GeneralChannel == null)
        {
            m_Logger.LogWarning("General Channel was null when trying to send a response from ai");
            return;
        }

        try
        {
            var response = await m_ChatInfoService.m_ConversationAi.GetResponseAsync();
            if (string.IsNullOrEmpty(response))
            {
                m_Logger.LogWarning("AI responded with null value!");
                return;
            }
            await m_ChatInfoService.GeneralChannel.SendMessageAsync(response);
        }
        catch (Exception e)
        {
            m_Logger.LogError(e, "Couldn't get a response from OpenAI:");
            throw;
        }

        if (shouldShutdownAfterResponse)
        {
            await SendGoodbyeMessage();
        }
    }

    private async Task SendGoodbyeMessage()
    {
        if (m_ChatInfoService.GeneralChannel == null)
        {
            return;
        }
        
        var goodbyeMessage = m_ChatInfoService.m_ShutdownMessages[m_Random.Next(0, m_ChatInfoService.m_ShutdownMessages.Length)];
        await m_ChatInfoService.GeneralChannel.SendMessageAsync(goodbyeMessage);
        m_ChatInfoService.m_ConversationAi = null;
    }

    private void CreateNewChatLimit()
    {
        m_ChatInfoService.m_ChatMessagesLeft = m_Random.Next(m_ChatInfoService.MinChatMessages, m_ChatInfoService.MaxChatMessages);
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