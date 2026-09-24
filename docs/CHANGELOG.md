# Changelog

> Uma linha por entrega, com hash do commit. Histórico completo e detalhado fica no `git log` — aqui é só o "o que foi entregue", não o "como".

## 2026-09-24

- **[DONE] CHORE-024: staging v1 no ar.** API no Railway (`busstationapi-production.up.railway.app`) + Neon (migrations via `efbundle`) + front no Vercel (`bus-station-tau.vercel.app`). Smoke via curl ok (health, register, login, search, tickets, 401/403 de admin). O fluxo de compra pela UI fica para o `FEAT-025`.
- **[DONE] API pronta para deploy em staging** (merge de `chore/deploy-staging`, `10eba9a`). Fecha:
  - `BUG-013`: login de admin com senha certa; `/admin/create` só para admin; primeiro admin por seed via `BOOTSTRAP_ADMIN_*`.
  - `FEAT-003`: `AdminPolicy` em `/cities`, `/routes` e `/boardings`, com leitura pública só em `GET /cities/list` e `GET /boardings/search`. Token de cliente em rota de admin agora recebe **403** (antes 401), inclusive em `/prices` e `/distances`.
  - `CHORE-022`: CORS por config, fail-fast de segredos, `/health` (liveness) + `/health/ready` (Postgres), Swagger desligado só em Production, forwarded headers, porta via `PORT`.
  - `CHORE-012`: Dockerfile multi-stage não-root + docker-compose local.
  - `CHORE-023`: migrations por `efbundle`, fora do startup.
  - `CHORE-011`: CI no GitHub Actions (build + E2E contra Postgres + `docker build`).
- **[DONE] BUG-014: compra de passagem voltou a funcionar** — grupo `/tickets` passa a exigir `UserPolicy` (antes caía na policy default, que desafiava o scheme `Bearer` nunca registrado → 500 em toda request). Testes E2E de passagem/jornada do cliente reativados. `a9c29c6`
- **[DONE] Suíte de testes E2E da API** (`tests/BusStation_API.E2E`, fecha `CHORE-010`) — 44 cenários por jornada (cliente, operação/admin, busca, passagens, conta, cidades) contra Postgres real. Achou 2 bugs novos: `BUG-014` (compra de passagem sempre 500) e `BUG-015` (reajuste de preço 500 com 2+ rotas); os testes deles, de `BUG-001` e de `BUG-013` ficam com `Skip` até a correção.
- **[DONE] Terminada a extração de `Program.cs` e removidos os DTOs antigos** (`DTO/Admin`, `Boarding`, `Destination`, `Distance`, `Origin`, `Route`) — `POST /admin/create` foi para `AuthEndpoints`; `GET /boardings/search` (estava comentado, o front depende dele) voltou em `BoardingEndpoints`; `/distances` voltou a exigir `AdminPolicy` (estava anônimo desde a extração); grupos mortos removidos (fecha `CHORE-004`). `6227a5f`

## 2026-09-18

- **[DONE] Fechado o módulo de Tickets (RF-26–RF-31) e resolvida a decisão D-02** — `Ticket` passou a referenciar `Boarding` em vez de `Route` (débito de vaga agora ocorre em `Boarding.Seat`); adicionados `GET /tickets/list` e `GET /tickets/list/{id}`, escopados ao usuário autenticado. Corrigidos 3 bugs do refactor em andamento: claim JWT errada (todo login autenticado recebia 401 ao comprar), `Ticket.UserId` nunca setado, débito de vaga que tinha sumido. `86596d3`
- **[DONE] Extraído o módulo de Prices** (`Create`/`Update`/`List`) de `Program.cs` para `Endpoints/PriceEndpoints.cs`; corrigida validação com lógica invertida que impedia o arquivo de compilar. `86596d3`
- **[DONE] Documentação do projeto reorganizada** — `docs/BACKLOG.md` (fila de trabalho MoSCoW) e `docs/ARCHITECTURE.md` (visão técnica + dívida técnica atualizada) criados; `REQUISITOS.md` passa a ser só o PRD (visão, RF/RN/RNF, decisões de produto).

## Histórico anterior (resumo — ver `git log` para o detalhe)

- Extraídos os módulos de City, Route, Distance, Boarding, Auth e User de `Program.cs` para `Endpoints/*.cs` dedicados (`c0f848e`, `dd54b2b`, `69e3199`, `4c6527c`, `b8508ff`)
- `GET /cities/list` liberado para anônimo; adicionado `GET /boardings/search` com DTO próprio de tela (`4ac3744`)
