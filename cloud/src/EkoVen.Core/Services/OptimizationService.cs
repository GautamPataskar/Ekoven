// EkoVen.Core/Services/OptimizationService.cs
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using EkoVen.Core.Models;
using EkoVen.Core.Common;

namespace EkoVen.Core.Services
{
    public class OptimizationService
    {
        private readonly ILogger<OptimizationService> _logger;
        private readonly Dictionary<string, OptimizationState> _optimizationStates;
        private readonly object _stateLock = new object();

        public OptimizationService(ILogger<OptimizationService> logger)
        {
            _logger = logger;
            _optimizationStates = new Dictionary<string, OptimizationState>();
        }

        public async Task<OptimizationResult> OptimizeOperationAsync(BmsData data)
        {
            try
            {
                Helpers.Logging.LogOperationStart(_logger, "OptimizeOperation", data.DeviceId);
                var startTime = DateTime.UtcNow;

                // Get or create optimization state
                var state = GetOptimizationState(data.DeviceId);

                // Update state with new data
                UpdateOptimizationState(state, data);

                // Calculate optimal parameters
                var result = CalculateOptimalParameters(data, state);

                // Apply optimization
                await ApplyOptimizationAsync(data, result);

                var duration = DateTime.UtcNow - startTime;
                Helpers.Logging.LogOperationComplete(_logger, "OptimizeOperation", 
                    data.DeviceId, duration);

                return result;
            }
            catch (Exception ex)
            {
                Helpers.Logging.LogOperationError(_logger, ex, "OptimizeOperation", 
                    data.DeviceId);
                throw;
            }
        }

        private OptimizationState GetOptimizationState(string deviceId)
        {
            lock (_stateLock)
            {
                if (!_optimizationStates.ContainsKey(deviceId))
                {
                    _optimizationStates[deviceId] = new OptimizationState
                    {
                        DeviceId = deviceId,
                        LastUpdate = DateTime.UtcNow,
                        TemperatureHistory = new List<double>(),
                        EfficiencyHistory = new List<double>(),
                        CoolingHistory = new List<double>()
                    };
                }
                return _optimizationStates[deviceId];
            }
        }

        private void UpdateOptimizationState(OptimizationState state, BmsData data)
        {
            state.LastUpdate = DateTime.UtcNow;
            
            // Update temperature history
            state.TemperatureHistory.Add(data.Measurements.Temperature);
            if (state.TemperatureHistory.Count > 100)
                state.TemperatureHistory.RemoveAt(0);

            // Update efficiency history
            var efficiency = CalculateCurrentEfficiency(data);
            state.EfficiencyHistory.Add(efficiency);
            if (state.EfficiencyHistory.Count > 100)
                state.EfficiencyHistory.RemoveAt(0);

            // Update cooling history
            state.CoolingHistory.Add(data.Measurements.CoolingPower);
            if (state.CoolingHistory.Count > 100)
                state.CoolingHistory.RemoveAt(0);
        }

        private OptimizationResult CalculateOptimalParameters(BmsData data, OptimizationState state)
        {
            var result = new OptimizationResult
            {
                Timestamp = DateTime.UtcNow,
                DeviceId = data.DeviceId
            };

            // Calculate optimal cooling power
            result.OptimalCoolingPower = CalculateOptimalCoolingPower(data, state);

            // Calculate optimal charging parameters
            (result.OptimalChargingCurrent, result.OptimalChargingVoltage) = 
                CalculateOptimalCharging(data, state);

            // Calculate efficiency impact
            result.ExpectedEfficiencyGain = CalculateExpectedEfficiencyGain(data, result);

            // Set optimization flags
            result.RequiresCooling = data.Measurements.Temperature > 
                data.Configuration.CoolingThreshold;
            result.RequiresChargeAdjustment = Math.Abs(result.OptimalChargingCurrent - 
                data.Measurements.Current) > 1.0;

            return result;
        }

        private async Task ApplyOptimizationAsync(BmsData data, OptimizationResult optimization)
        {
            // Apply cooling optimization
            if (optimization.RequiresCooling)
            {
                data.Measurements.CoolingPower = optimization.OptimalCoolingPower;
            }

            // Apply charging optimization
            if (optimization.RequiresChargeAdjustment)
            {
                // Implement charging adjustment logic
                await AdjustChargingParametersAsync(data, optimization);
            }

            // Log optimization results
            _logger.LogInformation(
                "Optimization applied for device {DeviceId}: Cooling={Cooling:F2}, " +
                "Current={Current:F2}, Voltage={Voltage:F2}",
                data.DeviceId,
                optimization.OptimalCoolingPower,
                optimization.OptimalChargingCurrent,
                optimization.OptimalChargingVoltage);
        }

        private double CalculateOptimalCoolingPower(BmsData data, OptimizationState state)
        {
            // Calculate temperature trend
            var tempTrend = CalculateTemperatureTrend(state.TemperatureHistory);

            // Calculate base cooling power
            var baseCooling = Math.Max(0, (data.Measurements.Temperature - 
                Constants.OptimizationParams.OptimalTemperature) * 10);

            // Adjust based on trend
            var adjustedCooling = baseCooling * (1 + tempTrend);

            // Limit to configured range
            return Math.Min(Math.Max(adjustedCooling, 
                Constants.OptimizationParams.MinCoolingPower),
                Constants.OptimizationParams.MaxCoolingPower);
        }

