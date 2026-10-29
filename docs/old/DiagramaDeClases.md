# Diagrama de Clases — BearPizzeria (Versión Rama: ProyectoPizzeria)

> [!NOTE]
> Este documento representa la **estructura estática inicial** del sistema correspondiente a la rama **`ProyectoPizzeria`** (modelo CRUD previo a la incorporación de autenticación JWT y carrito de compras).
> Ha sido normalizado y adaptado respetando rigurosamente las pautas pedagógicas del apunte **`Desarrollo de Sistemas - 6.º Año · Diagrama de Clases UML`**:
> 1. Derivación del modelo desde los Casos de Uso hacia las clases estructurales.
> 2. Representación de los tres compartimentos por clase (*Nombre*, *Atributos con tipo y visibilidad privada `-`*, *Métodos / Responsabilidades públicas `+`*).
> 3. Eliminación de claves foráneas como atributos primitivos (representadas exclusivamente mediante relaciones).
> 4. Justificación de la clase asociativa `PedidoPizza`.
> 5. Especificación formal de multiplicidades (`1`, `0..*`, `1..*`) y relaciones de composición (`*--`).
> 6. Separación estricta entre el **Modelo de Clases del Dominio** y los artefactos de infraestructura/base de datos.

---

## 1. Del Caso de Uso a las Clases del Dominio

Siguiendo el análisis planteado en el apunte:
* **Un Caso de Uso no se convierte en una clase ni automáticamente en un método único**: el caso de uso representa un objetivo completo del actor que requiere la colaboración de múltiples elementos estructurales.
* **No todo sustantivo es una clase**: se identifican las entidades representativas del negocio sobre las cuales el sistema necesita conservar datos y asignar responsabilidades.

### Modelo inicial de clases y responsabilidades (Alcance `ProyectoPizzeria`)

| Clase / Enumeración | Información que conserva (Atributos `-`) | Responsabilidades / Operaciones (`+`) |
| :--- | :--- | :--- |
| **`Cliente`** | `- id: int`<br/>`- usuario: string`<br/>`- nombre: string`<br/>`- direccion: string`<br/>`- telefono: string`<br/>`- email: string` | `+ registrarse(): void`<br/>`+ actualizarDatos(): void`<br/>`+ consultarPedidos(): List` |
| **`Pizza`** | `- id: int`<br/>`- nombre: string`<br/>`- descripcion: string`<br/>`- precio: decimal`<br/>`- tamano: TamanoPizza` | `+ mostrarInformacion(): string`<br/>`+ calcularPrecio(): decimal` |
| **`Pedido`** | `- id: int`<br/>`- fechaPedido: DateTime`<br/>`- estado: EstadoPedido`<br/>`- total: decimal` | `+ agregarItem(pizza: Pizza, cantidad: int): void`<br/>`+ calcularTotal(): decimal`<br/>`+ avanzarEstado(): void`<br/>`+ cancelar(): void` |
| **`PedidoPizza`** | `- cantidad: int`<br/>`- precioUnitario: decimal` | `+ calcularSubtotal(): decimal` |
| **`TamanoPizza`** | `<<enumeration>>`<br/>`Personal`, `Mediana`, `Grande`, `Familiar` | Define las variantes físicas de porción ofrecidas por cada pizza en el catálogo base. |
| **`EstadoPedido`** | `<<enumeration>>`<br/>`EnPreparacion`, `EnViaje`, `Entregado` | Representa el ciclo logístico del pedido a través de Cocina y Reparto. |

---

## 2. ¿Por qué `PedidoPizza` es una clase?

*(Aplicación directa de la Sección 8 del apunte didáctico)*

Relacionar directamente `Pedido` con `Pizza` mediante una simple línea asociativa impediría conservar información propia que surge únicamente en el instante en que una pizza forma parte de una orden:
* **Cantidad** solicitada de esa pizza.
* **Precio unitario histórico** (congelado al momento de emitir el pedido, para que futuros cambios de precio en el catálogo no alteren pedidos ya facturados).
* **Subtotal** de esa línea de compra.

Al poseer atributos y comportamiento propio (`calcularSubtotal()`), `PedidoPizza` deja de ser una simple línea de asociación y pasa a ser modelada como un **elemento de primera clase en el dominio** (clase asociativa / detalle).

---

## 3. Diagrama de Clases UML del Dominio

