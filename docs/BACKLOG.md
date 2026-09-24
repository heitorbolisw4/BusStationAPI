# BusStation — Backlog

> Fila de trabalho viva, priorizada em MoSCoW. Metodologia: **Kanban** — fluxo contínuo, sem sprint fixo. Puxe o próximo item pela prioridade (Must antes de Should antes de Could), revise esta lista a cada 2-3 itens fechados (não por calendário). Todo item fechado sai daqui e vira uma linha em `CHANGELOG.md`.
>
> Legenda de prioridade: **Must** (bug de dado/dinheiro ou brecha de segurança ativa) · **Should** (falta sentida por um usuário real, ou destrava prática de teste/deploy) · **Could** (melhora não crítica) · **Won't agora** (ver seção 9 do `REQUISITOS.md`).
>
> Formato de ticket: ID, prioridade, estimativa (T-shirt: XS < 1h, S ~meio dia, M ~1-2 dias, L > 2 dias), origem (rastreabilidade), contexto, critério de aceite testável, status.

---

### [BUG-001] Checagem de cidade duplicada não valida `Acronym`
**Prioridade:** Must | **Estimativa:** XS
**Origem:** `docs/ARCHITECTURE.md` §4.1
**Contexto:** `CityEndpoints.Create` só compara `CityName`. `Acronym` tem índice único no banco — duas cidades com `Acronym` repetido não caem no `Results.Conflict()` esperado, estouram `DbUpdateException` não tratada (500 cru pro cliente).
**Critério de aceite:**
- [ ] `POST /cities/create` retorna `409 Conflict` quando `Acronym` já existe (mesmo com `CityName` diferente)
- [ ] Teste de regressão cobre: mesmo `Acronym`/`CityName` diferente → 409; `CityName` igual/`Acronym` diferente → comportamento atual preservado
**Status:** To Do

---

### [BUG-009] Login de admin aceita senha errada e rejeita a certa
**Prioridade:** Must | **Estimativa:** XS
**Origem:** encontrado durante o refactor `6227a5f`
**Contexto:** `AuthEndpoints.AdminLogin` checa `service.PasswordVerify(...)` sem o `!` (o `Login` de usuário tem). Resultado: token de admin sai pra quem erra a senha, e quem acerta leva 401. Além disso, `POST /admin/create` é anônimo: qualquer um cria um admin.
**Critério de aceite:**
- [ ] `POST /admin/login` com senha correta → `200` + token; com senha errada → `401`
- [ ] Teste de regressão cobre os dois casos
- [ ] Decidir com a liderança quem pode chamar `POST /admin/create` (seed + `AdminPolicy`?) e registrar a decisão
**Status:** To Do

---

### [DEBT-002] Valores monetários em `float` deveriam ser `decimal`
**Prioridade:** Must | **Estimativa:** M
**Origem:** RNF-03 (`REQUISITOS.md`), `ARCHITECTURE.md` §4.3
**Contexto:** `Price.PricePerKm`, `Route.Price`, `Ticket.FarePaid` usam `float` — risco de erro de arredondamento binário em dinheiro. Requer migration (mudança de tipo de coluna) e conferir cálculos (`km × preço/km`) continuam corretos.
**Critério de aceite:**
- [ ] As três propriedades migradas para `decimal`
- [ ] Migration aplicada sem perda de dado no ambiente local
- [ ] Teste cobre um caso de arredondamento que hoje falharia em `float` (ex.: 0.1 + 0.2 style) e passa em `decimal`
**Status:** To Do

---

### [FEAT-003] Papel de Admin protegendo `/cities`, `/routes` e `/boardings`
**Prioridade:** Must | **Estimativa:** S
**Origem:** RNF-01 / Decisão D-01 (`REQUISITOS.md`)
**Contexto:** `AdminPolicy` e a claim `"adm"` já existem e funcionam (usadas em `/prices`, `/distances`). Só falta aplicar `.RequireAuthorization("AdminPolicy")` aos grupos `cities`, `routes` e `boardings` em `Program.cs`, que hoje são públicos.
**Critério de aceite:**
- [ ] Request anônima a `POST /cities/create`, `POST /routes/create` e `POST /boardings/create` retorna `401`
- [ ] Request com token de User (não Admin) retorna `403`
- [ ] Request com token de Admin continua funcionando
- [ ] `GET`s de leitura pública (busca de rotas/embarques) continuam acessíveis sem login — confirmar quais realmente devem ficar públicos antes de proteger o grupo inteiro
**Status:** To Do

---

