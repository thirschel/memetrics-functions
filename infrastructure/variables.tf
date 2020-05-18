variable "infrastructure_resource_group_name" {
  description = "The name of the infrastructure resource group"
}

variable "infrastructure_resource_group_location" {
  description = "The location of the infrastructure resource group"
}

variable "infrastructure_app_service_plan_name" {
  description = "App Service Plan ID to use"
}

variable "storage_account_tier" {
  description = "Defines the Tier to use for this Storage Account. Valid options are Standard and Premium"
  default     = "Standard"
}

variable "tags" {
  description = "A mapping of tages to assign to the resource. Changing this forces a new resource to be created."
  default = {
    source  = "terraform"
    product = "MeMetrics"
  }
}

# Various secrets that get used between the updater and the application

variable "app_insights_instrumentation_key" {}

variable "docker_custom_image_name" {}
variable "docker_registry_server_url" {}
variable "docker_registry_server_username" {}
variable "docker_registry_server_password" {}
variable "aspnetcore_environment" {}

variable "memetrics_base_url" {}
variable "memetrics_api_key" {}

variable "gmail_client_id" {}
variable "gmail_client_secret" {}
variable "gmail_history_refresh_token" {}
variable "gmail_main_refresh_token" {}
variable "gmail_sms_label" {}
variable "gmail_call_log_label" {}
variable "gmail_personal_capital_label" {}
variable "gmail_linkedin_label" {}
variable "gmail_sms_email_address" {}
variable "gmail_recruiter_email_address" {}

variable "lyft_refresh_token" {}
variable "lyft_basic_auth" {}
variable "lyft_cookie" {}

variable "uber_client_id" {}
variable "uber_client_secret" {}
variable "uber_refresh_token" {}
variable "uber_cookie" {}
variable "uber_user_id" {}

variable "groupme_access_token" {}

variable "linkedin_username" {}
variable "linkedin_password" {}

variable "personal_capital_username" {}
variable "personal_capital_password" {}
variable "personal_capital_pmdata" {}