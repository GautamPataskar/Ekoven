// EkoVen.Core/Common/Helpers.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EkoVen.Core.Models;
using Microsoft.Extensions.Logging;

namespace EkoVen.Core.Common
{
    public static class Helpers
    {
        public static class Validation
        {
            public static bool IsInRange(double value, double min, double max)
            {
                return value >= min && value <= max;
            }

            public static bool IsValidTimestamp(DateTime timestamp)
            {
                return timestamp != default && timestamp <= DateTime.UtcNow;
            }

            public static bool IsValidDeviceId(string deviceId)
            {
                return !string.IsNullOrEmpty(deviceId) && deviceId.Length <= 50;
            }
        }

        public static class Calculations
        {
            public static double CalculateEfficiency(double energyOut, double energyIn)
            {
                return energyIn > 0 ? energyOut / energyIn : 0;
            }

            public static double CalculatePower(double voltage, double current)
            {
                return voltage * current;
            }

            public static double CalculateEnergy(double power, TimeSpan duration)
            {
                return power * duration.TotalHours;
            }

            public static double CalculateTemperatureGradient(double temp1, double temp2, double distance)
            {
                return distance > 0 ? Math.Abs(temp1 - temp2) / distance : 0;
            }
        }

        public static class DataProcessing
        {
            public static double MovingAverage(IList<double> values, int windowSize)
            {
                if (values == null || values.Count == 0)
                    return 0;

                int start = Math.Max(0, values.Count - windowSize);
                double sum = 0;
                int count = 0;

                for (int i = start; i < values.Count; i++)
                {
                    sum += values[i];
                    count++;
                }

                return count > 0 ? sum / count : 0;
            }

            public static (double mean, double stdDev) CalculateStatistics(IList<double> values)
            {
                if (values == null || values.Count == 0)
                    return (0, 0);

                double sum = 0;
                double sumSquared = 0;
                int count = values.Count;

                foreach (var value in values)
                {
                    sum += value;
                    sumSquared += value * value;
                }

                double mean = sum / count;
                double variance = (sumSquared / count) - (mean * mean);
                double stdDev = Math.Sqrt(Math.Max(0, variance));

                return (mean, stdDev);
            }
        }

        public static class Logging
        {
            public static void LogOperationStart(ILogger logger, string operation, string deviceId)
            {
                logger.LogInformation("Starting {Operation} for device {DeviceId}", 
                    operation, deviceId);
            }

            public static void LogOperationComplete(ILogger logger, string operation, 
                string deviceId, TimeSpan duration)
            {
                logger.LogInformation("{Operation} completed for device {DeviceId} in {Duration}ms",
                    operation, deviceId, duration.TotalMilliseconds);
            }

            public static void LogOperationError(ILogger logger, Exception ex, string operation, 
                string deviceId)
            {
                logger.LogError(ex, "Error in {Operation} for device {DeviceId}: {Message}",
                    operation, deviceId, ex.Message);
            }
        }

        public static class Security
        {
            public static string HashDeviceId(string deviceId)
            {
                return deviceId != null ? 
                    Convert.ToBase64String(
                        System.Security.Cryptography.SHA256.Create()
                        .ComputeHash(System.Text.Encoding.UTF8.GetBytes(deviceId)))
                    : null;
            }

            public static bool ValidateApiKey(string apiKey)
            {
                // Implement API key validation logic
                return !string.IsNullOrEmpty(apiKey) && apiKey.Length == 32;
            }
        }

        public static class ErrorHandling
        {
            public static async Task<T> RetryWithExponentialBackoff<T>(
                Func<Task<T>> operation,
                int maxAttempts = 3,
                int initialDelayMs = 100)
            {
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        return await operation();
                    }
                    catch (Exception) when (attempt < maxAttempts)
                    {
                        await Task.Delay(initialDelayMs * (int)Math.Pow(2, attempt - 1));
                    }
                }
                return await operation();
            }
        }
    }
}