using MySqlConnector;

namespace APULMS.Models
{
    public class DatabaseHelper
    {
        private readonly IConfiguration _configuration;

        public DatabaseHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(
                _configuration.GetConnectionString("DefaultConnection")
            );
        }
    }
}
