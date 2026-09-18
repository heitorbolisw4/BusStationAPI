# Changelog

> Uma linha por entrega, com hash do commit. Histórico completo e detalhado fica no `git log` — aqui é só o "o que foi entregue", não o "como".

## 2026-09-18

- **[DONE] Fechado o módulo de Tickets (RF-26–RF-31) e resolvida a decisão D-02** — `Ticket` passou a referenciar `Boarding` em vez de `Route` (débito de vaga agora ocorre em `Boarding.Seat`); adicionados `GET /tickets/list` e `GET /tickets/list/{id}`, escopados ao usuário autenticado. Corrigidos 3 bugs do refactor em andamento: claim JWT errada (todo login autenticado recebia 401 ao comprar), `Ticket.UserId` nunca setado, débito de vaga que tinha sumido. `86596d3`
- **[DONE] Extraído o módulo de Prices** (`Create`/`Update`/`List`) de `Program.cs` para `Endpoints/PriceEndpoints.cs`; corrigida validação com lógica invertida que impedia o arquivo de compilar. `86596d3`
- **[DONE] Documentação do projeto reorganizada** — `docs/BACKLOG.md` (fila de trabalho MoSCoW) e `docs/ARCHITECTURE.md` (visão técnica + dívida técnica atualizada) criados; `REQUISITOS.md` passa a ser só o PRD (visão, RF/RN/RNF, decisões de produto).

## Histórico anterior (resumo — ver `git log` para o detalhe)

- Extraídos os módulos de City, Route, Distance, Boarding, Auth e User de `Program.cs` para `Endpoints/*.cs` dedicados (`c0f848e`, `dd54b2b`, `69e3199`, `4c6527c`, `b8508ff`)
- `GET /cities/list` liberado para anônimo; adicionado `GET /boardings/search` com DTO próprio de tela (`4ac3744`)
