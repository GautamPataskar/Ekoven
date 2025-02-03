# infrastructure/terraform/modules/ml/variables.tf

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

variable "storage_account_tier" {
  description = "Storage account tier"
  type        = string
}

variable "cosmos_db_consistency_level" {
  description = "Cosmos DB consistency level"
  type        = string
}

variable "subnet_id" {
  description = "Subnet ID for private endpoints"
  type        = string
}

variable "key_vault_access_object_ids" {
  description = "Object IDs that need Key Vault access"
  type        = list(string)
  default     = []
}