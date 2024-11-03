using System.Data;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Attributes
{
    public class RequireBotSpamAttribute : CheckBaseAttribute
    {
        private static int m_Count = 0;

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var configuration = ctx.Services.GetRequiredService<IConfiguration>();

            var botSpamChannel = configuration.GetSection("Channels").GetValue("bot-spam", 0ul);

            var result = botSpamChannel == 0 || ctx.Channel.Id == botSpamChannel;

            if (!result && !help)
            {
                await ctx.TryReactAsync("toothless_no");
                m_Count++;
                m_Count %= 3;
                if (m_Count == 4)
                {
                    await ctx.RespondAsync($"-# Hint: Head over to <#{botSpamChannel}>, you cannot use that command here");
                }
            }

            return result;
        }
    }
}
