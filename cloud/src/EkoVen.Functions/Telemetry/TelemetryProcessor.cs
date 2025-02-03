using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventHubs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using EkoVen.Functions.Telemetry.Models;

namespace EkoVen.Functions.Telemetry
{
    public class TelemetryProcessor
    {
        private readonly TelemetryValidator _validator;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<TelemetryProcessor> _logger;
        private readonly Container _telemetryContainer;
        private readonly Container _analyticsContainer;

        public TelemetryProcessor(
            CosmosClient cosmosClient,
            TelemetryValidator validator,
            ILogger<TelemetryProcessor> logger)
        {
            _cosmosClient = cosmosClient;
            _validator = validator;
            _logger = logger;
            _telemetryContainer = _cosmosClient.GetContainer("EkoVen", "Telemetry");
            _analyticsContainer = _cosmosClient.GetContainer("EkoVen", "Analytics");
        }

        [FunctionName("ProcessTelemetry")]
        public async Task ProcessTelemetryAsync(
            [EventHubTrigger("telemetry", Connection = "EventHubConnection")] EventData[] events)
        {
            var tasks = new List<Task>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var telemetry = JsonConvert.DeserializeObject<TelemetryData>(messageBody);

                    // Validate telemetry
                    var validationResult = await _validator.ValidateAsync(telemetry);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Invalid telemetry: {Message} for device {DeviceId}", 
                            validationResult.Message, telemetry?.DeviceId);
                        continue;
                    }

                    // Process telemetry
                    tasks.Add(ProcessSingleTelemetryAsync(telemetry));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing telemetry event");
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessSingleTelemetryAsync(TelemetryData telemetry)
        {
            try
            {
                // Enrich telemetry
                await EnrichTelemetryAsync(telemetry);

                // Store telemetry
                await StoreTelemetryAsync(telemetry);

                // Process alarms
                await ProcessAlarmsAsync(telemetry);

                // Update analytics
                await UpdateAnalyticsAsync(telemetry);

                _logger.LogInformation("Successfully processed telemetry for device {DeviceId}", telemetry.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing single telemetry for device {DeviceId}", telemetry.DeviceId);
                throw;
            }
        }

        private async Task EnrichTelemetryAsync(TelemetryData telemetry)
        {
            // Add processing timestamp
            telemetry.Metadata = telemetry.Metadata ?? new TelemetryMetadata();
            telemetry.Metadata.ProcessedAt = DateTime.UtcNow;

            // Calculate derived metrics
            telemetry.Power = telemetry.Voltage * telemetry.Current;
            
            // Add system status if not present
            if (string.IsNullOrEmpty(telemetry.Status))
            {
                telemetry.Status = DetermineSystemStatus(telemetry);
            }
        }

        private async Task StoreTelemetryAsync(TelemetryData telemetry)
        {
            try
            {
                await _telemetryContainer.CreateItemAsync(telemetry, 
                    new PartitionKey(telemetry.DeviceId));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limited while storing telemetry. Retrying...");
                await Task.Delay(1000);
                await StoreTelemetryAsync(telemetry);
            }
        }

        private async Task ProcessAlarmsAsync(TelemetryData telemetry)
        {
            if (telemetry.Alarms != null && telemetry.Alarms.Length > 0)
            {
                foreach (var alarm in telemetry.Alarms)
                {
                    // Process each alarm
                    await ProcessAlarmAsync(telemetry.DeviceId, alarm);
                }
            }
        }

        private async Task ProcessAlarmAsync(string deviceId, string alarm)
        {
            // Implement alarm processing logic
            // This could include sending notifications, creating incidents, etc.
            _logger.LogWarning("Alarm detected for device {DeviceId}: {Alarm}", deviceId, alarm);
        }

        private string DetermineSystemStatus(TelemetryData telemetry)
        {
            if (telemetry.Alarms != null && telemetry.Alarms.Length > 0)
            {
                return "WARNING";
            }

            if (telemetry.Temperature > 40 || telemetry.StateOfCharge < 10)
            {
                return "CAUTION";
            }

            return "NORMAL";
        }

        private async Task UpdateAnalyticsAsync(TelemetryData telemetry)
        {
            try
            {
                // Update real-time analytics
                await UpdateRealTimeAnalyticsAsync(telemetry);

                // Update aggregated analytics
                await UpdateAggregatedAnalyticsAsync(telemetry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analytics for device {DeviceId}", telemetry.DeviceId);
                throw;
            }
        }

        private async Task UpdateRealTimeAnalyticsAsync(TelemetryData telemetry)
        {
            // Implement real-time analytics update logic
        }

        private async Task UpdateAggregatedAnalyticsAsync(TelemetryData telemetry)
        {
            // Implement aggregated analytics update logic
        }
    }
}
