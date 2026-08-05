# BusStation API — Documento de Requisitos

> Documento produzido no papel de "time de produto", a partir do estado real do código em 26/07/2026 (entidades, endpoints e migrations existentes). Objetivo: dar um norte para o que já existe, travar o escopo do MVP e listar as decisões que só o dono do produto (você) pode tomar.

---

## 1. Visão do Produto

Sistema de venda de passagens de uma viação rodoviária: cadastro de cidades, trechos (origem → destino), preços por km, rotas comerciais, horários de embarque (boardings) e compra de passagens por usuários autenticados.

**Problema que o produto resolve:** permitir que um passageiro descubra rotas disponíveis entre duas cidades, veja preço e horário, e compre uma passagem. Permitir que a operação (viação) cadastre cidades, trechos, preços e rotas.

---

## 2. Atores / Personas

| Ator | Descrição | Status no código |
|---|---|---|
| **Passageiro** | Usuário final que se cadastra, compra passagens, gerencia seu perfil | ✅ existe (`/register`, `/login`, `/user/me`, `/user/profile`) |
| **Operador/Admin** | Responsável por cadastrar cidades, origens, destinos, distâncias, preços, rotas e embarques | ⬜ **não existe como papel** — hoje qualquer requisição sem autenticação consegue fazer isso (ver RNF-01 e Decisão D-01) |

---

## 3. Escopo do MVP (v1)

Regra geral: **v1 = comprar uma passagem de A para B, em uma data, com um valor calculado automaticamente.** Tudo que não serve diretamente esse fluxo é backlog (seção 8).

| Módulo | No escopo do MVP? |
|---|---|
| Autenticação (registro/login) | ✅ Sim |
| Cadastro de cidades/origens/destinos/distâncias/preços/rotas | ✅ Sim (via admin, mesmo que hoje sem endpoint próprio de admin) |
| Embarques (Boarding = data/horário/vaga de uma rota) | ✅ Sim |
| Compra de passagem (Ticket) | ✅ Sim — **é o único fluxo core ainda não fechado** |
| Cancelamento/reembolso de passagem | ❌ Fora do MVP |
| Pagamento real (gateway) | ❌ Fora do MVP |
| Assentos nomeados / mapa de poltronas | ❌ Fora do MVP |
| Múltiplas viações/empresas | ❌ Fora do MVP |
| Notificação (e-mail/SMS) | ❌ Fora do MVP |

---

## 4. Modelo de Domínio (glossário)

```
City ──< Origin
City ──< Destination
(Origin, Destination) ──< Distance (km)  — direcional: Indi→Udia e Udia→Indi são registros diferentes
Distance ──< Price (preço por km, histórico — o último Id é o "vigente")
Distance ──< Route (rota comercial nomeada; preço é congelado no momento da criação)
Route ──< Boarding (uma saída concreta: data + hora + vagas)
Route ──< Ticket (compra de um usuário)
User ──< Ticket
```

**Ponto de atenção de domínio:** `Ticket` hoje referencia `RouteId` (não `BoardingId`). Isso significa que a passagem é vendida contra a *rota* (abstrata, sem data), e não contra um *embarque* (data/hora concretos). Ver Decisão **D-02** — isso precisa ser resolvido antes de destravar o fluxo de compra, é provavelmente a raiz da sensação de "sem rumo".

---

## 5. Requisitos Funcionais

Legenda de status: ✅ Implementado · 🟡 Parcial/com bug · ⬜ Não implementado
Prioridade (MoSCoW): **M**ust · **S**hould · **C**ould · **W**on't (agora)

### 5.1 Autenticação e Usuário
| ID | Requisito | Prioridade | Status |
|---|---|---|---|
| RF-01 | Visitante deve poder se registrar com nome, idade (≥18), e-mail e senha | M | ✅ |
| RF-02 | Sistema deve rejeitar registro com e-mail já existente | M | ✅ |
| RF-03 | Usuário deve poder logar com e-mail/senha e receber um JWT | M | ✅ |
| RF-04 | Usuário autenticado deve poder ver seu próprio perfil | M | ✅ |
| RF-05 | Usuário autenticado deve poder atualizar nome/e-mail/idade | S | ✅ |
| RF-06 | Usuário deve poder excluir a própria conta | C | ✅ (endpoint comentado) |
| RF-07 | Usuário deve poder trocar senha | S | ✅ |
| RF-08 | Recuperação de senha (esqueci minha senha) | C | ⬜ |

