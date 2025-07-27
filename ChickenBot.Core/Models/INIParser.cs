namespace ChickenBot.Core.Models
{
    public class INIParser
    {
        public static Dictionary<string, Dictionary<string, string>> Parse(IEnumerable<string> lines)
        {
            var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

            var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            sections[string.Empty] = currentSection;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (trimmed.StartsWith('#'))
                {
                    continue;
                }

                if (trimmed.StartsWith('['))
                {
                    var key = trimmed.Trim('[', ']');
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    sections[key] = currentSection;
                    continue;
                }

                if (trimmed.Contains('='))
                {
                    var key = trimmed.Split('=')[0];
                    var value = trimmed.Substring(key.Length + 1);
                    currentSection[key.Trim()] = value.Trim();
                    continue;
                }

                currentSection[trimmed.Trim()] = string.Empty;
            }

            return sections;
        }
    }
}
