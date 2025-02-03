# infrastructure/terraform/modules/monitoring/variables.tf

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

variable "iot_hub_id" {
  description = "IoT Hub resource ID"
  type        = string
}

variable "ml_workspace_id" {
  description = "ML Workspace resource ID"
  type        = string
}

variable "admin_email" {
  description = "Admin email for alerts"
  type        = string
}

variable "retention_days" {
  description = "Log retention in days"
  type        = number
  default     = 30
}