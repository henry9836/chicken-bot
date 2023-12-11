using System.Reflection;
using System.Text.RegularExpressions;

namespace ChickenBot.Music.TrackResolve.Models
{
    public struct RegisteredResolver
    {
        public MethodInfo Method { get; }

        public object Instance { get; }

        public Regex? ValidDomain { get; }

        public RegisteredResolver(MethodInfo method, object instance, Regex? validDomain)
        {
            Method = method;
            Instance = instance;
            ValidDomain = validDomain;
        }
    }
}
