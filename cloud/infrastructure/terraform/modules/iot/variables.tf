# infrastructure/terraform/modules/iot/variables.tf

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region for resources"
  type        = string
}

variable "environment" {
  description = "Environment name"
  type        = string
}

variable "tags" {
  description = "Resource tags"
  type        = map(string)
}

variable "iot_hub_sku" {
  description = "IoT Hub SKU"
  type        = string
}

variable "iot_hub_capacity" {
  description = "IoT Hub capacity units"
  type        = number
}

variable "subnet_id" {
  description = "Subnet ID for private endpoints"
  type        = string
}

variable "key_vault_id" {
  description = "Key Vault ID for storing secrets"
  type        = string
}