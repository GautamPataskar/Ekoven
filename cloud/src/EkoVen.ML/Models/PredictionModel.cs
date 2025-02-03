// EkoVen.ML/Models/PredictionModel.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EkoVen.ML.Models
{
    public class PredictionModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("predictionType")]
        public string PredictionType { get; set; }

        [JsonProperty("predictions")]
        public Dictionary<string, double> Predictions { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("horizon")]
        public TimeSpan Horizon { get; set; }

        [JsonProperty("metadata")]
        public PredictionMetadata Metadata { get; set; }
    }

    public class PredictionMetadata
    {
        [JsonProperty("modelVersion")]
        public string ModelVersion { get; set; }

        [JsonProperty("algorithmType")]
        public string AlgorithmType { get; set; }

        [JsonProperty("trainingDate")]
        public DateTime TrainingDate { get; set; }

        [JsonProperty("features")]
        public List<string> Features { get; set; }

        [JsonProperty("metrics")]
        public Dictionary<string, double> Metrics { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class BatteryLifePrediction
    {
        [JsonProperty("remainingCycles")]
        public int RemainingCycles { get; set; }

        [JsonProperty("remainingDays")]
        public int RemainingDays { get; set; }

        [JsonProperty("endOfLifeDate")]
        public DateTime EndOfLifeDate { get; set; }

        [JsonProperty("currentCapacity")]
        public double CurrentCapacity { get; set; }

        [JsonProperty("projectedCapacity")]
        public Dictionary<string, double> ProjectedCapacity { get; set; }

        [JsonProperty("degradationRate")]
        public double DegradationRate { get; set; }

        [JsonProperty("confidenceInterval")]
        public ConfidenceInterval ConfidenceInterval { get; set; }
    }

    public class ThermalPrediction
    {
        [JsonProperty("predictedTemperatures")]
        public Dictionary<string, double> PredictedTemperatures { get; set; }

        [JsonProperty("hotspotProbability")]
        public double HotspotProbability { get; set; }

        [JsonProperty("thermalRunawayRisk")]
        public double ThermalRunawayRisk { get; set; }

        [JsonProperty("coolingRequirements")]
        public Dictionary<string, double> CoolingRequirements { get; set; }

        [JsonProperty("confidenceInterval")]
        public ConfidenceInterval ConfidenceInterval { get; set; }
    }

    public class ConfidenceInterval
    {
        [JsonProperty("lowerBound")]
        public double LowerBound { get; set; }

        [JsonProperty("upperBound")]
        public double UpperBound { get; set; }

        [JsonProperty("confidenceLevel")]
        public double ConfidenceLevel { get; set; }
    }
}