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
**Origem:** RNF-04 (`REQUISITOS.md`). Reforçado pelo FEAT-025: o front detecta "sem vaga" com regex sobre a mensagem em inglês `"Dont have seats"`, e um `type` estável evitaria isso.
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

### [FEAT-031] `TicketResponse` com cidade de origem e destino
**Prioridade:** Could | **Estimativa:** XS
**Origem:** revisão do contrato no FEAT-025 (agente do front)
**Contexto:** "Minhas passagens" só consegue mostrar `routeName`, que é texto livre do admin. Com origem e destino a tela não depende de o admin dar bons nomes às rotas.
**Critério de aceite:**
- [ ] `TicketResponse` inclui `originCity` e `destinationCity` (campos novos, sem quebrar os atuais)
- [ ] Teste E2E cobre os campos
**Status:** To Do

---

### [DEBT-028] Limpeza de refresh tokens expirados/revogados
**Prioridade:** Could | **Estimativa:** S
**Origem:** `ARCHITECTURE.md` §7 (refresh token, `FEAT-027`)
**Contexto:** cada login e cada refresh gravam uma linha em `RefreshTokens`, e nada apaga as antigas.
**Critério de aceite:**
- [ ] Rotina (hosted service periódico ou job) apaga linhas expiradas ou revogadas há mais de N dias
- [ ] Teste cobre que tokens ativos não são apagados
**Status:** To Do

---

### [FEAT-029] [front] Logout propagado entre abas
**Prioridade:** Could | **Estimativa:** XS
**Origem:** revisão do FEAT-025 (sessão no front)
**Contexto:** logout numa aba não derruba as outras na hora; elas só caem no próximo 401, quando o refresh falha.
**Critério de aceite:**
- [ ] Ouvir o evento `storage`: remoção do refresh token em outra aba encerra a sessão nesta
- [ ] Teste com dois providers compartilhando o storage
**Status:** To Do

---

### [CHORE-033] [front] Smoke de UI versionado (Playwright)
**Prioridade:** Should | **Estimativa:** M
**Origem:** fechamento do CHORE-021. O E2E de UI que validou o staging roda com scripts CDP fora do repo.
**Critério de aceite:**
- [ ] Playwright no repo do front, com o fluxo buscar → comprar (deslogado → cadastro → volta) → minhas passagens
- [ ] Roda contra uma `BASE_URL` (staging) e sobe o próprio browser (sem porta de depuração fixa)
- [ ] Documentado no README do front
**Status:** To Do

---

### [CHORE-020] CD automático para staging
**Prioridade:** Could | **Estimativa:** S
**Origem:** `../docs/deploy/escopo-deploy.md`, §4 (fase 2)
**Critério de aceite:** merge em `main` → migrations (pre-deploy do Railway, já ativo) → deploy → smoke de API e de UI (CHORE-033) automáticos; smoke vermelho deixa o pipeline vermelho.
**Status:** Blocked (depende de CHORE-033)

