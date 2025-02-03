# infrastructure/terraform/modules/iot/main.tf

resource "azurerm_iothub" "main" {
  name                = "ekoven-${var.environment}-iothub"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags

  sku {
    name     = var.iot_hub_sku
    capacity = var.iot_hub_capacity
  }

  endpoint {
    type                       = "AzureIotHub.StorageContainer"
    connection_string          = azurerm_storage_account.iot.primary_blob_connection_string
    name                      = "export"
    container_name            = azurerm_storage_container.iot_export.name
    encoding                  = "Avro"
    file_name_format          = "{iothub}/{partition}_{YYYY}_{MM}_{DD}_{HH}_{mm}"
    batch_frequency_in_seconds = 60
    max_chunk_size_in_bytes   = 314572800
  }

  route {
    name           = "export"
    source         = "DeviceMessages"
    condition      = "true"
    endpoint_names = ["export"]
    enabled        = true
  }

  enrichment {
    key            = "deviceType"
    value          = "$twin.tags.deviceType"
    endpoint_names = ["export"]
  }
}

resource "azurerm_storage_account" "iot" {
  name                     = "ekoven${var.environment}iot"
  resource_group_name      = var.resource_group_name
  location                = var.location
  account_tier            = "Standard"
  account_replication_type = "LRS"
  tags                    = var.tags
}

resource "azurerm_storage_container" "iot_export" {
  name                  = "iotexport"
  storage_account_name  = azurerm_storage_account.iot.name
  container_access_type = "private"
}

resource "azurerm_iothub_dps" "main" {
  name                = "ekoven-${var.environment}-dps"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags

  sku {
    name     = "S1"
    capacity = 1
  }

  linked_hub {
    connection_string = azurerm_iothub.main.connection_string
    location         = var.location
  }
}