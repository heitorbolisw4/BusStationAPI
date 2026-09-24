# BusStation API — Arquitetura

> Visão técnica de como o sistema é construído hoje, e onde mora a dívida técnica conhecida. Este documento é revisado sempre que a estrutura do código muda de forma relevante (não é histórico — para isso, ver `CHANGELOG.md`).

---

## 1. Stack

- **Runtime:** .NET 10, ASP.NET Core Minimal API
- **Banco:** PostgreSQL via EF Core (Npgsql), migrations versionadas em `Migrations/`
- **Autenticação:** cliente usa access token JWT curto + refresh token opaco rotativo (ver §7); admin só access token. JWT com dois schemes separados (`UserScheme`, `AdminScheme`) e duas policies. `UserPolicy` só aceita o `UserScheme`. `AdminPolicy` autentica pelos dois schemes e exige a claim `"adm"`: sem token → 401, token de cliente → 403, token de admin → ok.
- **Documentação de API:** Swagger/OpenAPI, ativo em todo ambiente exceto `Production` (ver §6)
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
2. ~~**RNF-01:** `/cities`, `/routes` e `/boardings` sem autenticação.~~ Resolvido (`FEAT-003`): os três grupos exigem `AdminPolicy`, com `AllowAnonymous()` só em `GET /cities/list` e `GET /boardings/search`, que o front chama sem login. Os GETs de `/routes` ficaram protegidos, porque o front não os usa e o `/boardings/search` já traz nome, km e preço da rota.
3. **Valores monetários em `float`** (`Price.PricePerKm`, `Route.Price`, `Ticket.FarePaid`) — RNF-03. `decimal` é o tipo correto para dinheiro (evita erro de arredondamento binário). → `DEBT-002`
4. ~~**Login de admin com verificação de senha invertida + `POST /admin/create` anônimo**~~ Resolvido (`BUG-013`). `/admin/create` exige `AdminPolicy`, e o primeiro admin vem do seed de bootstrap (§6).
8. ~~**Grupo `/tickets` sem policy** (500 em toda request)~~ Resolvido (`BUG-014`, `a9c29c6`): `/tickets` exige `UserPolicy`.
9. **`PriceEndpoints.UpdatePrice` assume 1 rota por distância** (`SingleOrDefaultAsync`). → `BUG-015`
5. **Sem padronização de erro** (RNF-04) — cada endpoint devolve `BadRequest(new { message })` manualmente; sem `ProblemDetails` nem middleware central. → `DEBT-005`
6. **Sem paginação em `/list`** (RNF-06) — aceitável com o volume atual de seed data, não escala. → `DEBT-006`
7. **Convenção de rota inconsistente** (RNF-05) — mistura `/create`, `/list/{id}` com verbos HTTP que já expressam a ação (ex.: `CityEndpoints` usa `POST /create` em vez de `POST /`). → `DEBT-008`

**Achados antigos já corrigidos** (estavam listados na seção 10 do `REQUISITOS.md` original, de 26/07/2026, e não se aplicam mais ao código atual — removidos de lá para não virar informação morta): `routes.MapGet("/list/{id:int}")` não retornava `Ok(...)`; `routes.MapPatch("/update/{id:int}")` tinha o recálculo de preço comentado; `Route.TicketId` órfão; endpoint de criação de ticket inteiro comentado. Todos resolvidos nos commits de refactor até 18/09/2026.

## 5. CI/CD e ambientes

Escopo aprovado em `../docs/deploy/escopo-deploy.md` (repo raiz).

- **Hospedagem (decidida em 2026-09-24):** Railway (API, Dockerfile; plano inicial era Render) + Neon (Postgres, sa-east-1) + Vercel (front). Staging é o único ambiente público na v1.
- **CI:** GitHub Actions (`.github/workflows/ci.yml`), em PR e em push na `main`: build + suíte E2E contra `postgres` como service container + `docker build`.
- **CD:** na v1 o deploy é disparado à mão, e as migrations rodam antes, também à mão, via `efbundle` (§6). CD automático é a fase 2 (`CHORE-020`).
- **Testes E2E (existem desde 2026-09-24):** `tests/BusStation_API.E2E` — xUnit + `WebApplicationFactory<Program>`, API inteira em memória falando HTTP contra um Postgres real. O banco é `<DefaultConnection>_e2e`, apagado e recriado pelas migrations a cada execução (o de dev nunca é tocado). Connection string: `BUSSTATION_E2E_CONNECTION` ou, na falta dela, a dos user-secrets da API. Rodar: `dotnet test` na raiz do repo. Testes que expõem bug aberto ficam com `Skip = "BUG-xxx: ..."` — corrigir o bug = remover o `Skip` e ver verde.
- **Testcontainers** continua sendo o próximo passo (dispensa Postgres instalado), mas depende do Docker Desktop rodando.

