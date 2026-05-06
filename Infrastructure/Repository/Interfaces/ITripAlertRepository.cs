using FleetBharat.TMSService.Application.DTOs;
using System.Data;

namespace FleetBharat.TMSService.Infrastructure.Repository.Interfaces
{
    public interface ITripAlertRepository
    {
        Task InsertAlertAsync(IDbTransaction transaction, TripAlertDTO alert);
        Task HandleStartAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus);
        Task HandleViaAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus);
        Task HandleEndAsync(IDbTransaction transaction, TripAlertDTO alert, string geoStatus);
    }
}