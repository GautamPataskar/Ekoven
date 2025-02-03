// tests/EkoVen.ML.Tests/PredictorTests.cs
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Azure.Cosmos;
using EkoVen.ML.Predictors;
using EkoVen.Core.Models;
using System.Threading.Tasks;

namespace EkoVen.ML.Tests
{
    public class PredictorTests
    {
        private readonly Mock<ILogger<BatteryLifePredictor>> _logger;
        private readonly Mock<IDistributedCache> _cache;
        private readonly Mock<CosmosClient> _cosmos;
        private readonly BatteryLifePredictor _predictor;

        public PredictorTests()
        {
            _logger = new Mock<ILogger<BatteryLifePredictor>>();
            _cache = new Mock<IDistributedCache>();
            _cosmos = new Mock<CosmosClient>();
            _predictor = new BatteryLifePredictor(
                _logger.Object,
                _cache.Object,
                _cosmos.Object,
                "TestConnectionString"
            );
        }

        [Fact]
        public async Task PredictRemainingLife_ValidInput_ReturnsPrediction()
        {
            // Arrange
            var bmsData = new BmsData
            {
                DeviceId = "test-device-001",
                Measurements = new BatteryMeasurements
                {
                    Voltage = 3.7,
                    Current = 2.0,
                    Temperature = 25,
                    Power = 7.4
                },
                State = new BatteryState
                {
                    Capacity = 95,
                    CycleCount = 100
                },
                Timestamp = System.DateTime.UtcNow
            };

            // Act
            var result = await _predictor.PredictRemainingLife(bmsData);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RemainingCycles > 0);
            Assert.True(result.DegradationRate > 0);
            Assert.InRange(result.CurrentCapacity, 0, 100);
        }

        [Fact]
        public async Task PredictRemainingLife_InvalidInput_ThrowsException()
        {
            // Arrange
            var invalidData = new BmsData
            {
                DeviceId = "test-device-002",
                Measurements = new BatteryMeasurements
                {
                    Voltage = -1, // Invalid voltage
                    Current = 1000000, // Invalid current
                    Temperature = 100 // Invalid temperature
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _predictor.PredictRemainingLife(invalidData)
            );
        }
    }
}