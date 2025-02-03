# infrastructure/terraform/main.tf

terraform {
  required_version = ">= 1.0.0"
  
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.0"
    }
  }
  
  backend "azurerm" {
    resource_group_name  = "terraform-state-rg"
    storage_account_name = "ekoventerraformstate"
    container_name      = "tfstate"
    key                 = "prod.terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true
    }
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "ekoven-${var.environment}-rg"
  location = var.location
  tags     = var.tags
}

# IoT Hub Module
module "iot" {
  source = "./modules/iot"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  tags               = var.tags
  
  iot_hub_sku        = var.iot_hub_sku
  iot_hub_capacity   = var.iot_hub_capacity
  cosmos_db_account  = module.ml.cosmos_db_account_name
}

# ML Module
module "ml" {
  source = "./modules/ml"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  tags               = var.tags
  
  storage_account_tier = var.storage_account_tier
  cosmos_db_consistency_level = var.cosmos_db_consistency_level
}

# Monitoring Module
module "monitoring" {
  source = "./modules/monitoring"
  
  resource_group_name = azurerm_resource_group.main.name
  location           = var.location
  environment        = var.environment
  tags               = var.tags
  
  iot_hub_name       = module.iot.iot_hub_name
  ml_workspace_name  = module.ml.workspace_name
}