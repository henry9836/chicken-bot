using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace ChickenBot.Core.Models
{
	/// <summary>
	/// Used to create a connection to the MySQL server, using the default connection string specified in the config file
	/// </summary>
	public class DatabaseContext
	{
		private readonly IConfiguration m_Configuration;

		public DatabaseContext(IConfiguration configuration)
		{
			m_Configuration = configuration;
		}

		/// <summary>
		/// Creates a new conenction to the database
		/// </summary>
		/// <remarks>
		/// Ensure you dispose the <seealso cref="MySqlConnection"/> after use
		/// </remarks>
		/// <returns>New and open MySQL Connection</returns>
		public async Task<MySqlConnection> GetConnectionAsync()
		{
			var connectionString = m_Configuration.GetConnectionString("default");

			var connection = new MySqlConnection(connectionString);

			await connection.OpenAsync();

			return connection;
		}
	}
}
