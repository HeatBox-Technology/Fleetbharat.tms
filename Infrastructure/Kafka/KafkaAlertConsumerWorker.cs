using Confluent.Kafka;
using FleetBharat.TMSService.Application.DTOs;
using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Application.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class KafkaAlertConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaRealtimeOptions _options;

    public KafkaAlertConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaRealtimeOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);
        Console.WriteLine("🔥Kafka Consumer Worker Started");

        if (!_options.Enabled)
            return;

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            EnableAutoCommit = true,
            Debug="all"
        };

        // Offset config
        config.AutoOffsetReset =
            _options.AutoOffsetReset?.ToLower() == "earliest"
                ? AutoOffsetReset.Earliest
                : AutoOffsetReset.Latest;

        // Security config (optional)
        if (!string.IsNullOrEmpty(_options.SecurityProtocol))
        {
            config.SecurityProtocol =
                Enum.Parse<SecurityProtocol>(_options.SecurityProtocol, true);
        }

        if (!string.IsNullOrEmpty(_options.SaslMechanism))
        {
            config.SaslMechanism =
                Enum.Parse<SaslMechanism>(_options.SaslMechanism, true);
        }

        if (!string.IsNullOrEmpty(_options.Username))
        {
            config.SaslUsername = _options.Username;
        }

        if (!string.IsNullOrEmpty(_options.Password))
        {
            config.SaslPassword = _options.Password;
        }

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(_options.Topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(5));

                if (result?.Message?.Value == null)
                    continue;

                var alert = JsonSerializer.Deserialize<TripAlertDTO>(
                    result.Message.Value,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (alert == null)
                    continue;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                    await alertService.ProcessAsync(alert);
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Kafka error: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing error: {ex.Message}");
            }
        }
    }
}