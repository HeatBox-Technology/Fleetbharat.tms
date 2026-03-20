using System.Data;

namespace FleetBharat.TMSService.Infrastructure.ConnectionFactory
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
