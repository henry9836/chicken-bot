using Newtonsoft.Json;

namespace ChickenBot.Fun.AutoReact
{
	public delegate bool ReactDiscriminator(PersistentReact instance, ReactProvider provider, bool activated);
	public class PersistentReact
	{
		public string Emoji { get; set; }

		public float ActivationChance { get; set; }

		public ulong User { get; set; }

		[JsonIgnore]
		public ReactDiscriminator? Discriminator { get; set; }

		public string Tag { get; set; } = string.Empty;

		public DateTime Created { get; set; } = DateTime.Now;


		public PersistentReact(string emoji, float activationChance, ulong user, ReactDiscriminator? discriminator)
		{
			Emoji = emoji;
			ActivationChance = activationChance;
			User = user;
			Discriminator = discriminator;
		}
	}
}
