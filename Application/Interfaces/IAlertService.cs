using FleetBharat.TMSService.Application.DTOs;

namespace FleetBharat.TMSService.Application.Interfaces
{
    public interface IAlertService
    {
        Task ProcessAsync(TripAlertDTO alert);
    }
}
