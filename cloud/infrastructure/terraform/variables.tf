# infrastructure/terraform/variables.tf

variable "environment" {
  description = "Environment name (e.g., prod, dev, staging)"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "westeurope"
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {
    Project     = "EkoVen"
    Environment = "Production"
    Terraform   = "true"
  }
}

variable "iot_hub_sku" {
  description = "IoT Hub SKU"
  type        = string
  default     = "S1"
}

variable "iot_hub_capacity" {
  description = "IoT Hub capacity units"
  type        = number
  default     = 1
}

variable "storage_account_tier" {
  description = "Storage account tier"
  type        = string
  default     = "Standard"
}

variable "cosmos_db_consistency_level" {
  description = "Cosmos DB consistency level"
  type        = string
  default     = "Session"
}