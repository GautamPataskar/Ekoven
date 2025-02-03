using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EkoVen.Core.Models
{
    public class BmsData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("measurements")]
        public BmsMeasurements Measurements { get; set; }

        [JsonProperty("state")]
        public BmsState State { get; set; }

        [JsonProperty("configuration")]
        public BmsConfiguration Configuration { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("alarms")]
        public List<BmsAlarm> Alarms { get; set; }

        [JsonProperty("metadata")]
        public BmsMetadata Metadata { get; set; }
    }

    public class BmsMeasurements
    {
        [JsonProperty("voltage")]
        public double Voltage { get; set; }

        [JsonProperty("current")]
        public double Current { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("ambientTemperature")]
        public double AmbientTemperature { get; set; }

        [JsonProperty("humidity")]
        public double Humidity { get; set; }

        [JsonProperty("pressure")]
        public double Pressure { get; set; }

        [JsonProperty("coolingPower")]
        public double CoolingPower { get; set; }
    }

    public class BmsState
    {
        [JsonProperty("soc")]
        public double StateOfCharge { get; set; }

        [JsonProperty("soh")]
        public double StateOfHealth { get; set; }

        [JsonProperty("cycleCount")]
        public int CycleCount { get; set; }

        [JsonProperty("capacity")]
        public double Capacity { get; set; }

        [JsonProperty("impedance")]
        public double Impedance { get; set; }

        [JsonProperty("power")]
        public double Power { get; set; }

        [JsonProperty("energy")]
        public double Energy { get; set; }
    }

    public class BmsConfiguration
    {
        [JsonProperty("maxVoltage")]
        public double MaxVoltage { get; set; }

        [JsonProperty("minVoltage")]
        public double MinVoltage { get; set; }

        [JsonProperty("maxCurrent")]
        public double MaxCurrent { get; set; }

        [JsonProperty("maxTemperature")]
        public double MaxTemperature { get; set; }

        [JsonProperty("nominalCapacity")]
        public double NominalCapacity { get; set; }

        [JsonProperty("coolingThreshold")]
        public double CoolingThreshold { get; set; }

        [JsonProperty("optimizationEnabled")]
        public bool OptimizationEnabled { get; set; }
    }

    public class BmsAlarm
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("threshold")]
        public double Threshold { get; set; }
    }

    public class BmsMetadata
    {
        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("hardwareVersion")]
        public string HardwareVersion { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("installationDate")]
        public DateTime InstallationDate { get; set; }
    }
}
