// EkoVen.Functions/Telemetry/TelemetryValidator.cs
using System;
using System.Threading.Tasks;
using EkoVen.Functions.Telemetry.Models;
using Microsoft.Extensions.Logging;

namespace EkoVen.Functions.Telemetry
{
    public class TelemetryValidator
    {
        private readonly ILogger _logger;
        private readonly ValidationConfig _config;

        public TelemetryValidator(ILogger logger, ValidationConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<ValidationResult> ValidateAsync(TelemetryData telemetry)
        {
            try
            {
                if (telemetry == null)
                {
                    return new ValidationResult { IsValid = false, Message = "Telemetry data is null" };
                }

                // Validate required fields
                if (string.IsNullOrEmpty(telemetry.DeviceId))
                {
                    return new ValidationResult { IsValid = false, Message = "DeviceId is required" };
                }

                // Validate timestamp
                if (telemetry.Timestamp == default || telemetry.Timestamp > DateTime.UtcNow)
                {
                    return new ValidationResult { IsValid = false, Message = "Invalid timestamp" };
                }

                // Validate ranges
                if (!IsInRange(telemetry.Voltage, _config.MinVoltage, _config.MaxVoltage))
                {
                    return new ValidationResult { IsValid = false, Message = "Voltage out of range" };
                }

                if (!IsInRange(telemetry.Current, _config.MinCurrent, _config.MaxCurrent))
                {
                    return new ValidationResult { IsValid = false, Message = "Current out of range" };
                }

                if (!IsInRange(telemetry.Temperature, _config.MinTemperature, _config.MaxTemperature))
                {
                    return new ValidationResult { IsValid = false, Message = "Temperature out of range" };
                }

                if (!IsInRange(telemetry.StateOfCharge, 0, 100))
                {
                    return new ValidationResult { IsValid = false, Message = "SOC out of range" };
                }

                if (!IsInRange(telemetry.StateOfHealth, 0, 100))
                {
                    return new ValidationResult { IsValid = false, Message = "SOH out of range" };
                }

                // Validate metadata
                if (telemetry.Metadata != null)
                {
                    if (string.IsNullOrEmpty(telemetry.Metadata.FirmwareVersion))
                    {
                        _logger.LogWarning("Missing firmware version for device {DeviceId}", telemetry.DeviceId);
                    }

                    if (telemetry.Metadata.NominalCapacity <= 0)
                    {
                        return new ValidationResult { IsValid = false, Message = "Invalid nominal capacity" };
                    }
                }

                return new ValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error for device {DeviceId}", telemetry?.DeviceId);
                return new ValidationResult { IsValid = false, Message = "Validation error: " + ex.Message };
            }
        }

        private bool IsInRange(double value, double min, double max)
        {
            return value >= min && value <= max;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    public class ValidationConfig
    {
        public double MinVoltage { get; set; } = 2.5;
        public double MaxVoltage { get; set; } = 4.2;
        public double MinCurrent { get; set; } = -100;
        public double MaxCurrent { get; set; } = 100;
        public double MinTemperature { get; set; } = -20;
        public double MaxTemperature { get; set; } = 60;
    }
}