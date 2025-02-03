// EkoVen.Core/Services/BmsService.cs
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using EkoVen.Core.Models;
using EkoVen.Core.Common;
using Microsoft.Azure.Cosmos;
using System.Linq;

namespace EkoVen.Core.Services
{
    public class BmsService
    {
        private readonly ILogger<BmsService> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _telemetryContainer;
        private readonly Container _configContainer;
        private readonly OptimizationService _optimizationService;

        public BmsService(
            CosmosClient cosmosClient,
            OptimizationService optimizationService,
            ILogger<BmsService> logger)
        {
            _cosmosClient = cosmosClient;
            _optimizationService = optimizationService;
            _logger = logger;
            
            var database = _cosmosClient.GetDatabase("EkoVen");
            _telemetryContainer = database.GetContainer(Constants.StorageContainers.Telemetry);
            _configContainer = database.GetContainer(Constants.StorageContainers.Configuration);
        }

        public async Task<BmsData> ProcessBmsDataAsync(BmsData data)
        {
            try
            {
                Helpers.Logging.LogOperationStart(_logger, "ProcessBmsData", data.DeviceId);
                var startTime = DateTime.UtcNow;

                // Validate data
                await ValidateBmsDataAsync(data);

                // Enrich data
                await EnrichBmsDataAsync(data);

                // Process alarms
                await ProcessAlarmsAsync(data);

                // Apply optimization if enabled
                if (data.Configuration.OptimizationEnabled)
                {
                    await _optimizationService.OptimizeOperationAsync(data);
                }

                // Store data
                await StoreBmsDataAsync(data);

                var duration = DateTime.UtcNow - startTime;
                Helpers.Logging.LogOperationComplete(_logger, "ProcessBmsData", 
                    data.DeviceId, duration);

                return data;
            }
            catch (Exception ex)
            {
                Helpers.Logging.LogOperationError(_logger, ex, "ProcessBmsData", 
                    data.DeviceId);
                throw;
            }
        }

        public async Task<BmsConfiguration> GetConfigurationAsync(string deviceId)
        {
            try
            {
                var response = await _configContainer.ReadItemAsync<BmsConfiguration>(
                    deviceId,
                    new PartitionKey(deviceId)
                );
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return CreateDefaultConfiguration();
            }
        }

        public async Task<List<BmsData>> GetHistoricalDataAsync(
            string deviceId, 
            DateTime startTime,
            DateTime endTime)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.deviceId = @deviceId " +
                "AND c.timestamp >= @startTime " +
                "AND c.timestamp <= @endTime")
                .WithParameter("@deviceId", deviceId)
                .WithParameter("@startTime", startTime)
                .WithParameter("@endTime", endTime);

            var results = new List<BmsData>();
            var iterator = _telemetryContainer.GetItemQueryIterator<BmsData>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        private async Task ValidateBmsDataAsync(BmsData data)
        {
            if (!Helpers.Validation.IsValidDeviceId(data.DeviceId))
                throw new ArgumentException("Invalid device ID");

            if (!Helpers.Validation.IsValidTimestamp(data.Timestamp))
                throw new ArgumentException("Invalid timestamp");

            var config = await GetConfigurationAsync(data.DeviceId);

            // Validate measurements
            ValidateMeasurements(data.Measurements, config);

            // Validate state
            ValidateState(data.State);
        }

        private void ValidateMeasurements(BmsMeasurements measurements, BmsConfiguration config)
        {
            if (!Helpers.Validation.IsInRange(measurements.Voltage, 
                config.MinVoltage, config.MaxVoltage))
                throw new ArgumentException("Voltage out of range");

            if (!Helpers.Validation.IsInRange(measurements.Current, 
                -config.MaxCurrent, config.MaxCurrent))
                throw new ArgumentException("Current out of range");

            if (!Helpers.Validation.IsInRange(measurements.Temperature, 
                Constants.BatteryLimits.MinTemperature, config.MaxTemperature))
                throw new ArgumentException("Temperature out of range");
        }

        private void ValidateState(BmsState state)
        {
            if (!Helpers.Validation.IsInRange(state.StateOfCharge, 
                Constants.BatteryLimits.MinSOC, Constants.BatteryLimits.MaxSOC))
                throw new ArgumentException("SOC out of range");

            if (!Helpers.Validation.IsInRange(state.StateOfHealth, 
                Constants.BatteryLimits.MinSOH, Constants.BatteryLimits.MaxSOH))
                throw new ArgumentException("SOH out of range");
        }

