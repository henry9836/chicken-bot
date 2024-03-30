using ChickenBot.API.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ChickenBot.Core.SubServices
{
    public class ProviderInitService : IHostedService
    {
        private readonly IUserFlagProvider m_FlagProvider;

        public ProviderInitService(IUserFlagProvider flagProvider)
        {
            m_FlagProvider = flagProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await m_FlagProvider.Init(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
