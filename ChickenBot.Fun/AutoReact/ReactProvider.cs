using ChickenBot.API.Attributes;

namespace ChickenBot.Fun.AutoReact
{
	[Singleton]
	public class ReactProvider
	{
		public List<PersistentReact> PersistentReacts { get; private set; } = new List<PersistentReact>();

		public void CreateAuto(PersistentReact react)
		{
			PersistentReacts.Add(react);
			Task.Run(SaveAsync);
		}

		public async Task SaveAsync()
		{
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(PersistentReacts);
			await File.WriteAllTextAsync("PersistentReact.json", json);
		}

		public async Task LoadAsync()
		{
			if (!File.Exists("PersistentReact.json"))
			{
				return;
			}

			var json = await File.ReadAllTextAsync("PersistentReact.json");
			PersistentReacts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PersistentReact>>(json) ?? new List<PersistentReact>();
		}
	}
}