        private (double current, double voltage) CalculateOptimalCharging(
            BmsData data, OptimizationState state)
        {
            double optimalCurrent = data.Measurements.Current;
            double optimalVoltage = data.Measurements.Voltage;

            // Adjust based on temperature
            if (data.Measurements.Temperature > Constants.AlarmThresholds.HighTemperatureWarning)
            {
                optimalCurrent *= 0.8; // Reduce current by 20%
            }

            // Adjust based on SOC
            if (data.State.StateOfCharge > 80)
            {
                optimalCurrent *= 0.7; // Reduce current by 30%
            }

            // Ensure within limits
            optimalCurrent = Math.Min(Math.Max(optimalCurrent, 
                -data.Configuration.MaxCurrent), 
                data.Configuration.MaxCurrent);
            
            optimalVoltage = Math.Min(Math.Max(optimalVoltage, 
                data.Configuration.MinVoltage), 
                data.Configuration.MaxVoltage);

            return (optimalCurrent, optimalVoltage);
        }

        private double CalculateCurrentEfficiency(BmsData data)
        {
            var power = Math.Abs(data.Measurements.Voltage * data.Measurements.Current);
            var coolingPower = data.Measurements.CoolingPower;
            
            return power > 0 ? (power - coolingPower) / power : 0;
        }

        private double CalculateTemperatureTrend(List<double> temperatures)
        {
            if (temperatures.Count < 2)
                return 0;

            var recent = temperatures.GetRange(
                Math.Max(0, temperatures.Count - 10), 
                Math.Min(10, temperatures.Count));
            
            var trend = 0.0;
            for (int i = 1; i < recent.Count; i++)
            {
                trend += recent[i] - recent[i - 1];
            }

            return trend / (recent.Count - 1);
        }

        private double CalculateExpectedEfficiencyGain(BmsData data, OptimizationResult optimization)
        {
            var currentEfficiency = CalculateCurrentEfficiency(data);
            var optimizedPower = Math.Abs(optimization.OptimalChargingVoltage * 
                optimization.OptimalChargingCurrent);
            var optimizedEfficiency = optimizedPower > 0 ? 
                (optimizedPower - optimization.OptimalCoolingPower) / optimizedPower : 0;

            return optimizedEfficiency - currentEfficiency;
        }

        private async Task AdjustChargingParametersAsync(BmsData data, OptimizationResult optimization)
        {
            // charging adjustment logic
            // This would typically interface with the charging hardware
            await Task.CompletedTask;
        }
    }


    public class OptimizationState
    {
        public string DeviceId { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<double> TemperatureHistory { get; set; }
        public List<double> EfficiencyHistory { get; set; }
        public List<double> CoolingHistory { get; set; }
        public double LastOptimalCoolingPower { get; set; }
        public double LastOptimalCurrent { get; set; }
        public double LastOptimalVoltage { get; set; }
        public int OptimizationCount { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; }

        public OptimizationState()
        {
            TemperatureHistory = new List<double>();
            EfficiencyHistory = new List<double>();
            CoolingHistory = new List<double>();
            PerformanceMetrics = new Dictionary<string, double>();
            LastUpdate = DateTime.UtcNow;
            OptimizationCount = 0;
        }
    }

    public class OptimizationResult
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public double OptimalCoolingPower { get; set; }
        public double OptimalChargingCurrent { get; set; }
        public double OptimalChargingVoltage { get; set; }
        public double ExpectedEfficiencyGain { get; set; }
        public bool RequiresCooling { get; set; }
        public bool RequiresChargeAdjustment { get; set; }
        public Dictionary<string, double> Metrics { get; set; }

        public OptimizationResult()
        {
            Metrics = new Dictionary<string, double>();
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"Optimization Result for {DeviceId}:\n" +
                   $"Timestamp: {Timestamp}\n" +
                   $"Cooling Power: {OptimalCoolingPower:F2}\n" +
                   $"Charging Current: {OptimalChargingCurrent:F2}\n" +
                   $"Charging Voltage: {OptimalChargingVoltage:F2}\n" +
                   $"Efficiency Gain: {ExpectedEfficiencyGain:P2}\n" +
                   $"Requires Cooling: {RequiresCooling}\n" +
                   $"Requires Charge Adjustment: {RequiresChargeAdjustment}";
        }
    }

    public class OptimizationException : Exception
    {
        public string DeviceId { get; }
        public string OptimizationType { get; }

        public OptimizationException(string deviceId, string optimizationType, string message)
            : base(message)
        {
            DeviceId = deviceId;
            OptimizationType = optimizationType;
        }

        public OptimizationException(string deviceId, string optimizationType, string message, Exception inner)
            : base(message, inner)
        {
            DeviceId = deviceId;
            OptimizationType = optimizationType;
        }
    }
}