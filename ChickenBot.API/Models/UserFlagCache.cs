namespace ChickenBot.API.Models
{
    public class UserFlagCache
    {
        public DateTime LastAccessed { get; private set; } = DateTime.Now;

        private Dictionary<string, UserFlag> m_Flags = new(StringComparer.InvariantCultureIgnoreCase);

        public bool TryGetCached(string flag, out UserFlag value)
        {
            LastAccessed = DateTime.Now;
            lock (m_Flags)
            {
                return m_Flags.TryGetValue(flag, out value);
            }
        }

        public void SetFlag(string flag, string? value)
        {
            LastAccessed = DateTime.Now;
            lock (m_Flags)
            {
                m_Flags[flag] = new UserFlag(true, value);
            }
        }

        public void SetFlagCleared(string flag)
        {
            LastAccessed = DateTime.Now;
            lock (m_Flags)
            {
                m_Flags[flag] = new UserFlag(false, null);
            }
        }
    }

    public readonly struct UserFlag
    {
        public bool IsSet { get; }

        public string? Value { get; }

        public UserFlag(bool isSet, string? value)
        {
            IsSet = isSet;
            Value = value;
        }
    }
}
