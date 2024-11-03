using ChickenBot.AdminCommands.Models.Data;
using ChickenBot.API.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChickenBot.AdminCommands.Models
{
    [Singleton]
    public class MessageAlertBroker
    {
        public IReadOnlyList<SerializedMessageAlert> Alerts => m_Alerts;

        private const string m_DataFile = "message_alerts.json";

        private List<SerializedMessageAlert> m_Alerts = new List<SerializedMessageAlert>();

        private readonly ILogger<MessageAlertBroker> m_Logger;

        public event Action<SerializedMessageAlert>? OnAlertCreated;
        public event Action<SerializedMessageAlert>? OnAlertDeleted;

        public MessageAlertBroker(ILogger<MessageAlertBroker> logger)
        {
            m_Logger = logger;
        }

        public async Task LoadAlerts()
        {

            if (File.Exists(m_DataFile))
            {
                var json = await File.ReadAllTextAsync(m_DataFile);

                var alt = JsonConvert.DeserializeObject<List<SerializedMessageAlert>>(json);

                if (alt == null)
                {
                    m_Logger.LogWarning("Alerts from file are null! Discarding contents and creating new list.");
                    alt = new List<SerializedMessageAlert>();
                }

                m_Alerts = alt;
                return;
            }

            m_Logger.LogInformation("Alerts file not found, creating new list.");
            m_Alerts = new List<SerializedMessageAlert>();
        }

        public int GetNextID()
        {
            if (m_Alerts.Count == 0)
            {
                return 0;
            }

            return m_Alerts.Max(x => x.ID) + 1;
        }

        public void CreateAlert(SerializedMessageAlert alert)
        {
            m_Alerts.Add(alert);

            OnAlertCreated?.Invoke(alert);

            Task.Run(SaveAsync);
        }

        public bool DeleteAlert(int ID)
        {
            var alert = m_Alerts.FirstOrDefault(x => x.ID == ID);

            if (alert == null)
            {
                return false;
            }

            m_Alerts.Remove(alert);

            Task.Run(SaveAsync);

            OnAlertDeleted?.Invoke(alert);

            return true;
        }

        public async Task SaveAsync()
        {
            var json = JsonConvert.SerializeObject(m_Alerts);

            await File.WriteAllTextAsync(m_DataFile, json);
        }
    }
}
