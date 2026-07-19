# Terraform Auth

Stack independente para `oficina-auth-cpf` e `oficina-authorizer`.

O backend usa a key `oficina/auth/terraform.tfstate`. O bucket do state e resolvido pelo workflow como `oficina-terraform-state-<account-id>-<AWS_REGION>`, com fallback temporario para `TF_STATE_BUCKET` durante migracao. O secret `/oficina/auth/jwt` e criado como container pelo Terraform; o valor `SigningKey` e sincronizado pelo proprio `Auth Deploy`.

Entradas vindas de SSM:

- `/oficina/infra/vpc/id`
- `/oficina/infra/subnets/private/1`
- `/oficina/infra/subnets/private/2`
- `/oficina/infra/rds/security-group-id`

Saidas publicadas em SSM:

- `/oficina/auth/cpf/alias-arn`
- `/oficina/auth/cpf/function-name`
- `/oficina/auth/authorizer/alias-arn`
- `/oficina/auth/authorizer/function-name`

Nao executar `apply` localmente nesta etapa.

