# BusStationAPI

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

## Configuração (variáveis de ambiente)

Fora de Development não existe user-secrets: tudo vem de variável de ambiente. Se faltar algo obrigatório, **a API não sobe** e o log diz exatamente o que falta (fail-fast).

| Variável | Obrigatória | Exemplo / observação |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | sim | formato Npgsql, veja abaixo |
| `JwtSettings__User__SecretKey` | sim | ≥ 32 bytes (HMAC-SHA256). `openssl rand -base64 48` |
| `JwtSettings__Admin__SecretKey` | sim | ≥ 32 bytes, diferente da de User |
| `Cors__AllowedOrigins` | sim fora de Development | `https://seu-front.vercel.app`. Várias origens: separe por vírgula. Em Development, sem valor, libera `http://localhost:5173` |
| `ASPNETCORE_ENVIRONMENT` | recomendado | `Staging` no staging (Swagger ligado). `Production` desliga o Swagger |
| `PORT` | não | porta HTTP. O Render injeta sozinho (padrão 10000). Sem ela, vale `ASPNETCORE_HTTP_PORTS` (8080 na imagem) |
| `BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD` | não | cria o **primeiro** admin no startup, só se as duas estiverem definidas e não existir nenhum admin. Depois do primeiro boot, pode apagar as duas |

`Issuer`/`Audience` do JWT não são segredo e têm valor padrão no `appsettings.json`.

### Connection string do Neon (formato Npgsql)

O painel/CLI do Neon entrega uma URI (`postgresql://user:senha@host/neondb?sslmode=require&channel_binding=require`), mas o Npgsql **não aceita URI**. A conversão é feita **à mão, na hora de cadastrar a variável** (não há parser no código):

```
Host=<host>;Database=neondb;Username=neondb_owner;Password=<senha>;SSL Mode=Require;Channel Binding=Require
```

- **Runtime da API (Render):** host **pooled** (`...-pooler...neon.tech`).
- **Migrations (efbundle):** host **direto** (sem `-pooler`).

### Endpoints de operação

- `GET /health`: 200 `Healthy` quando o Postgres responde, 503 quando não. É o `healthCheckPath` do Render.
- `GET /swagger`: UI do Swagger (fora de `Production`).

## Deploy no Render (API)

Web Service com runtime **Docker**, a partir deste repo, região **Virginia (US East)**.

- **Health Check Path:** `/health`
- **Environment:** as variáveis da tabela acima (`ASPNETCORE_ENVIRONMENT=Staging`, connection string **pooled**).
- A porta vem de `PORT`, que o Render define sozinho; não é preciso configurar nada.
- O TLS termina no proxy do Render; a API lê `X-Forwarded-For`/`X-Forwarded-Proto` (`UseForwardedHeaders`).
- **Antes** de cada deploy que traz migration nova: rodar o `efbundle` (seção "Migrations em deploy").
