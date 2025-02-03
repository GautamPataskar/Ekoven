// EkoVen.ML/Predictors/ThermalPredictor.cs

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Distributed;
using EkoVen.ML.Models;
using EkoVen.Core.Models;
using EkoVen.Core.Common;

namespace EkoVen.ML.Predictors
{
    public class ThermalPredictor
    {
        private readonly ILogger<ThermalPredictor> _logger;
        private readonly MLContext _mlContext;
        private readonly IDistributedCache _cache;
        private readonly CosmosClient _cosmosClient;
        private readonly string _cosmosConnectionString;
        private ITransformer _temperatureModel;
        private ITransformer _hotspotModel;
        private readonly string _temperatureModelPath;
        private readonly string _hotspotModelPath;
        private readonly Dictionary<string, ModelMetrics> _modelMetrics;

        public ThermalPredictor(
            ILogger<ThermalPredictor> logger,
            IDistributedCache cache,
            CosmosClient cosmosClient,
            string cosmosConnectionString)
        {
            _logger = logger;
            _cache = cache;
            _cosmosClient = cosmosClient;
            _cosmosConnectionString = cosmosConnectionString;
            _mlContext = new MLContext(seed: 1);
            _temperatureModelPath = "Models/thermal_prediction_model.zip";
            _hotspotModelPath = "Models/hotspot_detection_model.zip";
            _modelMetrics = new Dictionary<string, ModelMetrics>();
            InitializeModels();
        }

        private void InitializeModels()
        {
            try
            {
                if (System.IO.File.Exists(_temperatureModelPath) && 
                    System.IO.File.Exists(_hotspotModelPath))
                {
                    _temperatureModel = _mlContext.Model.Load(_temperatureModelPath, out var tempSchema);
                    _hotspotModel = _mlContext.Model.Load(_hotspotModelPath, out var hotspotSchema);
                    _logger.LogInformation("Thermal prediction models loaded successfully");
                }
                else
                {
                    _logger.LogWarning("Model files not found. Training new models...");
                    TrainNewModels();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading thermal prediction models");
                throw;
            }
        }

        private async void TrainNewModels()
        {
            try
            {
                var trainingData = await LoadTrainingDataAsync();
                
                // Train temperature model
                _temperatureModel = await TrainTemperatureModelAsync(trainingData);
                var tempMetrics = EvaluateTemperatureModel(_temperatureModel, trainingData);
                LogModelMetrics("Temperature", tempMetrics);

                // Train hotspot model
                _hotspotModel = await TrainHotspotModelAsync(trainingData);
                var hotspotMetrics = EvaluateHotspotModel(_hotspotModel, trainingData);
                LogModelMetrics("Hotspot", hotspotMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training new models");
                throw;
            }
        }

        private async Task<IDataView> LoadTrainingDataAsync()
        {
            var container = _cosmosClient.GetContainer("BatteryAnalytics", "ThermalData");
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = 'thermal_training' AND c._ts > @cutoffTime")
                .WithParameter("@cutoffTime", 
                    DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds());

            var iterator = container.GetItemQueryIterator<ThermalInputData>(query);
            var trainingData = new List<ThermalInputData>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                trainingData.AddRange(response.ToList());
            }

            return _mlContext.Data.LoadFromEnumerable(trainingData);
        }

        private async Task<ITransformer> TrainTemperatureModelAsync(IDataView trainingData)
        {
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                new[] { "CurrentTemperature", "AmbientTemperature", "Current", 
                        "Voltage", "CoolingPower", "StateOfCharge", "Load" })
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.Regression.Trainers.FastForest(new FastForestRegressionTrainer.Options
                {
                    NumberOfTrees = 100,
                    NumberOfLeaves = 20,
                    MinimumExampleCountPerLeaf = 10,
                    LearningRate = 0.2,
                    Shrinkage = 0.1
                }));

            var model = await Task.Run(() => pipeline.Fit(trainingData));
            await SaveModelAsync(model, _temperatureModelPath);
            return model;
        }