        private async Task EnrichBmsDataAsync(BmsData data)
        {
            // Generate ID if not present
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = Guid.NewGuid().ToString();
            }

            // Calculate derived metrics
            data.State.Power = Helpers.Calculations.CalculatePower(
                data.Measurements.Voltage, 
                data.Measurements.Current);

            // Update status based on measurements and alarms
            data.Status = DetermineSystemStatus(data);

            // Add metadata if missing
            if (data.Metadata == null)
            {
                data.Metadata = await GetDeviceMetadataAsync(data.DeviceId);
            }
        }

        private async Task ProcessAlarmsAsync(BmsData data)
        {
            var alarms = new List<BmsAlarm>();

            // Check temperature
            if (data.Measurements.Temperature >= Constants.AlarmThresholds.HighTemperatureCritical)
            {
                alarms.Add(new BmsAlarm
                {
                    Type = "Temperature",
                    Severity = "Critical",
                    Message = "Critical temperature level reached",
                    Timestamp = DateTime.UtcNow,
                    Value = data.Measurements.Temperature,
                    Threshold = Constants.AlarmThresholds.HighTemperatureCritical
                });
            }
            else if (data.Measurements.Temperature >= Constants.AlarmThresholds.HighTemperatureWarning)
            {
                alarms.Add(new BmsAlarm
                {
                    Type = "Temperature",
                    Severity = "Warning",
                    Message = "High temperature warning",
                    Timestamp = DateTime.UtcNow,
                    Value = data.Measurements.Temperature,
                    Threshold = Constants.AlarmThresholds.HighTemperatureWarning
                });
            }

            // Check SOC
            if (data.State.StateOfCharge <= Constants.AlarmThresholds.LowSOCCritical)
            {
                alarms.Add(new BmsAlarm
                {
                    Type = "SOC",
                    Severity = "Critical",
                    Message = "Critical low state of charge",
                    Timestamp = DateTime.UtcNow,
                    Value = data.State.StateOfCharge,
                    Threshold = Constants.AlarmThresholds.LowSOCCritical
                });
            }

            data.Alarms = alarms;

            if (alarms.Any())
            {
                await NotifyAlarmsAsync(data.DeviceId, alarms);
            }
        }

        private string DetermineSystemStatus(BmsData data)
        {
            if (data.Alarms?.Any(a => a.Severity == "Critical") ?? false)
                return Constants.SystemStatus.Critical;

            if (data.Alarms?.Any(a => a.Severity == "Warning") ?? false)
                return Constants.SystemStatus.Warning;

            if (data.State.StateOfHealth < 80)
                return Constants.SystemStatus.Maintenance;

            return Constants.SystemStatus.Normal;
        }

        private async Task StoreBmsDataAsync(BmsData data)
        {
            await Helpers.ErrorHandling.RetryWithExponentialBackoff(async () =>
            {
                await _telemetryContainer.CreateItemAsync(data, 
                    new PartitionKey(data.DeviceId));
                return true;
            });
        }

        private async Task NotifyAlarmsAsync(string deviceId, List<BmsAlarm> alarms)
        {
            // Implement alarm notification logic
            foreach (var alarm in alarms)
            {
                _logger.LogWarning("Alarm for device {DeviceId}: {Message}", 
                    deviceId, alarm.Message);
            }
        }

        private BmsConfiguration CreateDefaultConfiguration()
        {
            return new BmsConfiguration
            {
                MaxVoltage = Constants.BatteryLimits.MaxVoltage,
                MinVoltage = Constants.BatteryLimits.MinVoltage,
                MaxCurrent = Constants.BatteryLimits.MaxCurrent,
                MaxTemperature = Constants.BatteryLimits.MaxTemperature,
                NominalCapacity = 100.0,
                CoolingThreshold = 35.0,
                OptimizationEnabled = true
            };
        }

        private async Task<BmsMetadata> GetDeviceMetadataAsync(string deviceId)
        {
            // Implement device metadata retrieval
            return new BmsMetadata
            {
                FirmwareVersion = "1.0.0",
                HardwareVersion = "1.0.0",
                Manufacturer = "EkoVen",
                Model = "BMS-1000",
                SerialNumber = deviceId,
                InstallationDate = DateTime.UtcNow
            };
        }
    }
}