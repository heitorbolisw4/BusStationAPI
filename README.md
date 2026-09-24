# BusStationAPI

[![CI](https://github.com/heitorbolisw4/BusStationAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/heitorbolisw4/BusStationAPI/actions/workflows/ci.yml)

API do BusStation (ASP.NET Core 10, minimal API, EF Core + PostgreSQL, JWT com dois schemes: `UserScheme` e `AdminScheme`).
Visão técnica: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). Tickets: [`docs/BACKLOG.md`](docs/BACKLOG.md).

## Rodando local (dev)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=busstation;Username=postgres;Password=<senha>"
dotnet user-secrets set "JwtSettings:User:SecretKey"  "<no mínimo 32 caracteres>"
dotnet user-secrets set "JwtSettings:Admin:SecretKey" "<no mínimo 32 caracteres, diferente da de cima>"
dotnet ef database update
dotnet run
```

Testes E2E (precisam de um Postgres acessível; o banco `<nome>_e2e` é apagado e recriado a cada execução):

```bash
dotnet test
```

## CI

`.github/workflows/ci.yml` roda em todo PR e em push na `main`:

1. **build-test:** `restore` → `build -c Release` → `test`. A suíte E2E roda contra um `postgres:17` subido como `services:` do Actions (`BUSSTATION_E2E_CONNECTION`). As chaves JWT são geradas na hora com `openssl rand` e mascaradas no log.
2. **docker:** `docker build` da imagem, para que um Dockerfile quebrado seja pego no PR. Não publica a imagem.

## Docker

A imagem é a mesma que o Railway usa: `Dockerfile` multi-stage (`sdk:10.0` compila → `aspnet:10.0` roda, como usuário não-root `app`), escutando em `8080` (ou na `PORT` do provedor).

Ensaio local com Postgres (`docker-compose.yml`):

```bash
cp .env.example .env        # preencha POSTGRES_PASSWORD e as duas JWT_*_SECRET_KEY
docker compose up --build
curl http://localhost:8080/health/ready   # 200 Healthy (inclui o Postgres)
```

O Postgres do compose fica em `localhost:5433`. As migrations **não** rodam no boot: aplique com o `efbundle` (próxima seção) usando `Host=localhost;Port=5433;Database=busstation;Username=postgres;Password=<POSTGRES_PASSWORD>`. Se quiser o primeiro admin, preencha `BOOTSTRAP_ADMIN_*` no `.env` e rode `docker compose restart api` depois da migration.

## Migrations em deploy (efbundle)

A API **não** aplica migration no boot (`Database.Migrate()` no startup é proibido: corrida entre instâncias e deploy misturado com boot). As migrations, incluindo o seed `HasData`, são um passo explícito de release, feito com um executável gerado pelo EF (`efbundle`).

### No Railway (automático)

A imagem Docker já traz o `efbundle` em `/app/efbundle`, gerado no build com o `dotnet-ef` fixado em `dotnet-tools.json`. No serviço do Railway:

- **Settings → Deploy → Pre-deploy Command:** `./efbundle`
- **Variables:** `MIGRATIONS_CONNECTION` = connection string **direta** do Neon (host **sem** `-pooler`), formato Npgsql, sem aspas.

O Railway roda o bundle antes de trocar a versão no ar. Se a migration falhar, o deploy é abortado e a versão anterior continua servindo. Sem `MIGRATIONS_CONNECTION`, o bundle cai na `ConnectionStrings__DefaultConnection`, que é a pooled: funciona, mas migration pelo pooler não é o recomendado.

**Ordem de precedência da connection string no bundle:** `--connection` → `MIGRATIONS_CONNECTION` → `ConnectionStrings__DefaultConnection` (ambiente ou user-secrets).

**Migration que quebra o código no ar** (renomear ou remover coluna, por exemplo) precisa ser feita em duas etapas, *expand/contract*: primeiro uma versão que aceita os dois formatos, depois a limpeza. O pre-deploy roda a migration **antes** da versão nova subir, então durante alguns segundos o código antigo roda contra o schema novo.

### Manual (máquina do dev)

Continua valendo para testar migration nova numa branch descartável do Neon, ou se o pre-deploy estiver desligado:

```bash
# 1. gerar o bundle (fica fora do git: efbundle/efbundle.exe estão no .gitignore)
dotnet ef migrations bundle -o efbundle.exe --force        # Windows
dotnet ef migrations bundle -o efbundle --force            # Linux/macOS

# 2. aplicar com a connection string DIRETA do Neon (host SEM -pooler), formato Npgsql
./efbundle.exe --connection "Host=<host-direto>;Database=neondb;Username=neondb_owner;Password=<senha>;SSL Mode=Require;Channel Binding=Require"
```

- **Idempotente:** rodar de novo só imprime `No migrations were applied. The database is already up to date.`
- **Senha:** `neon cs production --project-id green-heart-40389256` devolve a URI com a senha. Converta para o formato Npgsql (seção "Connection string do Neon") e não cole a senha em arquivo versionado nem em issue.
- **Teste antes em branch descartável do Neon** (recomendado quando a migration é nova):
  ```bash
  neon branches create --project-id green-heart-40389256 --name deploy-test --parent production
  neon cs deploy-test --project-id green-heart-40389256      # rode o efbundle contra esta
  neon branches delete deploy-test --project-id green-heart-40389256
  ```
- **Primeiro admin:** não está na migration. Vem do seed de startup (`BOOTSTRAP_ADMIN_EMAIL`/`BOOTSTRAP_ADMIN_PASSWORD`), que só cria se não existir nenhum admin. Se a API subir antes da migration, o seed falha só no log e é tentado de novo no próximo boot.
- As ferramentas do EF usam `Data/DesignTimeDbContextFactory.cs`, e não o `Program.cs`, para o fail-fast de runtime (JWT, CORS) não bloquear o bundle.

## Configuração (variáveis de ambiente)

Fora de Development não existe user-secrets: tudo vem de variável de ambiente. Se faltar algo obrigatório, **a API não sobe** e o log diz exatamente o que falta (fail-fast).

| Variável | Obrigatória | Exemplo / observação |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | sim | formato Npgsql, veja abaixo |
| `JwtSettings__User__SecretKey` | sim | ≥ 32 bytes (HMAC-SHA256). `openssl rand -base64 48` |
| `JwtSettings__Admin__SecretKey` | sim | ≥ 32 bytes, diferente da de User |
| `Cors__AllowedOrigins` | sim fora de Development | `https://seu-front.vercel.app`. Várias origens: separe por vírgula. Em Development, sem valor, libera `http://localhost:5173` |
| `ASPNETCORE_ENVIRONMENT` | recomendado | `Staging` no staging (Swagger ligado). `Production` desliga o Swagger |
| `PORT` | não | porta HTTP. O Railway injeta sozinho. Sem ela, vale `ASPNETCORE_HTTP_PORTS` (8080 na imagem) |
| `BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD` | não | cria o **primeiro** admin no startup, só se as duas estiverem definidas e não existir nenhum admin. Depois do primeiro boot, pode apagar as duas |

`Issuer`/`Audience` do JWT não são segredo e têm valor padrão no `appsettings.json`.

### Connection string do Neon (formato Npgsql)

O painel/CLI do Neon entrega uma URI (`postgresql://user:senha@host/neondb?sslmode=require&channel_binding=require`), mas o Npgsql **não aceita URI**. A conversão é feita **à mão, na hora de cadastrar a variável** (não há parser no código):

```
Host=<host>;Database=neondb;Username=neondb_owner;Password=<senha>;SSL Mode=Require;Channel Binding=Require
```

- **Runtime da API (Railway):** host **pooled** (`...-pooler...neon.tech`).
- **Migrations (efbundle):** host **direto** (sem `-pooler`).

### Endpoints de operação

- `GET /health`: liveness, 200 `Healthy` se o processo está de pé. **Não consulta o banco**, porque é o health check do serviço no Railway: se batesse no Postgres a cada checagem, o Neon nunca suspenderia e gastaria as horas de compute do plano free.
- `GET /health/ready`: 200 quando o Postgres responde, 503 quando não. Use para checagem manual e no smoke test.
- `GET /swagger`: UI do Swagger (fora de `Production`).

## Autenticação de cliente (access + refresh token)

- `POST /login` → `200 { token, refreshToken, expiresIn }`. O `token` é o JWT de acesso (padrão 15 min, `JwtSettings__User__ExpirationTimeInMinutes`), e `expiresIn` diz em quantos segundos ele expira. O `refreshToken` é opaco e vale 7 dias (`JwtSettings__User__RefreshExpirationTimeInMinutes`).
- `POST /refresh { refreshToken }` → `200` com um par **novo**. O refresh token usado deixa de valer (rotação). Token inválido, expirado ou já usado → `401`, e o cliente deve mandar para o login.
- `POST /logout { refreshToken }` → sempre `204`.
- **Reuso = sessão comprometida:** se um refresh token já usado for apresentado de novo, **todas** as sessões do usuário são revogadas. Consequência para o front: dois `/refresh` em paralelo com o mesmo token derrubam a sessão, então o cliente precisa fazer o refresh **uma vez só** (single-flight) e reaproveitar o resultado nas requests que estavam esperando.
- O banco guarda só o hash SHA-256 do refresh token (tabela `RefreshTokens`).
- Admin continua só com access token (sem refresh).

## Deploy no Railway (API)

Staging: `https://busstationapi-production.up.railway.app`. Serviço com build pelo **Dockerfile**, a partir deste repo, com deploy a cada push em `main`.

- **Health check:** `/health`
- **Variables:** as da tabela acima (`ASPNETCORE_ENVIRONMENT=Staging`, connection string **pooled** em formato Npgsql, **sem aspas** em volta do valor).
- A porta vem de `PORT`, que o Railway define sozinho; não é preciso configurar nada.
- O TLS termina no proxy do Railway; a API lê `X-Forwarded-For`/`X-Forwarded-Proto` (`UseForwardedHeaders`).
- **Antes** de cada deploy que traz migration nova: rodar o `efbundle` (seção "Migrations em deploy").