        private async Task<ITransformer> TrainHotspotModelAsync(IDataView trainingData)
        {
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                new[] { "CurrentTemperature", "AmbientTemperature", "Current", 
                        "Voltage", "CoolingPower", "StateOfCharge", "Load", 
                        "MaxPredictedTemperature" })
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.FastForest(
                    new FastForestBinaryTrainer.Options
                    {
                        NumberOfTrees = 100,
                        NumberOfLeaves = 20,
                        MinimumExampleCountPerLeaf = 10,
                        LearningRate = 0.2
                    }));

            var model = await Task.Run(() => pipeline.Fit(trainingData));
            await SaveModelAsync(model, _hotspotModelPath);
            return model;
        }


        private async Task SaveModelAsync(ITransformer model, string modelPath)
        {
            try
            {
                await Task.Run(() => _mlContext.Model.Save(model, null, modelPath));
                _logger.LogInformation("Model saved successfully to {Path}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving model to {Path}", modelPath);
                throw;
            }
        }

        public async Task<ThermalPrediction> PredictThermalBehavior(
            BmsData bmsData, 
            TimeSpan predictionHorizon)
        {
            try
            {
                if (!IsValidInput(bmsData))
                {
                    throw new ArgumentException("Invalid input data");
                }

                var cachedPrediction = await GetCachedPrediction(bmsData.DeviceId);
                if (cachedPrediction != null && !IsPredictionStale(cachedPrediction))
                {
                    return cachedPrediction;
                }

                var inputData = PrepareThermalInputData(bmsData);
                var temperaturePredictions = PredictTemperatures(inputData, predictionHorizon);
                var hotspotProbability = PredictHotspotProbability(inputData, temperaturePredictions);
                var thermalRunawayRisk = CalculateThermalRunawayRisk(
                    temperaturePredictions, hotspotProbability);
                var coolingRequirements = CalculateCoolingRequirements(
                    temperaturePredictions, thermalRunawayRisk);
                var confidenceInterval = CalculateThermalConfidenceIntervals(
                    temperaturePredictions, bmsData);

                var result = new ThermalPrediction
                {
                    DeviceId = bmsData.DeviceId,
                    Timestamp = DateTime.UtcNow,
                    PredictedTemperatures = temperaturePredictions,
                    HotspotProbability = hotspotProbability,
                    ThermalRunawayRisk = thermalRunawayRisk,
                    CoolingRequirements = coolingRequirements,
                    ConfidenceInterval = confidenceInterval,
                    PredictionHorizon = predictionHorizon
                };

                await CachePrediction(bmsData.DeviceId, result);
                await LogPredictionAsync(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting thermal behavior for device {DeviceId}", 
                    bmsData.DeviceId);
                throw;
            }
        }

        private bool IsValidInput(BmsData bmsData)
        {
            if (bmsData == null || bmsData.Measurements == null)
                return false;

            return IsValidTemperature(bmsData.Measurements.Temperature) &&
                   IsValidTemperature(bmsData.Measurements.AmbientTemperature) &&
                   IsValidCurrent(bmsData.Measurements.Current) &&
                   IsValidVoltage(bmsData.Measurements.Voltage) &&
                   IsValidCoolingPower(bmsData.Measurements.CoolingPower);
        }

        private bool IsValidTemperature(double temperature)
        {
            return temperature >= Constants.BatteryLimits.MinTemperature &&
                   temperature <= Constants.BatteryLimits.MaxTemperature;
        }

        private bool IsValidCurrent(double current)
        {
            return current >= Constants.BatteryLimits.MinCurrent &&
                   current <= Constants.BatteryLimits.MaxCurrent;
        }

        private bool IsValidVoltage(double voltage)
        {
            return voltage >= Constants.BatteryLimits.MinVoltage &&
                   voltage <= Constants.BatteryLimits.MaxVoltage;
        }

        private bool IsValidCoolingPower(double coolingPower)
        {
            return coolingPower >= 0 && coolingPower <= Constants.BatteryLimits.MaxCoolingPower;
        }

        private Dictionary<string, double> PredictTemperatures(
            ThermalInputData input, 
            TimeSpan horizon)
        {
            var predictions = new Dictionary<string, double>();
            var predEngine = _mlContext.Model.CreatePredictionEngine<ThermalInputData, 
                ThermalPredictionOutput>(_temperatureModel);

            var timePoints = GenerateTimePoints(horizon);
            foreach (var timePoint in timePoints)
            {
                input.PredictionTimeMinutes = (int)timePoint.TotalMinutes;
                var prediction = predEngine.Predict(input);
                predictions.Add(timePoint.ToString(), prediction.PredictedTemperature);
            }

            return predictions;
        }

        private IEnumerable<TimeSpan> GenerateTimePoints(TimeSpan horizon)
        {
            var timePoints = new List<TimeSpan>();
            var interval = TimeSpan.FromMinutes(5); // 5-minute intervals
            var currentTime = TimeSpan.Zero;

            while (currentTime <= horizon)
            {
                timePoints.Add(currentTime);
                currentTime = currentTime.Add(interval);
            }

            return timePoints;
        }

        private double PredictHotspotProbability(
            ThermalInputData input,
            Dictionary<string, double> temperaturePredictions)
        {
            input.MaxPredictedTemperature = temperaturePredictions.Values.Max();
            
            var predEngine = _mlContext.Model.CreatePredictionEngine<ThermalInputData, 
                HotspotPredictionOutput>(_hotspotModel);
            
            return predEngine.Predict().HotspotProbability;
        }

        private double CalculateThermalRunawayRisk(
            Dictionary<string, double> temperatures,
            double hotspotProbability)
        {
            double maxTemp = temperatures.Values.Max();
            double tempRisk = CalculateTemperatureRisk(maxTemp);
            double rateOfChange = CalculateTemperatureRateOfChange(temperatures);
            double rateRisk = CalculateRateOfChangeRisk(rateOfChange);

            return (0.4 * tempRisk + 0.3 * hotspotProbability + 0.3 * rateRisk);
        }

        private double CalculateTemperatureRisk(double temperature)
        {
            const double criticalTemp = 55.0;
            const double warningTemp = 45.0;
            
            if (temperature >= criticalTemp)
                return 1.0;
            if (temperature <= warningTemp)
                return 0.0;
            
            return (temperature - warningTemp) / (criticalTemp - warningTemp);
        }

        private double CalculateTemperatureRateOfChange(Dictionary<string, double> temperatures)
        {
            var tempValues = temperatures.Values.ToList();
            if (tempValues.Count < 2)
                return 0;

            var changes = new List<double>();
            for (int i = 1; i < tempValues.Count; i++)
            {
                changes.Add(tempValues[i] - tempValues[i - 1]);
            }

            return changes.Max();
        }

        private double CalculateRateOfChangeRisk(double rateOfChange)
        {
            const double criticalRate = 2.0; // °C/min
            const double warningRate = 1.0; // °C/min
            
            if (rateOfChange >= criticalRate)
                return 1.0;
            if (rateOfChange <= warningRate)
                return 0.0;
            
            return (rateOfChange - warningRate) / (criticalRate - warningRate);
        }

        private Dictionary<string, double> CalculateCoolingRequirements(
            Dictionary<string, double> temperatures,
            double thermalRunawayRisk)
        {
            var requirements = new Dictionary<string, double>();
            foreach (var kvp in temperatures)
            {
                double baseRequirement = CalculateBaseCoolingPower(kvp.Value);
                double riskAdjustment = thermalRunawayRisk * 0.5; // Up to 50% additional cooling
                requirements.Add(kvp.Key, baseRequirement * (1 + riskAdjustment));
            }
            return requirements;
        }

        private double CalculateBaseCoolingPower(double temperature)
        {
            const double optimalTemp = 25.0;
            const double coolingEfficiency = 0.85;
            return Math.Max(0, (temperature - optimalTemp) * coolingEfficiency);
        }

        private async Task<ThermalPrediction> GetCachedPrediction(string deviceId)
        {
            var cacheKey = $"thermal_prediction_{deviceId}";
            var cachedValue = await _cache.GetAsync(cacheKey);
            
            if (cachedValue != null)
            {
                return System.Text.Json.JsonSerializer
                    .Deserialize<ThermalPrediction>(cachedValue);
            }
            
            return null;
        }

        private bool IsPredictionStale(ThermalPrediction prediction)
        {
            return (DateTime.UtcNow - prediction.Timestamp).TotalMinutes > 
                Constants.Validation.MaxPredictionAge;
        }

        private async Task CachePrediction(string deviceId, ThermalPrediction prediction)
        {
            var cacheKey = $"thermal_prediction_{deviceId}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };
            
            var serializedPrediction = System.Text.Json.JsonSerializer
                .SerializeToUtf8Bytes(prediction);
            
            await _cache.SetAsync(cacheKey, serializedPrediction, options);
        }

        private async Task LogPredictionAsync(ThermalPrediction prediction)
        {
            try
            {
                var container = _cosmosClient.GetContainer("BatteryAnalytics", "ThermalPredictions");
                await container.CreateItemAsync(prediction, 
                    new PartitionKey(prediction.DeviceId));

                _logger.LogInformation(
                    "Logged thermal prediction for device {DeviceId}", 
                    prediction.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error logging thermal prediction for device {DeviceId}", 
                    prediction.DeviceId);
            }
        }

        public class ThermalInputData
        {
            [LoadColumn(0)]
            public double CurrentTemperature { get; set; }

            [LoadColumn(1)]
            public double AmbientTemperature { get; set; }

            [LoadColumn(2)]
            public double Current { get; set; }

            [LoadColumn(3)]
            public double Voltage { get; set; }

            [LoadColumn(4)]
            public double CoolingPower { get; set; }

            [LoadColumn(5)]
            public double StateOfCharge { get; set; }

            [LoadColumn(6)]
            public double Load { get; set; }

            [LoadColumn(7)]
            public int PredictionTimeMinutes { get; set; }

            [LoadColumn(8)]
            public double MaxPredictedTemperature { get; set; }

            public bool Validate()
            {
                return IsValidTemperature(CurrentTemperature) &&
                       IsValidTemperature(AmbientTemperature) &&
                       Current >= -1000 && Current <= 1000 &&
                       Voltage > 0 && Voltage <= 1000 &&
                       CoolingPower >= 0 &&
                       StateOfCharge >= 0 && StateOfCharge <= 100 &&
                       Load >= 0 &&
                       PredictionTimeMinutes >= 0;
            }
        }

        public class ThermalPredictionOutput
        {
            [ColumnName("PredictedTemperature")]
            public float PredictedTemperature { get; set; }
        }

        public class HotspotPredictionOutput
        {
            [ColumnName("HotspotProbability")]
            public float HotspotProbability { get; set; }
        }
    }
}
