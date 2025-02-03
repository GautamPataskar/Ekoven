// EkoVen.Functions/Analytics/Models/AnalyticsData.cs
using System;
using Newtonsoft.Json;

namespace EkoVen.Functions.Analytics.Models
{
    public class AnalyticsData
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("period")]
        public string Period { get; set; }  // "1H", "24H", "7D", etc.

        [JsonProperty("performance")]
        public PerformanceMetrics Performance { get; set; }

        [JsonProperty("health")]
        public HealthMetrics Health { get; set; }

        [JsonProperty("thermal")]
        public ThermalMetrics Thermal { get; set; }

        [JsonProperty("efficiency")]
        public EfficiencyMetrics Efficiency { get; set; }

        [JsonProperty("predictions")]
        public PredictionMetrics Predictions { get; set; }
    }

    public class PerformanceMetrics
    {
        [JsonProperty("averageVoltage")]
        public double AverageVoltage { get; set; }

        [JsonProperty("averageCurrent")]
        public double AverageCurrent { get; set; }

        [JsonProperty("peakPower")]
        public double PeakPower { get; set; }

        [JsonProperty("energyDelivered")]
        public double EnergyDelivered { get; set; }

        [JsonProperty("energyReceived")]
        public double EnergyReceived { get; set; }

        [JsonProperty("cycleEfficiency")]
        public double CycleEfficiency { get; set; }
    }

    public class HealthMetrics
    {
        [JsonProperty("stateOfHealth")]
        public double StateOfHealth { get; set; }

        [JsonProperty("capacityLoss")]
        public double CapacityLoss { get; set; }

        [JsonProperty("impedanceIncrease")]
        public double ImpedanceIncrease { get; set; }

        [JsonProperty("estimatedLifetime")]
        public int EstimatedLifetime { get; set; }
    }

    public class ThermalMetrics
    {
        [JsonProperty("averageTemperature")]
        public double AverageTemperature { get; set; }

        [JsonProperty("maxTemperature")]
        public double MaxTemperature { get; set; }

        [JsonProperty("temperatureVariation")]
        public double TemperatureVariation { get; set; }

        [JsonProperty("coolingEfficiency")]
        public double CoolingEfficiency { get; set; }

        [JsonProperty("hotspotCount")]
        public int HotspotCount { get; set; }
    }

    public class EfficiencyMetrics
    {
        [JsonProperty("energyEfficiency")]
        public double EnergyEfficiency { get; set; }

        [JsonProperty("coulombicEfficiency")]
        public double CoulombicEfficiency { get; set; }

        [JsonProperty("thermalEfficiency")]
        public double ThermalEfficiency { get; set; }

        [JsonProperty("overallEfficiency")]
        public double OverallEfficiency { get; set; }
    }

    public class PredictionMetrics
    {
        [JsonProperty("estimatedRemainingLife")]
        public int EstimatedRemainingLife { get; set; }

        [JsonProperty("predictedFailureDate")]
        public DateTime? PredictedFailureDate { get; set; }

        [JsonProperty("maintenanceRecommendation")]
        public string MaintenanceRecommendation { get; set; }

        [JsonProperty("confidenceLevel")]
        public double ConfidenceLevel { get; set; }
    }
}