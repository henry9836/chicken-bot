namespace ChickenBot.API.Interfaces
{
    public interface IUserFlagProvider
    {
        /// <summary>
        /// Checks if a flag is set on a user, regardless of it's value
        /// </summary>
        Task<bool> IsFlagSet(ulong userID, string flag);

        /// <summary>
        /// Gets the value of a user flag.
        /// </summary>
        /// <returns>
        /// The flag value. 
        /// <see langword="null"/> can either mean the value is null, or the flag isn't set.
        /// </returns>
        Task<string?> GetFlagValue(ulong userID, string flag);

        /// <summary>
        /// Sets the value of a user flag, creating or updating the entry as needed
        /// </summary>
        Task SetFlagValue(ulong userID, string flag, string? value);

        /// <summary>
        /// Clears/Deletes a user flag
        /// </summary>
        /// <returns></returns>
        Task ClearFlagValue(ulong userID, string flag);

        Task Init(CancellationToken token);
    }
}
