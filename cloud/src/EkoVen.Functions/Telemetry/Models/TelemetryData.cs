// EkoVen.Functions/Telemetry/Models/TelemetryData.cs
using System;
using Newtonsoft.Json;

namespace EkoVen.Functions.Telemetry.Models
{
    public class TelemetryData
    {
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("voltage")]
        public double Voltage { get; set; }

        [JsonProperty("current")]
        public double Current { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("soc")]
        public double StateOfCharge { get; set; }

        [JsonProperty("soh")]
        public double StateOfHealth { get; set; }

        [JsonProperty("coolingPower")]
        public double CoolingPower { get; set; }

        [JsonProperty("ambientTemperature")]
        public double AmbientTemperature { get; set; }

        [JsonProperty("load")]
        public double Load { get; set; }

        [JsonProperty("cycleCount")]
        public int CycleCount { get; set; }

        [JsonProperty("alarms")]
        public string[] Alarms { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("metadata")]
        public TelemetryMetadata Metadata { get; set; }
    }

    public class TelemetryMetadata
    {
        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("configVersion")]
        public string ConfigVersion { get; set; }

        [JsonProperty("batteryType")]
        public string BatteryType { get; set; }

        [JsonProperty("nominalCapacity")]
        public double NominalCapacity { get; set; }
    }
}