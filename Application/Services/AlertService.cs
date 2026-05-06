using Dapper;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Npgsql;

public class AlertService : IAlertService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITripAlertRepository _tripAlertRepository;

    public AlertService(IDbConnectionFactory dbConnectionFactory, ITripAlertRepository tripAlertRepository)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _tripAlertRepository = tripAlertRepository;
    }

    public async Task ProcessAsync(TripAlertDTO alert)
    {

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            await _tripAlertRepository.InsertAlertAsync(transaction, alert);

            var pointType = alert.PointType?.ToUpper();
            var geoStatus = alert.GeoStatus?.ToUpper();

            if (pointType == "START")
                await _tripAlertRepository.HandleStartAsync(transaction, alert, geoStatus);

            else if (pointType == "VIA")
                await _tripAlertRepository.HandleViaAsync(transaction, alert, geoStatus);

            else if (pointType == "END")
                await _tripAlertRepository.HandleEndAsync(transaction, alert, geoStatus);

            transaction.Commit();
        }
        catch(Exception ex)
        {
            transaction.Rollback();
            throw ex;
        }
    }
}