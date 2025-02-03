# infrastructure/terraform/outputs.tf

output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "iot_hub_name" {
  value = module.iot.iot_hub_name
}

output "iot_hub_hostname" {
  value = module.iot.iot_hub_hostname
}

output "ml_workspace_name" {
  value = module.ml.workspace_name
}

output "cosmos_db_endpoint" {
  value = module.ml.cosmos_db_endpoint
}

output "monitoring_workspace_id" {
  value = module.monitoring.workspace_id
}