# infrastructure/terraform/modules/monitoring/main.tf

resource "azurerm_log_analytics_workspace" "main" {
  name                = "ekoven-${var.environment}-law"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags
  
  sku               = "PerGB2018"
  retention_in_days = 30
}

resource "azurerm_application_insights" "main" {
  name                = "ekoven-${var.environment}-ai"
  resource_group_name = var.resource_group_name
  location           = var.location
  tags               = var.tags
  
  workspace_id = azurerm_log_analytics_workspace.main.id
  application_type = "web"
}

resource "azurerm_monitor_diagnostic_setting" "iot_hub" {
  name                       = "iot-hub-diagnostics"
  target_resource_id        = var.iot_hub_id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  log {
    category = "Connections"
    enabled  = true

    retention_policy {
      enabled = true
      days    = 30
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      enabled = true
      days    = 30
    }
  }
}

resource "azurerm_monitor_action_group" "critical" {
  name                = "ekoven-${var.environment}-critical-ag"
  resource_group_name = var.resource_group_name
  short_name         = "critical"

  email_receiver {
    name          = "admin"
    email_address = var.admin_email
  }
}

resource "azurerm_monitor_metric_alert" "iot_hub_latency" {
  name                = "iot-hub-latency-alert"
  resource_group_name = var.resource_group_name
  scopes             = [var.iot_hub_id]
  description        = "Alert when IoT Hub latency exceeds threshold"

  criteria {
    metric_namespace = "Microsoft.Devices/IotHubs"
    metric_name      = "d2c.telemetry.ingress.latency"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 5000
  }

  action {
    action_group_id = azurerm_monitor_action_group.critical.id
  }
}