### [DEBT-005] Padronizar respostas de erro com `ProblemDetails`
**Prioridade:** Should | **Estimativa:** M
**Origem:** RNF-04 (`REQUISITOS.md`)
**Contexto:** Cada endpoint hoje devolve `BadRequest(new { message = ... })` manualmente. Sem middleware central de tratamento de erro/validação.
**Critério de aceite:**
- [ ] Erros de validação e não tratados passam a usar o formato `ProblemDetails` (RFC 7807)
- [ ] Pelo menos um módulo (ex.: Tickets) migrado como prova de conceito
**Status:** To Do

---

### [DEBT-006] Paginação em endpoints `/list`
**Prioridade:** Should | **Estimativa:** M
**Origem:** RNF-06 (`REQUISITOS.md`)
**Contexto:** Nenhuma listagem (`cities/list`, `routes/list`, `prices/list`, `tickets/list`...) tem paginação.
**Critério de aceite:**
- [ ] Pelo menos `GET /tickets/list` aceita `page`/`pageSize` e retorna total de itens
- [ ] Teste cobre página vazia e página parcial
**Status:** To Do

---

### [FEAT-007] Cancelamento de passagem com devolução de vaga
**Prioridade:** Should | **Estimativa:** M
**Origem:** Seção 9 do `REQUISITOS.md` (estava fora do MVP; agora que a compra funciona ponta a ponta, é o próximo passo natural de produto)
**Contexto:** Hoje não existe `DELETE`/cancelamento de `Ticket`. Precisa devolver a vaga ao `Boarding` (`Seat += 1`) e decidir regra de prazo (ex.: só cancela até X horas antes do embarque).
**Critério de aceite:**
- [ ] `DELETE /tickets/{id}` cancela o ticket do próprio usuário autenticado
- [ ] Vaga é devolvida ao `Boarding` correspondente
- [ ] Teste cobre: cancelar ticket de outro usuário → 403/404; cancelar após o embarque já ter passado → regra a definir e testar
**Status:** To Do

---

### [DEBT-008] Convenção de rota consistente (REST puro)
**Prioridade:** Could | **Estimativa:** L
**Origem:** RNF-05 (`REQUISITOS.md`)
**Contexto:** Mistura de `/create`, `/list/{id}` com verbos HTTP que já expressam a ação. Mudança grande, quebra o frontend se feita sem coordenar — não é XS.
**Critério de aceite:**
- [ ] Convenção documentada em `ARCHITECTURE.md`
- [ ] Pelo menos um módulo migrado, frontend atualizado junto
**Status:** To Do

---

### [FEAT-009] Recuperação de senha
**Prioridade:** Could | **Estimativa:** M
**Origem:** RF-08 (`REQUISITOS.md`)
**Status:** To Do

---

### [CHORE-010] Projeto de testes xUnit + primeiro teste de regressão
**Prioridade:** Must | **Estimativa:** S
**Origem:** Gate de "Done" definido para o Kanban deste projeto — nenhum ticket fecha sem teste
**Contexto:** Hoje não existe nenhum projeto de teste automatizado. Este ticket é pré-requisito de todos os outros (não dá pra exigir teste de regressão em `BUG-001` sem isso existir primeiro).
**Critério de aceite:**
- [ ] Projeto `BusStation_API.Tests` criado (xUnit + `WebApplicationFactory`)
- [ ] Primeiro teste de integração real rodando contra o pipeline de endpoints (ex.: `POST /cities/create` feliz + conflito)
- [ ] `dotnet test` funcionando localmente
**Status:** To Do

---

### [CHORE-011] Pipeline de CI (GitHub Actions)
**Prioridade:** Should | **Estimativa:** S
**Origem:** Objetivo declarado do projeto (praticar CI/CD) — `ARCHITECTURE.md` §5
**Contexto:** Depende de `CHORE-010` existir primeiro (senão não há o que rodar).
**Critério de aceite:**
- [ ] Workflow roda `dotnet build` + `dotnet test` em todo PR
- [ ] Badge de status no README
**Status:** Blocked (depende de CHORE-010)

---

### [CHORE-012] Containerizar API e publicar em staging
**Prioridade:** Should | **Estimativa:** M
**Origem:** Objetivo declarado do projeto (praticar deploy) — `ARCHITECTURE.md` §5
**Critério de aceite:**
- [ ] `Dockerfile` funcional para a API
- [ ] Deploy manual bem-sucedido em Railway ou Fly.io (a decidir) com Postgres gerenciado
- [ ] Variáveis de ambiente/segredos fora do código
**Status:** Blocked (depende de decisão de hospedagem)
