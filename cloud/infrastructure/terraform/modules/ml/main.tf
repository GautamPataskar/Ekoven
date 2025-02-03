# infrastructure/terraform/modules/ml/main.tf

resource "azurerm_machine_learning_workspace" "main" {
  name                    = "ekoven-${var.environment}-mlw"
  resource_group_name     = var.resource_group_name
  location               = var.location
  tags                   = var.tags
  
  application_insights_id = azurerm_application_insights.main.id
  key_vault_id           = azurerm_key_vault.main.id
  storage_account_id     = azurerm_storage_account.ml.id
  container_registry_id  = azurerm_container_registry.main.id

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_storage_account" "ml" {
  name                     = "ekoven${var.environment}ml"
  resource_group_name      = var.resource_group_name
  location                = var.location
  account_tier            = var.storage_account_tier
  account_replication_type = "LRS"
  tags                    = var.tags
}

resource "azurerm_cosmosdb_account" "main" {
  name                = "ekoven-${var.environment}-cosmos"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags
  
  offer_type = "Standard"
  kind       = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = var.cosmos_db_consistency_level
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }
}

resource "azurerm_key_vault" "main" {
  name                = "ekoven-${var.environment}-kv"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags
  
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  sku_name = "standard"
}

resource "azurerm_container_registry" "main" {
  name                = "ekoven${var.environment}cr"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags
  
  sku = "Standard"
  admin_enabled = true
}