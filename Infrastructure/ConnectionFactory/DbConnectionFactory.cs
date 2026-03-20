using Npgsql;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.ConnectionFactory
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<DbConnectionFactory> _logger;

        public DbConnectionFactory(IConfiguration configuration, ILogger<DbConnectionFactory> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("Default");
            if(string.IsNullOrWhiteSpace(_connectionString))
            {
                _logger.LogError("Database connection string is not configured.");
                throw new InvalidOperationException("Database connection string is not configured.");
            }

        }
        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
