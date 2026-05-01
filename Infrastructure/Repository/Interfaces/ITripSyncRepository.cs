using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ITripSyncRepository
    {
        Task<IEnumerable<dynamic>> GetCurrentUnsyncedTripsAsync();

        Task<int> MarkTripsAsSyncedAsync(
        IDbTransaction transaction,
        List<int> tripIds);
    }
}
