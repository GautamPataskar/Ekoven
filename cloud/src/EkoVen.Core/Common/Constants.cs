// EkoVen.Core/Common/Constants.cs
namespace EkoVen.Core.Common
{
    public static class Constants
    {
        public static class BatteryLimits
        {
            public const double MinVoltage = 2.5;
            public const double MaxVoltage = 4.2;
            public const double MinCurrent = -100.0;
            public const double MaxCurrent = 100.0;
            public const double MinTemperature = -20.0;
            public const double MaxTemperature = 60.0;
            public const double MinSOC = 0.0;
            public const double MaxSOC = 100.0;
            public const double MinSOH = 0.0;
            public const double MaxSOH = 100.0;
        }

        public static class AlarmThresholds
        {
            public const double HighTemperatureWarning = 40.0;
            public const double HighTemperatureCritical = 45.0;
            public const double LowSOCWarning = 20.0;
            public const double LowSOCCritical = 10.0;
            public const double HighCurrentWarning = 80.0;
            public const double HighCurrentCritical = 90.0;
        }

        public static class OptimizationParams
        {
            public const double TargetEfficiency = 0.95;
            public const double MinCoolingPower = 0.0;
            public const double MaxCoolingPower = 1000.0;
            public const double OptimalTemperature = 25.0;
            public const int OptimizationInterval = 300; // seconds
        }

        public static class SystemStatus
        {
            public const string Normal = "NORMAL";
            public const string Warning = "WARNING";
            public const string Critical = "CRITICAL";
            public const string Error = "ERROR";
            public const string Maintenance = "MAINTENANCE";
        }

        public static class StorageContainers
        {
            public const string Telemetry = "Telemetry";
            public const string Analytics = "Analytics";
            public const string Alarms = "Alarms";
            public const string Configuration = "Configuration";
        }

        public static class CacheKeys
        {
            public const string DeviceConfig = "DeviceConfig_{0}";
            public const string DeviceState = "DeviceState_{0}";
            public const string OptimizationState = "OptimizationState_{0}";
            public const int DefaultCacheMinutes = 15;
        }
    }
}