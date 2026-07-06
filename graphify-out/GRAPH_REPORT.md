# Graph Report - PSR-MinimalAPI  (2026-07-06)

## Summary
- 100 nodes · 154 edges · 17 communities detected
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS

## God Nodes (most connected - your core abstractions)
1. `BearPizzeria.Api.Models` - 2 edges
2. `BearPizzeria.Api.Models.DTOs` - 2 edges
3. `Pedido` - 2 edges
4. `BearPizzeria.Api.Validators` - 2 edges
5. `BearPizzeria.Api.Models` - 2 edges
6. `BearPizzeria.Api.Validators` - 2 edges
7. `BearPizzeria.Api.Validators` - 2 edges
8. `BearPizzeria.Api.Services` - 2 edges
9. `BearPizzeria.Api.Validators` - 2 edges
10. `Pizza` - 2 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Communities

### Community 0 - "Entity (Community 0)"
Cohesion: 0,22
Nodes (18): SocketServerService.cs, SocketServerService.cs, BearPizzeria.Api.Services, ActualizarEstadoPedidoAsync(), SocketServerService(), RemoverCliente(), ProcesarPedidosChannelAsync(), while() (+10 more)

### Community 1 - "Entity (Community 1)"
Cohesion: 0,26
Nodes (17): Program.cs, Program.cs, Program.cs, Program.cs, if(), Program.cs, catch(), foreach() (+9 more)

### Community 3 - "Entity (Community 3)"
Cohesion: 0,60
Nodes (6): CrearPedidoRequestValidator.cs, CrearPedidoRequestValidator.cs, RuleForEach(), RuleFor(), CrearPedidoRequestValidator(), BearPizzeria.Api.Validators

### Community 2 - "Entity (Community 2)"
Cohesion: 0,60
Nodes (6): PedidoDbContext.cs, PedidoDbContext.cs, SeedData(), PedidoDbContext(), OnModelCreating(), BearPizzeria.Api.Data

### Community 4 - "Entity (Community 4)"
Cohesion: 0,60
Nodes (6): ActualizarEstadoRequestValidator.cs, ActualizarEstadoRequestValidator.cs, BeValidEstado(), RuleFor(), ActualizarEstadoRequestValidator(), BearPizzeria.Api.Validators

### Community 6 - "Entity (Community 6)"
Cohesion: 0,70
Nodes (5): ClienteValidator.cs, ClienteValidator.cs, ClienteValidator(), RuleFor(), BearPizzeria.Api.Validators

### Community 5 - "Entity (Community 5)"
Cohesion: 0,70
Nodes (5): PedidoItemRequestValidator.cs, PedidoItemRequestValidator.cs, RuleFor(), PedidoItemRequestValidator(), BearPizzeria.Api.Validators

### Community 10 - "Entity (Community 10)"
Cohesion: 0,83
Nodes (4): PedidoPizza.cs, PedidoPizza.cs, BearPizzeria.Api.Models, PedidoPizza

### Community 7 - "Entity (Community 7)"
Cohesion: 0,83
Nodes (4): ActualizarEstadoRequest.cs, ActualizarEstadoRequest.cs, BearPizzeria.Api.Models.DTOs, ActualizarEstadoRequest

### Community 9 - "Entity (Community 9)"
Cohesion: 0,83
Nodes (4): PedidoItemRequest.cs, PedidoItemRequest.cs, BearPizzeria.Api.Models.DTOs, PedidoItemRequest

### Community 8 - "Entity (Community 8)"
Cohesion: 0,83
Nodes (4): Pedido.cs, Pedido.cs, Pedido, BearPizzeria.Api.Models

### Community 12 - "Entity (Community 12)"
Cohesion: 0,83
Nodes (4): Cliente.cs, Cliente.cs, BearPizzeria.Api.Models, Cliente

### Community 13 - "Entity (Community 13)"
Cohesion: 0,83
Nodes (4): CrearPedidoRequest.cs, CrearPedidoRequest.cs, CrearPedidoRequest, BearPizzeria.Api.Models.DTOs

### Community 11 - "Entity (Community 11)"
Cohesion: 0,83
Nodes (4): Pizza.cs, Pizza.cs, Pizza, BearPizzeria.Api.Models

### Community 16 - "Entity (Community 16)"
Cohesion: 1,00
Nodes (3): main(), consumo.go, consumo.go

### Community 15 - "Entity (Community 15)"
Cohesion: 1,00
Nodes (3): TamanoPizza.cs, TamanoPizza.cs, BearPizzeria.Api.Models

### Community 14 - "Entity (Community 14)"
Cohesion: 1,00
Nodes (3): EstadoPedido.cs, EstadoPedido.cs, BearPizzeria.Api.Models

## Suggested Questions
_Not enough signal to generate questions. The graph has no ambiguous edges, no bridge nodes, and all communities are well-connected._

