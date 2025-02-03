# EkoVen API Documentation

## API Endpoints Overview
This documentation covers the main endpoints available in the EkoVen demo for battery life prediction and optimization.

## Base URL
https://api.ekoven.demo/v1


## Authentication
All API requests require an API key to be included in the header:

Authorization: Bearer apikey_here


## Battery Life Prediction API

### Predict Remaining Life
Predicts the remaining life and health of a battery based on current measurements.

**Endpoint:** `POST /api/battery/predict`

**Request Body:**
json
{
"deviceId": "batt-001",
"measurements": {
"voltage": 3.7,
"current": 2.0,
"temperature": 25,
"power": 7.4
},
"state": {
"capacity": 95,
"cycleCount": 100
}
}

**Response:**
json
{
"remainingCycles": 2500,
"remainingDays": 450,
"currentCapacity": 95,
"degradationRate": 0.01,
"confidenceInterval": {
"lower": 2300,
"upper": 2700
}
}



## Battery Optimization API

### Get Optimal Parameters
Retrieves optimal charging and thermal parameters for a battery.

**Endpoint:** `POST /api/battery/optimize`

**Request Body:**
json
{
"deviceId": "batt-001",
"currentState": {
"soc": 75,
"temperature": 25,
"voltage": 3.7,
"current": 2.0
}
}

**Response:**
json
{
"optimalCurrent": 4.2,
"thermalControl": {
"targetTemp": 23,
"coolingPower": 0.8
},
"recommendations": [
"Maintain current charging rate",
"Monitor temperature"
]
}


## Code Examples

### Python Example
python
import requests
def predict_battery_life(device_id, voltage, current, temperature):
url = "https://api.ekoven.demo/v1/api/battery/predict"
headers = {
"Authorization": "Bearer YOUR_API_KEY",
"Content-Type": "application/json"
}
data = {
"deviceId": device_id,
"measurements": {
"voltage": voltage,
"current": current,
"temperature": temperature
}
}
response = requests.post(url, json=data, headers=headers)
return response.json()

Example usage
result = predict_battery_life("batt-001", 3.7, 2.0, 25)
print(f"Remaining cycles: {result['remainingCycles']}")

### C# eg
csharp
using System.Net.Http;
using System.Text.Json;
public async Task<BatteryPrediction> PredictBatteryLife(
string deviceId,
double voltage,
double current,
double temperature)
{
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY");
var request = new BatteryPredictionRequest
{
DeviceId = deviceId,
Measurements = new Measurements
{
Voltage = voltage,
Current = current,
Temperature = temperature
}
};
var response = await client.PostAsJsonAsync(
"https://api.ekoven.demo/v1/api/battery/predict",
request
);
return await response.Content.ReadFromJsonAsync<BatteryPrediction>();
}