```mermaid
classDiagram
    class Cliente {
        -int id
        -string usuario
        -string nombre
        -string direccion
        -string telefono
        -string email
        +registrarse() void
        +actualizarDatos() void
        +consultarPedidos() List
    }

    class Pizza {
        -int id
        -string nombre
        -string descripcion
        -decimal precio
        -TamanoPizza tamano
        +mostrarInformacion() string
        +calcularPrecio() decimal
    }

    class TamanoPizza {
        <<enumeration>>
        Personal
        Mediana
        Grande
        Familiar
    }

    class Pedido {
        -int id
        -DateTime fechaPedido
        -EstadoPedido estado
        -decimal total
        +agregarItem(pizza: Pizza, cantidad: int) void
        +calcularTotal() decimal
        +avanzarEstado() void
        +cancelar() void
    }

    class EstadoPedido {
        <<enumeration>>
        EnPreparacion
        EnViaje
        Entregado
    }

    class PedidoPizza {
        -int cantidad
        -decimal precioUnitario
        +calcularSubtotal() decimal
    }

    Cliente "1" -- "0..*" Pedido : realiza
    Pedido "1" *-- "1..*" PedidoPizza : se compone de
    Pizza "1" -- "0..*" PedidoPizza : forma parte de
    Pedido "1" --> "1" EstadoPedido : estado actual
    Pizza "1" --> "1" TamanoPizza : tamaño base
```

### Justificación de Relaciones y Multiplicidades:
* **`Cliente (1) -- Pedido (0..*)`**: Un cliente puede haber realizado ninguno o muchos pedidos a lo largo del tiempo; todo pedido pertenece exactamente a un único cliente.
* **`Pedido (1) *-- PedidoPizza (1..*)`**: Relación de **composición estricta** (`*--`). Un pedido se compone obligatoriamente de al menos una línea de detalle de pizza. Las instancias de `PedidoPizza` no poseen existencia independiente: si se destruye o cancela el pedido cabecera, se destruyen sus líneas de detalle.
* **`Pizza (1) -- PedidoPizza (0..*)`**: Cada línea de detalle referencia a una pizza específica del catálogo; una pizza puede estar presente en ninguno o muchos pedidos.
* **Dependencias de tipos enumerados (`--> 1`)**: `Pedido` y `Pizza` utilizan `EstadoPedido` y `TamanoPizza` como valores acotados de dominio.

---

## 4. Arquitectura Distribuida del Sistema

*(Capa de subsistemas y comunicación técnica de la rama `ProyectoPizzeria`)*

```mermaid
flowchart LR
    CH["Cliente Hambriento<br/>(App Consola / Cliente HTTP)"]
    BK["Backend API<br/>(BearPizzeria.Api Minimal API)"]
    CO["Cocina Automatizada<br/>(BearPizzeria.Kitchen)"]
    RE["Reparto Logístico<br/>(BearPizzeria.Delivery)"]
    DB[("Base de Datos<br/>(MySQL)")]

    CH -- "HTTP REST (/api/pedidos)" --> BK
    BK -- "Sockets TCP (Puerto 5050)" --> CO
    BK -- "Sockets TCP (Puerto 5050)" --> RE
    BK -- "Mapeo Objeto-Relacional" --> DB

    style CH fill:#e1f5fe,stroke:#0288d1
    style BK fill:#ede7f6,stroke:#512da8
    style CO fill:#e8f8f5,stroke:#117a65
    style RE fill:#fbeee6,stroke:#b9770e
    style DB fill:#fef9e7,stroke:#b7950b
```

---

## 5. Diagrama de Secuencia — Flujo de Procesamiento de Pedido

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente (Actor)
    participant API as Backend (BearPizzeria.Api)
    participant DB as MySQL (Persistencia)
    participant K as Cocina (Worker TCP)
    participant D as Reparto (Worker TCP)

    C->>API: POST /api/pedidos { clienteId, items }
    API->>DB: INSERT Pedido + PedidoPizzas (Estado: EnPreparacion)
    DB-->>API: Confirmación (Id generado)
    API->>K: Notificación Socket TCP: NuevoPedido
    API-->>C: 201 Created (PedidoResponse)
    
    Note over K: Elaboración en cocina
    K->>API: PATCH /api/pedidos/{id}/estado (EnViaje)
    API->>DB: UPDATE Pedido SET Estado = EnViaje
    API->>D: Notificación Socket TCP: PedidoEnViaje
    
    Note over D: Traslado a domicilio
    D->>API: PATCH /api/pedidos/{id}/estado (Entregado)
    API->>DB: UPDATE Pedido SET Estado = Entregado
    
    C->>API: GET /api/pedidos/{id}
    API-->>C: 200 OK (Estado: Entregado)
```
