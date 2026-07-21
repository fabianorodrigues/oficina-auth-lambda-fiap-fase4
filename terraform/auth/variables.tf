variable "aws_region" {
  type        = string
  description = "AWS region."
}

variable "auth_cpf_zip_path" {
  type        = string
  description = "Path to oficina-auth-cpf ZIP."
  default     = "../../artifacts/lambda/oficina-auth-cpf.zip"
}

variable "authorizer_zip_path" {
  type        = string
  description = "Path to oficina-authorizer ZIP."
  default     = "../../artifacts/lambda/oficina-authorizer.zip"
}

variable "log_retention_days" {
  type        = number
  description = "CloudWatch log retention in days."
  default     = 14
}

variable "auth_cpf_role_arn" {
  type        = string
  description = "Existing IAM role ARN for the CPF login Lambda. Leave empty only when Terraform is allowed to create IAM."
  default     = ""

  validation {
    condition     = trimspace(var.auth_cpf_role_arn) == "" || can(regex("^arn:[^:]+:iam::[0-9]{12}:role/.+$", trimspace(var.auth_cpf_role_arn)))
    error_message = "auth_cpf_role_arn must be empty or a valid IAM role ARN."
  }
}

variable "authorizer_role_arn" {
  type        = string
  description = "Existing IAM role ARN for the authorizer Lambda. Leave empty only when Terraform is allowed to create IAM."
  default     = ""

  validation {
    condition     = trimspace(var.authorizer_role_arn) == "" || can(regex("^arn:[^:]+:iam::[0-9]{12}:role/.+$", trimspace(var.authorizer_role_arn)))
    error_message = "authorizer_role_arn must be empty or a valid IAM role ARN."
  }
}

