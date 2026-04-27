using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ICurrentTripRepository
    {
        Task<int> UpdateCurrentTripAndLegentIcon(IDbTransaction transaction);
    }
}
