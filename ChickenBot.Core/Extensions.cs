namespace ChickenBot.Core
{
    public static class Extensions
    {
        public static string ToPlatformSpecificPath(this string path)
        {
            var localSeparator = Path.DirectorySeparatorChar;
            var otherSeparator = localSeparator == '/' ? '\\' : '/';

            return path.Replace(otherSeparator, localSeparator);
        }
    }
}
