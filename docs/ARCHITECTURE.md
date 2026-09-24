# BusStation API — Arquitetura

> Visão técnica de como o sistema é construído hoje, e onde mora a dívida técnica conhecida. Este documento é revisado sempre que a estrutura do código muda de forma relevante (não é histórico — para isso, ver `CHANGELOG.md`).

---

## 1. Stack

- **Runtime:** .NET 10, ASP.NET Core Minimal API
- **Banco:** PostgreSQL via EF Core (Npgsql), migrations versionadas em `Migrations/`
- **Autenticação:** JWT com dois schemes separados (`UserScheme`, `AdminScheme`) e duas policies (`UserPolicy`, `AdminPolicy` — esta exige a claim `"adm"`)
- **Documentação de API:** Swagger/OpenAPI (`app.MapSwagger()`, ativo em Development)
- **Frontend:** React + Vite, consumindo a API via `src/api/*.js` (repo separado, `BusStationFrontEnd`)

## 2. Camadas

```
Program.cs            bootstrap: DI, auth schemes, CORS, Swagger, grupos de rota, wiring dos módulos
  └─ Endpoints/*.cs    extension methods (MapXEndpoints), um arquivo por módulo de domínio
       └─ DTO/*Contracts.cs   records de request/response (convenção: um "Contracts" estático por módulo)
            └─ Entities/*.cs  entidades EF Core; regra de validação simples fica estática na própria entidade
                              (ex.: Price.CreateValidPrice) quando é só checagem de formato/obrigatoriedade
       └─ Data/AppDbContext.cs   Fluent API (OnModelCreating) + seed data (HasData)
```

Módulos já extraídos para `Endpoints/`: `AuthEndpoints`, `UserEndpoints`, `CityEndpoints`, `RouteEndpoints`, `DistanceEndpoints`, `BoardingEndpoints`, `PriceEndpoints`, `TicketEndpoints`. `Program.cs` hoje só tem o bootstrap (DI, auth, CORS, Swagger) e o wiring dos grupos; a criação de admin foi para `AuthEndpoints` e as classes antigas `DTO/<Módulo>/*Dto.cs` foram removidas (`6227a5f`).

## 3. Modelo de domínio

```
City ──< Distance (km, direcional: A→B e B→A são registros diferentes — ver D-05)
Distance ──< Price (preço por km, histórico — o último Id é o "vigente")
Distance ──< Route (rota comercial nomeada; preço é congelado no momento da criação — ver D-03)
Route ──< Boarding (uma saída concreta: data + hora + vagas)
Boarding ──< Ticket (compra de um usuário para um embarque específico)
User ──< Ticket
```

`Ticket` referencia `BoardingId` (não `RouteId`) desde 2026-09-18 — decisão D-02 em `REQUISITOS.md`. `RouteId`/`RouteName` continuam acessíveis via `Ticket.Boarding.Routes`.

## 4. Dívida técnica conhecida (verificado em 2026-09-18)

Itens abaixo têm ticket correspondente em `BACKLOG.md` — este documento explica o *porquê*, o backlog controla o *quando*.

1. **Checagem de cidade duplicada não cobre `Acronym`** (`CityEndpoints.Create`) — só compara `CityName`. `Acronym` tem índice único no banco (`AppDbContext`), então duas cidades com o mesmo `Acronym` não caem no `Results.Conflict()` esperado: estouram `DbUpdateException` não tratada (500 cru). → `BUG-001`
2. **RNF-01 ainda aberto (D-01 não resolvida):** os grupos `/cities`, `/routes` e `/boardings` não exigem nenhuma autenticação — nem login, nem `AdminPolicy`. A infraestrutura de Admin já existe e funciona (`AdminTokenService` emite a claim `"adm"`, `AdminPolicy` já é usada em `/prices`, `/distances`), só falta aplicar aos grupos certos. → `FEAT-003`
3. **Valores monetários em `float`** (`Price.PricePerKm`, `Route.Price`, `Ticket.FarePaid`) — RNF-03. `decimal` é o tipo correto para dinheiro (evita erro de arredondamento binário). → `DEBT-002`
4. **Login de admin com verificação de senha invertida + `POST /admin/create` anônimo** (`AuthEndpoints`). → `BUG-013`
8. **`PriceEndpoints.UpdatePrice` assume 1 rota por distância** (`SingleOrDefaultAsync`). → `BUG-015`
5. **Sem padronização de erro** (RNF-04) — cada endpoint devolve `BadRequest(new { message })` manualmente; sem `ProblemDetails` nem middleware central. → `DEBT-005`
6. **Sem paginação em `/list`** (RNF-06) — aceitável com o volume atual de seed data, não escala. → `DEBT-006`
7. **Convenção de rota inconsistente** (RNF-05) — mistura `/create`, `/list/{id}` com verbos HTTP que já expressam a ação (ex.: `CityEndpoints` usa `POST /create` em vez de `POST /`). → `DEBT-008`

**Achados antigos já corrigidos** (estavam listados na seção 10 do `REQUISITOS.md` original, de 26/07/2026, e não se aplicam mais ao código atual — removidos de lá para não virar informação morta): `routes.MapGet("/list/{id:int}")` não retornava `Ok(...)`; `routes.MapPatch("/update/{id:int}")` tinha o recálculo de preço comentado; `Route.TicketId` órfão; endpoint de criação de ticket inteiro comentado. Todos resolvidos nos commits de refactor até 18/09/2026.

## 5. CI/CD e ambientes (planejado — ver `BACKLOG.md`, ainda não implementado)

- **CI:** GitHub Actions — `dotnet build` + `dotnet test` em todo PR.
- **CD:** deploy automático em `staging` a cada merge em `main`; `prod` por deploy manual/tag.
- **Hospedagem candidata:** Railway ou Fly.io para API + Postgres (Dockerfile); Vercel ou Netlify para o frontend.
- **Testes E2E (existem desde 2026-09-24):** `tests/BusStation_API.E2E` — xUnit + `WebApplicationFactory<Program>`, API inteira em memória falando HTTP contra um Postgres real. O banco é `<DefaultConnection>_e2e`, apagado e recriado pelas migrations a cada execução (o de dev nunca é tocado). Connection string: `BUSSTATION_E2E_CONNECTION` ou, na falta dela, a dos user-secrets da API. Rodar: `dotnet test` na raiz do repo. Testes que expõem bug aberto ficam com `Skip = "BUG-xxx: ..."` — corrigir o bug = remover o `Skip` e ver verde.
- **Testcontainers** continua sendo o próximo passo (dispensa Postgres instalado), mas depende do Docker Desktop rodando.