### 5.2 Cadastro de Malha (Cidades, Origens, Destinos, Distâncias, Preços)
| ID | Requisito | Prioridade | Status |
|---|---|---|---|
| RF-09 | Admin deve poder cadastrar cidade (nome, estado, sigla) | M | ✅ (sem restrição de quem pode chamar — ver D-01) |
| RF-10 | Sistema deve impedir cidade duplicada | M | 🟡 (regra de duplicidade compara campo errado — revisar) |
| RF-11 | Admin deve poder listar, editar e excluir cidades | M | 🟡 (edição/exclusão existem; exclusão bloqueia se cidade for origem/destino) |
| RF-12 | Admin deve poder marcar uma cidade como origem válida | M | ✅ |
| RF-13 | Admin deve poder marcar uma cidade como destino válido | M | ✅ |
| RF-14 | Admin deve poder cadastrar a distância (km) entre uma origem e um destino | M | ✅ |
| RF-15 | Admin deve poder cadastrar/atualizar o preço por km de uma distância | M | 🟡 (só criação existe; sem edição/listagem) |
| RF-16 | Sistema deve impedir distância com origem = destino | S | ✅ |

### 5.3 Rotas e Embarques
| ID | Requisito | Prioridade | Status |
|---|---|---|---|
| RF-17 | Admin deve poder criar uma rota comercial vinculada a uma distância | M | ✅ |
| RF-18 | Sistema deve calcular o preço da rota automaticamente (km × preço/km vigente) | M | ✅ |
| RF-19 | Passageiro deve poder listar rotas disponíveis | M | ✅ |
| RF-20 | Passageiro deve poder consultar detalhe de uma rota | M | 🟡 (endpoint existe mas não retorna resposta) |
| RF-21 | Admin deve poder atualizar o preço de uma rota | S | 🟡 (endpoint existe, lógica de recálculo está comentada) |
| RF-22 | Admin deve poder criar um embarque (data, horário e vagas) para uma rota | M | ✅ |
| RF-23 | Sistema deve impedir embarque com data no passado | M | ✅ |
| RF-24 | Passageiro deve poder listar embarques disponíveis de uma rota (com vagas > 0) | M | ⬜ |
| RF-25 | Admin deve poder cancelar/inativar uma rota ou embarque | C | ⬜ |

### 5.4 Passagens (Tickets)
| ID | Requisito | Prioridade | Status |
|---|---|---|---|
| RF-26 | Passageiro autenticado deve poder comprar passagem(ns) para um embarque | **M** | ⬜ **bloqueado por D-02** |
| RF-27 | Sistema deve verificar vagas disponíveis antes de confirmar a compra | M | ⬜ |
| RF-28 | Sistema deve debitar as vagas do embarque após compra | M | ⬜ |
| RF-29 | Sistema deve registrar o valor pago (`FarePaid`) no momento da compra | M | ⬜ |
| RF-30 | Passageiro deve poder listar suas próprias passagens compradas | M | ⬜ |
| RF-31 | Passageiro deve poder ver detalhe de uma passagem | S | ⬜ |

---

## 6. Regras de Negócio

| ID | Regra |
|---|---|
| RN-01 | Idade mínima para cadastro: 18 anos |
| RN-02 | Senha é armazenada com hash (BCrypt), nunca em texto puro |
| RN-03 | Não pode existir distância com origem igual ao destino |
| RN-04 | Não pode existir embarque com data anterior à data atual |
| RN-05 | O preço de uma rota é calculado no momento da criação (km × preço/km vigente naquele instante) — **a definir (D-03): esse preço fica congelado para sempre, ou é recalculado quando o preço/km muda?** |
| RN-06 | Uma cidade só pode ser excluída se não for origem nem destino de nenhum trecho |

---

## 7. Requisitos Não-Funcionais

| ID | Categoria | Requisito |
|---|---|---|
| RNF-01 | Segurança | Endpoints de escrita de cidades/origens/destinos/distâncias/preços/rotas/embarques devem exigir papel de administrador. **Hoje são públicos** — qualquer pessoa sem login cria cidade, preço e rota. Isto é uma lacuna de segurança, não só uma pendência funcional. |
| RNF-02 | Segurança | Senhas com hash (BCrypt) — atendido. Tokens JWT com expiração e validação de issuer/audience — atendido. |
| RNF-03 | Integridade de dados | Valores monetários (`Price`, `Route.Price`, `Ticket.FarePaid`) usam `float`. Para dinheiro, `decimal` é o tipo correto (evita erro de arredondamento binário). |
| RNF-04 | Consistência | Regras de validação estão duplicadas manualmente em cada endpoint (if-chains). Sem middleware central de validação/tratamento de erro — resposta de erro não tem formato padronizado (`ProblemDetails`, por exemplo). |
| RNF-05 | Usabilidade de API | Endpoints não seguem um padrão único de nomenclatura (`/cities/create` vs REST puro `POST /cities`). Definir convenção e aplicar em todos os módulos. |
| RNF-06 | Performance | Listagens (`/list`) não têm paginação. Aceitável agora (poucos dados via seed), mas listar sem limite não escala. |
| RNF-07 | Observabilidade | Nenhum log estruturado além do padrão do ASP.NET. Sem correlação de request/erro. |
| RNF-08 | Manutenibilidade | Toda a API está em `Program.cs` (minimal API, ~600 linhas). Nenhuma camada de serviço para os módulos de domínio (só Auth/Token têm serviço próprio). |
| RNF-09 | Portabilidade | Banco: PostgreSQL via EF Core — sem acoplamento a infra específica além da connection string. |
| RNF-10 | Privacidade (LGPD) | Dados pessoais armazenados: nome, idade, e-mail. Sem CPF/documento hoje. Se compra de passagem exigir documento do passageiro futuramente, revisar tratamento de dado sensível. |

