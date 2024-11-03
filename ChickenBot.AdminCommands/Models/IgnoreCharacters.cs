namespace ChickenBot.AdminCommands.Models
{
    public class IgnoreCharacters
    {
        public int MaxSkip { get; }

        public IgnoreCharacters(int maxSkip = 0)
        {
            MaxSkip = maxSkip;
        }

        public virtual bool ShouldIgnore(char c)
        {
            if (char.IsPunctuation(c))
            {
                return true;
            }

            if (char.IsSeparator(c))
            {
                return true;
            }

            return false;
        }
    }
}
