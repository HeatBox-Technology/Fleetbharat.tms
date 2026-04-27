using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using System.Data;

namespace FleetBharat.TMSService.Application.Services
{
    public class CurrentTripService : ICurrentTripService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ICurrentTripRepository _currentTripRepository;
        private readonly ILogger<CurrentTripService> _logger;

        public CurrentTripService(
            IDbConnectionFactory dbConnectionFactory,
            ICurrentTripRepository currentTripRepository,
            ILogger<CurrentTripService> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _currentTripRepository = currentTripRepository;
            _logger = logger;
        }
        public async Task UpdateCurrentTripAndLegentIcon()
        {
            _logger.LogInformation("Starting current trip assignment...");

            using var connection = _dbConnectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
             
            try
            {
                var rows = await _currentTripRepository.UpdateCurrentTripAndLegentIcon(transaction);

                transaction.Commit();

                _logger.LogInformation("Current trips assigned. Rows updated: {Rows}", rows);
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _logger.LogError(ex, "Error while assigning current trips");

                throw;
            }
        }
    }
}
