# Diagrama de Clases — BearPizzeria

## Diagrama UML

```mermaid
classDiagram
    class Cliente {
        +int Id
        +string Nombre
        +string Direccion
        +string Telefono
        +string Email
    }

    class Pizza {
        +int Id
        +string Nombre
        +string Descripcion
        +decimal Precio
        +TamanoPizza Tamano
    }

    class TamanoPizza {
        <<enumeration>>
        Personal
        Mediana
        Grande
        Familiar
    }

    class Pedido {
        +int Id
        +int ClienteId
        +DateTime FechaPedido
        +EstadoPedido Estado
        +decimal Total
    }

    class EstadoPedido {
        <<enumeration>>
        EsperaDeConfirmacion
        EnPreparacion
        EnViaje
        Entregado
    }

    class PedidoPizza {
        +int PedidoId
        +int PizzaId
        +int Cantidad
        +decimal PrecioUnitario
    }

    class PedidoDbContext {
        +DbSet~Cliente~ Clientes
        +DbSet~Pizza~ Pizzas
        +DbSet~Pedido~ Pedidos
        +DbSet~PedidoPizza~ PedidoPizzas
        +OnModelCreating()
    }

    class SocketServerService {
        -ConcurrentDictionary~string, TcpClient~ _kitchenClients
        -ConcurrentDictionary~string, TcpClient~ _deliveryClients
        -Channel~Pedido~ _pedidoChannel
        +ExecuteAsync()
        +NotificarNuevoPedidoAsync(Pedido)
        -AcceptClientsAsync()
        -HandleClientAsync()
        -RemoverCliente()
        -ProcesarMensajeAsync()
        -ProcesarPedidosChannelAsync()
        -EnviarADeliveryAsync()
        -EnviarATodosAsync()
    }

    class CrearPedidoRequest {
        +int ClienteId
        +List~PedidoItemRequest~ Items
    }

    class PedidoItemRequest {
        +int PizzaId
        +int Cantidad
    }

    class ActualizarEstadoRequest {
        +string Estado
    }

    Cliente "1" --> "*" Pedido
    Pedido "1" --> "*" PedidoPizza
    Pizza "1" --> "*" PedidoPizza
    Pedido "1" --> "1" EstadoPedido
    Pizza "1" --> "1" TamanoPizza
    PedidoDbContext --> Cliente
    PedidoDbContext --> Pizza
    PedidoDbContext --> Pedido
    PedidoDbContext --> PedidoPizza
    Program --> PedidoDbContext
    Program --> SocketServerService
    Program --> CrearPedidoRequest
    CrearPedidoRequest "1" --> "*" PedidoItemRequest
```

## Diagrama de Arquitectura Distribuida

```mermaid
flowchart LR
    CH[Cliente Hambriento<br/>App Consola C#]
    BK[Backend<br/>BearPizzeria.Api<br/>Minimal API + EFCore]
    CO[Cocina Automatizada<br/>BearPizzeria.Kitchen<br/>App Consola C#]
    RE[Reparto<br/>BearPizzeria.Delivery<br/>App Consola C#]
    DB[(MySQL<br/>BearPizzeria)]

    CH -- HTTP REST --> BK
    BK -- TCP Sockets --> CO
    BK -- TCP Sockets --> RE
    BK -- EFCore --> DB

    style CH fill:#f9f,stroke:#333
    style BK fill:#bbf,stroke:#333
    style CO fill:#bfb,stroke:#333
    style RE fill:#fbb,stroke:#333
    style DB fill:#ff9,stroke:#333
```

## Diagrama de Secuencia — Flujo Completo de un Pedido

```mermaid
sequenceDiagram
    participant C as Cliente
    participant API as Backend (API)
    participant DB as MySQL
    participant K as Cocina
    participant D as Reparto

    C->>API: POST /api/pedidos
    API->>DB: INSERT Pedido (EsperaDeConfirmacion)
    DB-->>API: Pedido creado
    API->>K: TCP: NuevoPedido {Id, items}
    API-->>C: 201 Created
    Note over K: Simula preparación
    K->>API: TCP: ActualizarEstado {Id, EnPreparacion}
    API->>DB: UPDATE Pedido SET Estado = EnPreparacion
    Note over K: Simula fin cocina
    K->>API: TCP: ActualizarEstado {Id, EnViaje}
    API->>DB: UPDATE Pedido SET Estado = EnViaje
    API->>D: TCP: PedidoEnViaje {Id, direccion}
    Note over D: Simula entrega
    D->>API: TCP: ActualizarEstado {Id, Entregado}
    API->>DB: UPDATE Pedido SET Estado = Entregado
    C->>API: GET /api/pedidos/{id}
    API-->>C: { Estado: Entregado }
```
