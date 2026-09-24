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
- [ ] Remover o `Skip` de `CityTests.Create_with_an_acronym_already_in_use_returns_409` e ele passar; `Create_rejects_blank_fields_and_duplicate_name` continua verde
**Status:** To Do

---


### [BUG-015] Reajuste de preço/km quebra quando a distância tem mais de uma rota
**Prioridade:** Must | **Estimativa:** S
**Origem:** suíte E2E (`CHORE-010`)
**Contexto:** `PATCH /prices/update/{id}` devolve 500 quando a distância tem 2+ rotas cadastradas. Com 0 rotas devolve 404 e o reajuste não é salvo. Toca a decisão D-03 (preço congelado x recalculado) — alinhar com a liderança o comportamento esperado antes de corrigir.
**Critério de aceite:**
- [ ] Remover o `Skip` de `CatalogAdminTests.Updating_price_per_km_of_a_distance_with_two_routes_does_not_crash` e ele passar
- [ ] Comportamento com 0 rotas definido e coberto por teste
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

### [FEAT-025] [front] Telas de cadastro/login, compra e "minhas passagens"
**Prioridade:** Must | **Estimativa:** L
**Origem:** fechamento do staging v1 (`../docs/deploy/escopo-deploy.md` §7): o DoD 2 não fecha porque o front só tem a busca
**Contexto:** a API já suporta o fluxo inteiro (`/register`, `/login`, `/tickets/create`, `/tickets/list`) e foi validada no staging. Falta a UI. Token de usuário expira em 5 min (`JwtSettings:User:ExpirationTimeInMinutes`), então a UI precisa tratar 401 levando de volta ao login.
**Critério de aceite:**
- [ ] Cadastro e login, com token guardado e enviado como `Bearer`
- [ ] Botão de comprar numa saída da busca → `POST /tickets/create`, com feedback de sucesso e erro (sem vaga, não logado)
- [ ] Tela "minhas passagens" (`GET /tickets/list`)
- [ ] 401 em qualquer chamada autenticada → volta ao login com mensagem
- [ ] Testes Vitest cobrindo os fluxos felizes e o 401
**Status:** To Do

---

### [CHORE-026] Migrations no Pre-Deploy Command do Railway
**Prioridade:** Should | **Estimativa:** S
**Origem:** troca de Render para Railway (`../docs/deploy/escopo-deploy.md` §6)
**Contexto:** hoje o `efbundle` roda à mão da máquina do dev. O Railway tem Pre-Deploy Command, e isso tira o passo manual.
**Critério de aceite:**
- [ ] A imagem inclui o `efbundle` (gerado num stage do Dockerfile)
- [ ] Pre-Deploy Command configurado para rodar o bundle com a connection string **direta** (variável separada da pooled)
- [ ] Deploy sem migration nova continua passando (idempotência)
- [ ] README atualizado
**Status:** To Do

---

### [CHORE-021] Smoke test pós-deploy
**Prioridade:** Must | **Estimativa:** S
**Origem:** `../docs/deploy/escopo-deploy.md`, §4
**Critério de aceite:** ver o escopo do deploy (script contra `BASE_URL` cobrindo `/health/ready` → compra → `/tickets/list`, com timeout de 90s na 1ª request por causa do cold start).
**Status:** Blocked (depende de FEAT-025 e de embarque cadastrado pelo admin)

---

### [CHORE-020] CD automático para staging
**Prioridade:** Could | **Estimativa:** S
**Origem:** `../docs/deploy/escopo-deploy.md`, §4 (fase 2)
**Critério de aceite:** merge em `main` → migrations (CHORE-026) → deploy → smoke test (CHORE-021) automático; smoke vermelho deixa o pipeline vermelho.
**Status:** Blocked (depende de CHORE-021)

