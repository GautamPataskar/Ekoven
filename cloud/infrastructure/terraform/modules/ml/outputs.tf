# infrastructure/terraform/modules/ml/outputs.tf

output "workspace_name" {
  value = azurerm_machine_learning_workspace.main.name
}

output "workspace_id" {
  value = azurerm_machine_learning_workspace.main.id
}

output "cosmos_db_endpoint" {
  value = azurerm_cosmosdb_account.main.endpoint
}

output "cosmos_db_account_name" {
  value = azurerm_cosmosdb_account.main.name
}

output "storage_account_id" {
  value = azurerm_storage_account.ml.id
}

output "key_vault_id" {
  value = azurerm_key_vault.main.id
}