---

## 8. Decisões em Aberto (precisam ser tomadas antes de continuar)

Estas são as perguntas que, respondidas, eliminam a sensação de "não ter rumo" — cada nova ideia que aparecer, teste primeiro contra a pergunta: "isso resolve uma dessas decisões, ou é escopo novo pro backlog (seção 9)?"

- **D-01 — Existe papel de Admin?**
  Hoje não há distinção de papel/role. Decidir: (a) criar `Role` no `User` e proteger os endpoints de cadastro de malha, ou (b) aceitar que por ora é um sistema "de balcão" sem admin separado (estudo). Isso define RNF-01.

- **D-02 — Passagem é vendida contra Rota ou contra Embarque?**
  `Ticket.RouteId` existe, mas `Boarding` é quem tem data/hora/vagas. Uma rota pode ter vários embarques (viagens em dias diferentes). Comprar "da rota" não diz *quando* o passageiro viaja. **Recomendação:** `Ticket` deveria referenciar `BoardingId`, não `RouteId`, e o débito de vagas (RF-27/28) deve ocorrer no `Boarding`, não na `Route` (que nem tem mais campo de vagas). Esta é provavelmente a decisão mais importante pendente — destrava RF-26 a RF-29.

- **D-03 — Preço da rota é congelado ou dinâmico?**
  Ao criar a rota, o preço é calculado uma vez e salvo. Se o preço/km mudar depois, a rota antiga mantém o valor antigo. É intencional (preço histórico) ou deveria recalcular a cada consulta/venda?

- **D-04 — Vagas são um contador simples ou assentos nomeados?**
  Hoje `Boarding.Seat` é só um número inteiro (capacidade). Não há seleção de poltrona. Confirmar que contador simples é suficiente para o escopo de estudo atual (recomendo manter simples — assento nomeado é complexidade de UI/back que não ensina nada novo de domínio agora).

- **D-05 — Uma distância por sentido ou bidirecional?**
  Hoje `Distance` é direcional (Indi→Udia e Udia→Indi são duas linhas). Isso é redundante quando km é sempre igual nos dois sentidos, mas permite preços diferentes por sentido no futuro. Confirmar se mantém direcional (mais flexível) ou simplifica para não-direcional (menos dado duplicado).

---

## 9. Fora de Escopo agora (backlog — não implementar até v1 estar fechado)

- Cancelamento e reembolso de passagem
- Integração com gateway de pagamento real
- Múltiplas empresas/viações na mesma base
- Mapa de assentos nomeados
- Notificação por e-mail/SMS de compra
- Recuperação de senha / troca de senha
- Papéis granulares (ex.: atendente vs. gerente) além de Admin/Passageiro
- Relatórios/BI para a operação

Regra prática: toda ideia nova que surgir enquanto você codifica, escreva aqui embaixo com uma linha, e volte para o RF que estava fazendo. Não implemente na hora.

---

## 10. Achados Técnicos no Código Atual (observação, não é requisito)

Notas de leitura do código — não corrigidas aqui de propósito, para você mesmo decidir e corrigir como parte do aprendizado:

1. `cities.MapPost("/create")`: a checagem de duplicidade compara `c.State == request.Acronym` — parece trocado com `c.Acronym == request.Acronym`.
2. `routes.MapGet("/list/{id:int}")`: monta a query mas nunca retorna `Results.Ok(...)` — o método não compila um retorno em todos os caminhos ou fica implícito.
3. `routes.MapPatch("/update/{id:int}")`: a lógica de recálculo de preço está toda comentada; hoje o endpoint só faz `SaveChangesAsync` sem alterar nada.
4. `Entities/Route.cs` tem `TicketId` (int solto, sem uso aparente — sobra de uma modelagem anterior?) e `IsActive` (não é lida/escrita em nenhum endpoint).
5. O bloco `tickets.MapPost("/create", ...)` está inteiro comentado — referencia `route.Seat`, campo que não existe mais em `Route` (foi para `Boarding`). Confirma o D-02 acima: essa parte do código ficou "presa" exatamente na decisão de domínio não resolvida.

---

## 11. Próximo Passo Sugerido

1. Resolver D-01 a D-05 nesta seção (pode ser só marcando a resposta ao lado de cada uma, direto neste arquivo).
2. Fechar RF-26 a RF-31 (módulo de Tickets) — é o único fluxo core que falta para o MVP fechar ponta a ponta.
3. Só depois disso, considerar qualquer item da seção 9.
