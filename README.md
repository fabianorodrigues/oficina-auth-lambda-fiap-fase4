# oficina-auth-lambda-fiap-fase4

## Responsabilidade

Implementa autenticacao por CPF/senha e validacao JWT para a solução Oficina,
por meio de duas Lambdas independentes. Provisionado após
[oficina-infra-fiap-fase4](../oficina-infra-fiap-fase4/README.md) (Infra DB e
Platform) e antes dos três microsserviços.

## Componentes

| Componente | Funcao | VPC | Secrets |
| ---------- | ------ | --- | ------- |
| `oficina-auth-cpf` | Valida CPF/senha em `OficinaCadastroDb` e emite JWT | Sim | `/oficina/auth/database`, `/oficina/auth/jwt` |
| `oficina-authorizer` | Valida Bearer JWT para HTTP API payload v2 | Nao | `/oficina/auth/jwt` |

## Fluxo

```text
Cliente -> API Gateway -> oficina-auth-cpf -> OficinaCadastroDb -> JWT
Cliente com JWT -> API Gateway -> oficina-authorizer -> claims -> rota autorizada
```

## JWT

- Algoritmo: HS256
- Issuer: `oficina`
- Audience: `oficina-api`
- Expiracao: 60 minutos
- Clock skew: 60 segundos
- Claims: `sub`, `cpf`, `role`, `name`, `iat`, `exp`, `jti`

A chave fica somente no Secrets Manager em `/oficina/auth/jwt`, campo `SigningKey`. O workflow `Auth Deploy` consome o Repository Secret `JWT_SIGNING_KEY`, aplica Terraform, cria o container do secret quando necessario e sincroniza a versao `AWSCURRENT` sem imprimir o valor.

## Banco

A Lambda consulta somente a tabela real `Funcionarios` do `OficinaCadastroDb`, criada pelo Cadastro, com usuario futuro `auth_read`. Campos usados: `Id`, `Cpf`, `Nome`, `Perfil`, `SenhaHash`, `Ativo`. A consulta e parametrizada e nao executa migrations nem escrita.

O hash de senha segue o contrato atual do Cadastro: `PBKDF2-SHA256$100000$salt$hash`.

## Provisionamento

1. `Auth Deploy`
2. Deploy dos microsservicos
3. `Entrypoint Deploy`, que depende das duas Lambdas publicadas com alias `live`

## Limitacoes conhecidas

Esta etapa usa HS256 e nao implementa refresh token, Cognito, MFA ou revogacao imediata de token.

## Validacoes locais

```powershell
dotnet tool restore
dotnet restore Oficina.Auth.sln
dotnet build Oficina.Auth.sln -c Release --no-restore
dotnet test Oficina.Auth.sln -c Release --no-build
pwsh scripts/validate-official-config.ps1
pwsh scripts/package-lambdas.ps1
$env:JWT_SIGNING_KEY = "Synthetic-Jwt-Signing-Key-At-Least-32-Bytes!"
pwsh scripts/sync-jwt-secret.ps1 -DryRun
Remove-Item Env:JWT_SIGNING_KEY
```

Terraform:

```powershell
cd terraform/auth
terraform fmt -recursive
terraform init -backend=false
terraform validate
```

Nao executar `terraform apply`, workflows ou comandos AWS mutantes localmente nesta etapa.

## Próximo componente

Após `Auth Deploy`, siga para
[oficina-cadastro-fiap-fase4](../oficina-cadastro-fiap-fase4/README.md),
[oficina-estoque-fiap-fase4](../oficina-estoque-fiap-fase4/README.md) e
[oficina-ordens-servico-fiap-fase4](../oficina-ordens-servico-fiap-fase4/README.md),
que podem ser implantados de forma independente e em paralelo.
