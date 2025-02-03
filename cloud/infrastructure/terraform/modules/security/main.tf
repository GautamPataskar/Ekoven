# infrastructure/terraform/modules/security/main.tf

# Network Security
resource "azurerm_network_security_group" "main" {
  name                = "ekoven-${var.environment}-nsg"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags

  security_rule {
    name                       = "deny-direct-internet"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range         = "*"
    destination_port_range    = "*"
    source_address_prefix     = "Internet"
    destination_address_prefix = "VirtualNetwork"
  }

  security_rule {
    name                       = "allow-azure-services"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range         = "*"
    destination_port_range    = "*"
    source_address_prefix     = "AzureCloud"
    destination_address_prefix = "VirtualNetwork"
  }
}

# Private Endpoints
resource "azurerm_private_endpoint" "cosmos" {
  name                = "ekoven-${var.environment}-cosmos-pe"
  resource_group_name = var.resource_group_name
  location           = var.location
  subnet_id          = var.subnet_id

  private_service_connection {
    name                           = "cosmos-connection"
    private_connection_resource_id = var.cosmos_db_id
    is_manual_connection          = false
    subresource_names            = ["SQL"]
  }
}

resource "azurerm_private_endpoint" "storage" {
  name                = "ekoven-${var.environment}-storage-pe"
  resource_group_name = var.resource_group_name
  location           = var.location
  subnet_id          = var.subnet_id

  private_service_connection {
    name                           = "storage-connection"
    private_connection_resource_id = var.storage_account_id
    is_manual_connection          = false
    subresource_names            = ["blob"]
  }
}

# Key Vault Access Policies
resource "azurerm_key_vault_access_policy" "ml_workspace" {
  key_vault_id = var.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = var.ml_workspace_principal_id

  key_permissions = [
    "Get", "List", "Create", "Delete", "Update",
    "Import", "Backup", "Restore", "Recover"
  ]

  secret_permissions = [
    "Get", "List", "Set", "Delete", "Backup",
    "Restore", "Recover"
  ]

  certificate_permissions = [
    "Get", "List", "Create", "Delete", "Update",
    "Import", "Backup", "Restore", "Recover"
  ]
}

# Role Assignments
resource "azurerm_role_assignment" "storage_blob_contributor" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = var.ml_workspace_principal_id
}

# Azure AD Security Groups
resource "azuread_group" "ml_admins" {
  display_name     = "ML Workspace Administrators"
  security_enabled = true
}

resource "azuread_group" "iot_admins" {
  display_name     = "IoT Hub Administrators"
  security_enabled = true
}