## 6. Configuração de deploy (decisões)

- **Swagger:** UI e JSON ligados em Development e Staging, desligados só com `ASPNETCORE_ENVIRONMENT=Production`. Staging é ambiente de estudo, e o Swagger ajuda a debugar ali. Como hoje não existe `Production`, a regra já deixa o caminho pronto.
- **CORS:** origens vêm de `Cors:AllowedOrigins` (lista ou valor único separado por vírgula) e a policy é aplicada em todo ambiente. Em Development, sem configuração, cai em `http://localhost:5173`. Fora de Development, a lista vazia derruba o startup. A policy é montada via `IOptions<CorsOptions>` lendo a configuração final, para o E2E conseguir trocar as origens por host.
- **Fail-fast:** `StartupConfig.EnsureRequiredSettings` falha o startup, listando tudo o que falta, se estiverem vazias a connection string ou as duas `SecretKey`, ou se alguma chave tiver menos de 32 bytes (limite do HMAC-SHA256). `Issuer`/`Audience` não são segredo e têm padrão no `appsettings.json`.
- **Connection string do Neon:** convertida **à mão** de URI para o formato Npgsql na hora de cadastrar a variável (documentado no README). Um parser no startup seria só mais código para testar, e resolve um problema que acontece uma vez.
- **Health check:** `GET /health` é liveness e não toca no banco. É o health check do serviço no Railway, e se consultasse o Postgres a cada checagem o compute do Neon nunca suspenderia, gastando as horas do plano free. `GET /health/ready` inclui `AddDbContextCheck<AppDbContext>` (um `CanConnect` no Postgres), que é o pacote first-party da Microsoft, na mesma versão do EF Core.
- **Proxy/porta:** `UseForwardedHeaders` (For + Proto) com as listas de proxies conhecidos limpas, porque o IP do proxy do provedor não é fixo. A porta vem de `PORT` quando existe (Render), senão de `ASPNETCORE_HTTP_PORTS`.
- **Primeiro admin:** `AdminBootstrapper` roda no startup (`IHostedService`) e cria um admin a partir de `BOOTSTRAP_ADMIN_EMAIL`/`BOOTSTRAP_ADMIN_PASSWORD` só se não existir nenhum. Se o banco ainda não tiver as tabelas, registra o erro no log e a API sobe mesmo assim. O seed não roda dentro da migration para que a senha não vá parar no repositório.
- **Migrations em deploy:** `dotnet ef migrations bundle` gera o `efbundle` (fora do git), rodado contra a connection string **direta** do Neon antes do deploy. Nunca `Database.Migrate()` no startup. As ferramentas do EF usam `DesignTimeDbContextFactory` (user-secrets/variável de ambiente, ou `--connection`) em vez de subir o `Program.cs`, então o bundle não depende das chaves JWT nem do CORS.

## 7. Refresh token (decisões, 2026-09-24)

- **Opaco, não JWT:** precisa ser revogável (logout, reuso), e um JWT só "morre" quando expira. É um valor aleatório de 512 bits, e o banco guarda apenas o SHA-256. Não é BCrypt porque não há dicionário para atacar num valor aleatório, e precisamos buscar pelo hash.
- **Rotação com detecção de reuso:** cada `/refresh` revoga o token usado e emite outro. Se um token revogado reaparece, todas as sessões do usuário são revogadas. A revogação usa `ExecuteUpdate ... WHERE RevokedAt IS NULL`, então de dois refresh concorrentes só um vence, e o outro conta como reuso.
- **No corpo JSON, não em cookie HttpOnly:** front (`vercel.app`) e API (`railway.app`) estão em sites diferentes, e os navegadores bloqueiam cada vez mais cookie de terceiro (`SameSite=None`). O custo é o token ficar acessível a JavaScript (risco de XSS), o que é aceito para staging de estudo. Com domínio próprio para front e API, dá para migrar para cookie.
- **Só cliente:** admin mantém token curto sem refresh. Sessão longa de admin é risco maior e não há tela de admin ainda.
- **Dívida:** tokens expirados/revogados acumulam na tabela. Precisa de limpeza periódica (`DEBT-028`).

