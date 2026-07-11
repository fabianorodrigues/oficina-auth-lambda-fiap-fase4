# Terraform Auth

Stack independente para `oficina-auth-cpf` e `oficina-authorizer`.

O backend usa a key `oficina/auth/terraform.tfstate`. O secret `/oficina/auth/jwt` e criado apenas como container; o valor `SigningKey` e sincronizado por workflow separado.

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

