# Plan de Ejecución — BearPizzeria

> Documento que detalla las fases de implementación del Sistema Distribuido de Pizzería Digital, desde la documentación base hasta la estabilización y demo funcional.
>
> **Estado: COMPLETADO** ✅

---

## Resumen del Proyecto

Migrar la base `TodoPSR` (Minimal API + Dapper) hacia un ecosistema distribuido, asincrónico y desacoplado para una pizzería automatizada.

**Solución única:** `BearPizzeria.sln`

**Stack tecnológico:**
- .NET 10 — Minimal API + EFCore + Sockets TCP
- MySQL + MySql.EntityFrameworkCore
- Apps de consola C# (Cliente, Cocina, Reparto)

**Actores del sistema:**
1. Cliente Hambriento (App C#)
2. Backend (Minimal API)
3. Cocina Automatizada (App C#)
4. Reparto (App C#)

---

## Fase 0 — Documentación Base

**Objetivo:** Crear la documentación inicial del proyecto y adecuar el esquema de base de datos.

| # | Tarea | Archivos |
|---|-------|----------|
| 0.1 | Analizar el dominio: identificar entidades (`Cliente`, `Pizza`, `Pedido`), atributos y enum `EstadoPedido`. | — |
| 0.2 | Crear `docs/CasosDeUso.md` con diagramas Mermaid y especificación de los 13 casos de uso. | `docs/CasosDeUso.md` |
| 0.3 | Crear `docs/Der.md` con diagrama entidad-relación en Mermaid. | `docs/Der.md` |
| 0.4 | Crear `docs/DiagramaDeClases.md` con diagrama de clases UML, arquitectura y secuencia en Mermaid. | `docs/DiagramaDeClases.md` |
| 0.5 | Actualizar `script.sql` con el nuevo esquema de pizzería (Clientes, Pizzas, Pedidos, PedidoPizzas + datos iniciales). | `script.sql` |
| 0.6 | Actualizar `README.md` con la nueva arquitectura y pasos de instalación. | `README.md` |

**🎯 Entregable:** `docs/` completa con documentación técnica y base de datos actualizada.

---

## Fase 1 — Migración Dapper → EFCore + Modelos de Dominio

**Objetivo:** Reemplazar Dapper por Entity Framework Core con MySQL, crear las entidades del dominio pizzeria y el DbContext.

| # | Tarea | Archivos |
|---|-------|----------|
| 1.1 | Actualizar `TodoPSR.csproj` → `BearPizzeria.Api.csproj`: eliminar `Dapper` y `MySqlConnector`, agregar `Pomelo.EntityFrameworkCore.MySql`. | `src/BearPizzeria.Api/BearPizzeria.Api.csproj` |
| 1.2 | Crear `Models/Cliente.cs` (Id, Nombre, Direccion, Telefono, Email). Sin contraseñas. | `src/BearPizzeria.Api/Models/Cliente.cs` |
| 1.3 | Crear `Models/Pizza.cs` (Id, Nombre, Descripcion, Precio, Tamano). | `src/BearPizzeria.Api/Models/Pizza.cs` |
| 1.4 | Crear `Models/Pedido.cs` (Id, ClienteId, FechaPedido, Estado —enum—, Total). | `src/BearPizzeria.Api/Models/Pedido.cs` |
| 1.5 | Crear `Models/PedidoPizza.cs` (PedidoId, PizzaId, Cantidad, PrecioUnitario). | `src/BearPizzeria.Api/Models/PedidoPizza.cs` |
| 1.6 | Crear `Models/EstadoPedido.cs` — enum con los 4 estados del ciclo de vida. | `src/BearPizzeria.Api/Models/EstadoPedido.cs` |
| 1.7 | Crear `Models/TamanoPizza.cs` — enum con los tamaños disponibles. | `src/BearPizzeria.Api/Models/TamanoPizza.cs` |
| 1.8 | Crear `Data/PedidoDbContext.cs` con DbContext y Fluent API para relaciones y configuraciones. | `src/BearPizzeria.Api/Data/PedidoDbContext.cs` |
| 1.9 | Eliminar archivos legacy: `Todo.cs`, `IADO.cs`, `AdoDapper.cs`. | — |
| 1.10 | Configurar DI en `Program.cs`: registrar `PedidoDbContext` con `UseMySql` y connection string. Eliminar registros de `IDbConnection` e `IADO`. | `src/BearPizzeria.Api/Program.cs` |
| 1.11 | Agregar/actualizar `appsettings.json` con connection string a MySQL. | `src/BearPizzeria.Api/appsettings.json` |

**🎯 Entregable:** Backend con EFCore funcional, entidades de dominio creadas y compilación exitosa.

---

## Fase 2 — Endpoints REST de la Pizzería

**Objetivo:** Exponer los endpoints del negocio y eliminar los endpoints legacy de Todo.

| # | Tarea | Archivos |
|---|-------|----------|
| 2.1 | Endpoints de Pizzas: `GET /api/pizzas`, `GET /api/pizzas/{id}`. | `src/BearPizzeria.Api/Program.cs` |
| 2.2 | Endpoint `POST /api/pedidos`: recibe pedido con lista de pizzas y cantidades, crea en estado `EsperaDeConfirmacion`. | `src/BearPizzeria.Api/Program.cs` |
| 2.3 | Endpoint `GET /api/pedidos/{id}`: consulta estado y detalle del pedido. | `src/BearPizzeria.Api/Program.cs` |
| 2.4 | Endpoint `PATCH /api/pedidos/{id}/estado`: actualiza estado (uso interno). | `src/BearPizzeria.Api/Program.cs` |
| 2.5 | Endpoint `POST /api/clientes`: registrar cliente. | `src/BearPizzeria.Api/Program.cs` |
| 2.6 | Eliminar rutas legacy `/todoitems` de `Program.cs`. | `src/BearPizzeria.Api/Program.cs` |
| 2.7 | Verificar compilación con `dotnet build`. | — |

**🎯 Entregable:** Backend REST funcional con todos los endpoints del negocio.

---

## Fase 3 — Red Distribuida con Sockets TCP

**Objetivo:** Implementar el servidor de sockets en el Backend y las aplicaciones de Cocina y Reparto.

| # | Tarea | Archivos |
|---|-------|----------|
| 3.1 | Crear `Services/SocketServerService.cs`: `BackgroundService` con `TcpListener` en puerto 5050. Acepta conexiones de Cocina y Reparto, las identifica y transmite mensajes JSON delimitados por `\n`. | `src/BearPizzeria.Api/Services/SocketServerService.cs` |
| 3.2 | Registrar `SocketServerService` como `AddHostedService` en `Program.cs`. | `src/BearPizzeria.Api/Program.cs` |
| 3.3 | Crear carpeta `src/` y proyectos de consola. | — |
| 3.4 | Crear `src/BearPizzeria.Kitchen/`: app de consola que se conecta por TCP, recibe pedidos, simula preparación y notifica cambios de estado. | `src/BearPizzeria.Kitchen/Program.cs`, `.csproj` |
| 3.5 | Crear `src/BearPizzeria.Delivery/`: app de consola que se conecta por TCP, escucha pedidos "En viaje", simula entrega y notifica "Entregado". | `src/BearPizzeria.Delivery/Program.cs`, `.csproj` |
| 3.6 | Crear solución `BearPizzeria.sln` que incluya los 4 proyectos (API, Kitchen, Delivery, Client). | `BearPizzeria.sln` |

**Protocolo de mensajería TCP:**
```
{
  "TipoMensaje": "NuevoPedido" | "ActualizarEstado",
  "PedidoId": int,
  "Estado": "EsperaDeConfirmacion" | "EnPreparacion" | "EnViaje" | "Entregado",
  "Detalle": { ... }
}
```

**🎯 Entregable:** Red distribuida funcional con Backend, Cocina y Reparto comunicándose por sockets TCP.

---

## Fase 4 — Módulo Cliente C#

**Objetivo:** Crear la app de consola que actúa como el *Cliente Hambriento*.

| # | Tarea | Archivos |
|---|-------|----------|
| 4.1 | Crear `src/BearPizzeria.Client/`: app de consola C# con menú interactivo. | `src/BearPizzeria.Client/Program.cs`, `.csproj` |
| 4.2 | Implementar consumo REST con `HttpClient`: `POST /api/pedidos`, `GET /api/pedidos/{id}`, `GET /api/pizzas`, `POST /api/clientes`. | `src/BearPizzeria.Client/Program.cs` |
| 4.3 | Manejo de errores: try-catch en llamadas HTTP, timeouts, reintentos con backoff. | `src/BearPizzeria.Client/Program.cs` |

**🎯 Entregable:** Módulo Cliente integrado y funcional.

---

## Fase 5 — Resiliencia y Robustez

**Objetivo:** Blindar el sistema distribuido contra fallos de red y datos inválidos.

| # | Tarea | Archivos |
|---|-------|----------|
| 5.1 | Blindar servidor de sockets: capturar `SocketException` en `SocketServerService` para evitar que una desconexión abrupta derribe el backend. Logging de desconexiones. | `src/BearPizzeria.Api/Services/SocketServerService.cs` |
| 5.2 | Blindar módulos Cocina/Reparto: reintentar conexión al backend con backoff exponencial si el socket se cae. | `src/BearPizzeria.Kitchen/Program.cs`, `src/BearPizzeria.Delivery/Program.cs` |
| 5.3 | Validación de payloads en endpoints REST: validar pizzas existentes, cliente existente, cantidades > 0. | `src/BearPizzeria.Api/Program.cs` |
| 5.4 | Agregar logging estructurado con `ILogger` en servicios y endpoints. | Varios |

**🎯 Entregable:** Arquitectura protegida contra interrupciones de red.

---

## Fase 6 — Estabilización y Demo

**Objetivo:** Verificar el sistema extremo a extremo y preparar la defensa oral.

| # | Tarea | Archivos |
|---|-------|----------|
| 6.1 | Prueba de flujo completo: Cliente → POST pedido → Cocina recibe → Reparto recibe → Entregado. | — |
| 6.2 | Inyección de fallos: probar caída de Cocina a medio proceso y verificar que Backend sigue operativo. | — |
| 6.3 | Actualizar documentación en `docs/` con ajustes surgidos durante implementación. | `docs/CasosDeUso.md`, `Der.md`, `DiagramaDeClases.md` |
| 6.4 | Preparar guion de defensa oral con focos en: async/await, sockets TCP, EFCore vs Dapper, arquitectura distribuida, manejo de fallos. | — |

**🎯 Entregable:** Repositorio finalizado, sistema distribuido estable, documentación actualizada y guion técnico para demo.

---

## Estructura de Directorios Final

```
PSR-MinimalAPI/
├── BearPizzeria.sln
├── docs/
│   ├── PlanDeTrabajo.md
│   ├── TrabajoPlanDeEjecucion.md
│   ├── CasosDeUso.md
│   ├── Der.md
│   └── DiagramaDeClases.md
├── src/
│   ├── BearPizzeria.Api/
│   │   ├── Models/
│   │   │   ├── Cliente.cs
│   │   │   ├── Pizza.cs
│   │   │   ├── Pedido.cs
│   │   │   ├── PedidoPizza.cs
│   │   │   ├── EstadoPedido.cs
│   │   │   └── TamanoPizza.cs
│   │   ├── Data/
│   │   │   └── PedidoDbContext.cs
│   │   ├── Services/
│   │   │   └── SocketServerService.cs
│   │   ├── Program.cs
│   │   ├── BearPizzeria.Api.csproj
│   │   └── appsettings.json
│   ├── BearPizzeria.Kitchen/
│   │   ├── Program.cs
│   │   └── BearPizzeria.Kitchen.csproj
│   ├── BearPizzeria.Delivery/
│   │   ├── Program.cs
│   │   └── BearPizzeria.Delivery.csproj
│   └── BearPizzeria.Client/
│       ├── Program.cs
│       └── BearPizzeria.Client.csproj
├── script.sql
├── README.md
└── consumo.go (opcional)
```
