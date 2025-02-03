# infrastructure/terraform/modules/iot/outputs.tf

output "iot_hub_name" {
  value = azurerm_iothub.main.name
}

output "iot_hub_hostname" {
  value = azurerm_iothub.main.hostname
}

output "iot_hub_id" {
  value = azurerm_iothub.main.id
}

output "dps_id" {
  value = azurerm_iothub_dps.main.id
}

output "storage_account_id" {
  value = azurerm_storage_account.iot.id
}