
locals {
  name       = "memetrics-${lower(terraform.workspace)}"
  short_name = "mm${lower(replace(terraform.workspace, "-", ""))}"

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME                      = "dotnet"
    WEBSITES_ENABLE_APP_SERVICE_STORAGE           = "false"
    DOCKER_CUSTOM_IMAGE_NAME                      = var.docker_custom_image_name
    DOCKER_REGISTRY_SERVER_URL                    = var.docker_registry_server_url
    DOCKER_REGISTRY_SERVER_USERNAME               = var.docker_registry_server_username
    DOCKER_REGISTRY_SERVER_PASSWORD               = var.docker_registry_server_password
    ASPNETCORE_ENVIRONMENT                        = var.aspnetcore_environment
    APPINSIGHTS_INSTRUMENTATIONKEY                = var.app_insights_instrumentation_key

    MeMetrics_Base_Url                            = var.memetrics_base_url
    MeMetrics_Api_Key                             = var.memetrics_api_key

    Gmail_Client_Id                               = var.gmail_client_id
    Gmail_Client_Secret                           = var.gmail_client_secret
    Gmail_History_Refresh_Token                   = var.gmail_history_refresh_token
    Gmail_Main_Refresh_Token                      = var.gmail_main_refresh_token
    Gmail_Sms_Label                               = var.gmail_sms_label
    Gmail_Call_Log_Label                          = var.gmail_call_log_label
    Gmail_Personal_Capital_Label                  = var.gmail_personal_capital_label
    Gmail_LinkedIn_Label                          = var.gmail_linkedin_label
    Gmail_Sms_Email_Address                       = var.gmail_sms_email_address
    Gmail_Recruiter_Email_Address                 = var.gmail_recruiter_email_address

    Lyft_Refresh_Token                            = var.lyft_refresh_token
    Lyft_Basic_Auth                               = var.lyft_basic_auth
    Lyft_Cookie                                   = var.lyft_cookie

    Uber_Client_Id                                = var.uber_client_id
    Uber_Client_Secret                            = var.uber_client_secret
    Uber_Refresh_Token                            = var.uber_refresh_token
    Uber_Cookie                                   = var.uber_cookie
    Uber_User_Id                                  = var.uber_user_id

    GroupMe_Access_Token                          = var.groupme_access_token

    LinkedIn_Username                             = var.linkedin_username
    LinkedIn_Password                             = var.linkedin_password

    Personal_Capital_Username                     = var.personal_capital_username
    Personal_Capital_Password                     = var.personal_capital_password
    Personal_Capital_PMData                       = var.personal_capital_pmdata
    WEBSITES_ENABLE_APP_SERVICE_STORAGE           = false
    FUNCTIONS_EXTENSION_VERSION                   = "~3"
    WEBSITE_TIME_ZONE                             = "Eastern Standard Time"
  }
}

data "azurerm_subscription" "current" {}

resource "azurerm_storage_account" "updater_storage_account" {
  name                     = "${local.short_name}updater"
  resource_group_name      = var.infrastructure_resource_group_name
  location                 = var.infrastructure_resource_group_location
  account_tier             = var.storage_account_tier
  account_replication_type = "RAGRS"
  tags                     = var.tags
}

# Linux host custom docker containers cannot be used with a consumption plan
# https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-csharp#code-try-29
resource "azurerm_function_app" "memetrics_function_app" {
  name                      = "${var.infrastructure_resource_group_name}-updater-function-app"
  resource_group_name       = var.infrastructure_resource_group_name
  location                  = var.infrastructure_resource_group_location
  app_service_plan_id       = "${data.azurerm_subscription.current.id}/resourceGroups/${var.infrastructure_resource_group_name}/providers/Microsoft.Web/serverfarms/${var.infrastructure_app_service_plan_name}"
  storage_connection_string = azurerm_storage_account.updater_storage_account.primary_connection_string
  version                   = "~3"
  https_only                = true
  app_settings              = local.app_settings

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on = true
    linux_fx_version  = "DOCKER|${var.docker_custom_image_name}"
  }

  tags = var.tags
}