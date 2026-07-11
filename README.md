# oficina-auth-lambda-fiap-fase4

Implementa autenticacao por CPF/senha e validacao JWT para a Fase 4.

## Componentes

| Componente | Funcao | VPC | Secrets |
| ---------- | ------ | --- | ------- |
| `oficina-auth-cpf` | Valida CPF/senha em `OficinaCadastroDb` e emite JWT | Sim | `/oficina/auth/database`, `/oficina/auth/jwt` |
| `oficina-authorizer` | Valida Bearer JWT para HTTP API payload v2 | Nao | `/oficina/auth/jwt` |

## Fluxo

```text
Cliente -> API Gateway futuro -> oficina-auth-cpf -> OficinaCadastroDb -> JWT
Cliente com JWT -> API Gateway futuro -> oficina-authorizer -> claims -> rota autorizada
```

## JWT

- Algoritmo: HS256
- Issuer: `oficina`
- Audience: `oficina-api`
- Expiracao: 60 minutos
- Clock skew: 60 segundos
- Claims: `sub`, `cpf`, `role`, `name`, `iat`, `exp`, `jti`

A chave fica somente no Secrets Manager em `/oficina/auth/jwt`, campo `SigningKey`. O deploy nao recebe `JWT_SIGNING_KEY`; a sincronizacao ocorre pelo workflow `Auth JWT Secret Sync`.

## Banco

A Lambda consulta somente a tabela real `Funcionarios` do `OficinaCadastroDb`, criada pelo Cadastro, com usuario futuro `auth_read`. Campos usados: `Id`, `Cpf`, `Nome`, `Perfil`, `SenhaHash`, `Ativo`. A consulta e parametrizada e nao executa migrations nem escrita.

O hash de senha segue o contrato atual do Cadastro: `PBKDF2-SHA256$100000$salt$hash`.

## Execucao futura

1. `Auth JWT Secret Sync`
2. `Auth Deploy`
3. `Auth Smoke Test`
4. Deploy do Entrypoint em etapa posterior

## Rollback

Rollback altera apenas o alias `live` para uma versao publicada anterior. Nao apaga versoes, nao altera `$LATEST`, nao altera banco e nao faz rollback de secret.

## Limitacoes academicas

Esta etapa usa HS256 e nao implementa refresh token, Cognito, MFA ou revogacao imediata. Em ambiente corporativo, avaliar RS256/ES256, KMS, IdP central, rotacao automatizada e MFA.

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
