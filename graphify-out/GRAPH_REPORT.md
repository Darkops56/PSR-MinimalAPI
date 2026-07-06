# Graph Report - PSR-MinimalAPI  (2026-07-06)

## Summary
- 31 nodes · 47 edges · 7 communities detected
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS

## God Nodes (most connected - your core abstractions)
1. `TodoPSR` - 2 edges
2. `Todo` - 2 edges
3. `TodoPSR` - 2 edges
4. `IADO` - 2 edges
5. `TodoPSR` - 2 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Communities

### Community 0 - "Entity (Community 0)"
Cohesion: 0,25
Nodes (8): AdoDapper.cs, ObtenerTodosFinalizadosAsync(), last_insert_id(), VALUE(), EliminarTodoAsync(), AgregarTodoAsync(), TodoPSR, ActualizarTodoAsync()

### Community 1 - "Entity (Community 1)"
Cohesion: 0,40
Nodes (5): IADO.cs, TodoPSR, EliminarTodoAsync(), ObtenerTodosAsync(), AgregarTodoAsync()

### Community 3 - "Entity (Community 3)"
Cohesion: 0,83
Nodes (4): Todo.cs, Todo.cs, Todo, TodoPSR

### Community 2 - "Entity (Community 2)"
Cohesion: 0,50
Nodes (4): AdoDapper.cs, ObtenerTodosAsync(), NotImplementedException(), AdoDapper()

### Community 4 - "Entity (Community 4)"
Cohesion: 0,50
Nodes (4): IADO.cs, ObtenerTodosFinalizadosAsync(), ActualizarTodoAsync(), IADO

### Community 5 - "Entity (Community 5)"
Cohesion: 1,00
Nodes (3): Program.cs, if(), Program.cs

### Community 6 - "Entity (Community 6)"
Cohesion: 1,00
Nodes (3): consumo.go, main(), consumo.go

## Suggested Questions
_Not enough signal to generate questions. The graph has no ambiguous edges, no bridge nodes, and all communities are well-connected._

