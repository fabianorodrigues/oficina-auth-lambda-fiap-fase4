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

