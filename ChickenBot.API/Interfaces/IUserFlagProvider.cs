namespace ChickenBot.API.Interfaces
{
    public interface IUserFlagProvider
    {
        Task<bool> IsFlagSet(ulong userID, string flag);

        Task<string?> GetFlagValue(ulong userID, string flag);

        Task SetFlagValue(ulong userID, string flag, string? value);

        Task ClearFlagValue(ulong userID, string flag);

        Task Init(CancellationToken token);
    }
}
