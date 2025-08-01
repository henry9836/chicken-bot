using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using ChickenBot.ChatAI.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.ChatAI;

[Singleton]
public class ChatAiService : IEventHandler<MessageCreatedEventArgs>
{
    private readonly IConfiguration m_Configuration;
    private readonly IConfigEditor m_ConfigurationEditor;
    private readonly ILogger<ChatAiService> m_Logger;
    private readonly DiscordClient m_Client;
    private readonly IConversationAIProvider m_AiProvider;
    private readonly Random m_Random = new Random();
    private bool bDebugMessageSentOnNull = false;
    private ChatAISharedInfoService m_ChatInfoService;

    private bool m_Initialized = false;

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

    public void Init()
    {
        // Get the main cooldown, if can't find make a new one
        m_ChatInfoService.m_MainCooldownThreshold = m_Configuration.GetSection("ChatAI").GetValue("AICooldownStamp", DateTime.MinValue);
        if (m_ChatInfoService.m_MainCooldownThreshold == DateTime.MinValue)
        {
            m_Logger.LogWarning("Couldn't find a cooldown value in the config file, generating a new one...");
            m_ChatInfoService.UpdateMainCooldown();
        }
    }

    public async Task DebugQuestion(DiscordClient sender, MessageCreatedEventArgs args)
    {
        if (m_ChatInfoService.m_ConversationAi == null)
        {
            bDebugMessageSentOnNull = true;
            m_ChatInfoService.m_ConversationAi = await m_AiProvider.CreateConversation();
            m_ChatInfoService.GeneralChannel = args.Channel;
        }

        DiscordMember member = await args.Guild.GetMemberAsync(args.Author.Id);
        _ = Task.Run(() => DebugPokeAIBrain(member, args.Message.Content));
    }

    private async Task PokeAIBrain(bool shouldShutdownAfterResponse)
    {
        if (m_ChatInfoService.m_ConversationAi == null)
        {
            m_Logger.LogWarning("Conversation object was null when trying to send a response from ai");
            return;
        }

        if (m_ChatInfoService.GeneralChannel is null)
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

    private async Task DebugPokeAIBrain(DiscordMember user, string message)
    {
        if (m_ChatInfoService.m_ConversationAi == null)
        {
            m_Logger.LogWarning("Conversation object was null when trying to send a response from ai");
            return;
        }

        if (m_ChatInfoService.GeneralChannel is null)
        {
            m_Logger.LogWarning("General Channel was null when trying to send a response from ai");
            return;
        }

        try
        {
            var response = await m_ChatInfoService.m_ConversationAi.GetManualResponseAsync(user, message);
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

        if (bDebugMessageSentOnNull)
        {
            m_ChatInfoService.m_ConversationAi = null;
        }
        bDebugMessageSentOnNull = false;
    }

    private async Task SendGoodbyeMessage()
    {
        if (m_ChatInfoService.GeneralChannel is null)
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
        return m_ChatInfoService.m_MainCooldownThreshold <= DateTime.Now;
    }

    private bool IsChatCooldownOver()
    {
        return m_ChatInfoService.m_ChatCooldown <= DateTime.Now;
    }

    private bool IsBotDev(ulong id)
    {
        var botDevIds = new ulong[] { 102606498860896256, 764761783965319189 };
        return botDevIds.Contains(id);
    }

    public async Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs args)
    {
        if (!m_Initialized)
        {
            m_Initialized = true;
            Init();
        }

        // TODO FIX THIS
        // Disable AI for now while looking for solution to long chat replies
        // Maybe we can check char count and then ask api to give another response with a blanket can you shorten this response please, /shrug
        if (!IsMainCooldownOver() || !IsChatCooldownOver())
        {
            m_ChatInfoService.UpdateMainCooldown();
            return;
        }

        // Filter out dms
        if (args.Author is not DiscordMember member)
        {
            return;
        }

        // Debug mode
        if (args.Message.Content.Contains("!ask"))
        {
            if (IsBotDev(args.Author.Id))
            {
                await DebugQuestion(sender, args);

                return;
            }
        }

        // Check that our main cooldown isn't still active
        if (m_ChatInfoService.m_MainCooldownThreshold > DateTime.Now)
        {
            // If our conversation is still active then someone used the !shut command and we need to say goodbye
            if (m_ChatInfoService.m_ConversationAi != null && !bDebugMessageSentOnNull)
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
            var name = string.IsNullOrWhiteSpace(member.Nickname) ? member.GlobalName : member.Nickname;

            var message = args.Message.Content + $" - {name}";

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

        if (m_ChatInfoService.GeneralChannel is null)
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
}