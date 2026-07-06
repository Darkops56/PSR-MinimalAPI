# Guion de Defensa Oral — BearPizzeria

## Estructura de la Presentación

### 1. Introducción (2 min)

**"Hoy presentamos BearPizzeria, un sistema distribuido de gestión de pedidos para pizzería que integra programación asincrónica, sockets TCP, Entity Framework Core y una arquitectura REST."**

- Proyecto académico para la materia Programación de Sistemas en Red
- 4 componentes independientes comunicándose en red
- Demostración de conceptos de sistemas distribuidos

---

### 2. Arquitectura General (3 min)

**"El sistema cuenta con cuatro actores principales..."**

| Actor | Tecnología | Comunicación |
|-------|-----------|-------------|
| **Cliente Hambriento** | App consola C# | HTTP REST |
| **Backend API** | Minimal API .NET 10 | HTTP + Sockets TCP |
| **Cocina Automatizada** | App consola C# | TCP Cliente |
| **Reparto** | App consola C# | TCP Cliente |

**Punto clave:** "Separamos la comunicación en dos planos: el cliente interactúa vía REST (síncrono, request-response), mientras que los módulos internos usan sockets TCP (persistentes, bidireccionales)."

---

### 3. Async/Await y Programación Asincrónica (3 min)

**"Todo el sistema está construido sobre async/await, desde el primer endpoint hasta el último socket."**

Ejemplos concretos:

```csharp
// Endpoint asincrónico con EFCore
app.MapPost("/api/pedidos", async (request, db, socketServer) =>
{
    await db.Pedidos.AddAsync(pedido);
    await db.SaveChangesAsync();
    await socketServer.NotificarNuevoPedidoAsync(pedido);
});

// Socket TCP no bloqueante
var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
```

**¿Por qué async/await?**
- Escalabilidad: no se bloquean hilos del thread pool mientras esperamos I/O (red, DB)
- Eficiencia: una sola instancia del backend puede atender múltiples clientes simultáneamente
- Legibilidad: el código se lee como si fuera síncrono, sin callbacks

---

### 4. Sockets TCP (3 min)

**"Los sockets TCP son el corazón de la comunicación interna del sistema."**

Arquitectura:
- Backend ejecuta un `TcpListener` en un `BackgroundService`
- Cocina y Reparto se conectan como `TcpClient`
- Protocolo: mensajes JSON delimitados por `\n`

```csharp
// Servidor (Backend)
var listener = new TcpListener(IPAddress.Any, 5050);
var client = await listener.AcceptTcpClientAsync();

// Cliente (Cocina/Reparto)
using var client = new TcpClient();
await client.ConnectAsync("localhost", 5050);
```

**¿Por qué sockets en vez de otra cosa?**
- Comunicación persistente y bidireccional
- Latencia mínima (sin overhead HTTP)
- Control total sobre el protocolo
- Ideal para notificaciones en tiempo real

**Manejo de errores:**
```csharp
catch (SocketException ex)
{
    _logger.LogWarning("Cliente desconectado");
    // Backend sigue operativo
}
```

---

### 5. Entity Framework Core vs Dapper (3 min)

**"Migramos de Dapper a Entity Framework Core para aprovechar el mapeo objeto-relacional completo."**

| Aspecto | Dapper (anterior) | EFCore (actual) |
|---------|-------------------|-----------------|
| Enfoque | Micro-ORM | ORM completo |
| Consultas | SQL manual | LINQ + generación automática |
| Migraciones | No tiene | Integradas |
| Cambio tracking | Manual | Automático |
| Relaciones | Manual | Fluent API |

Ejemplo comparativo:

```csharp
// Dapper: SQL manual
var query = "SELECT * FROM Todo";
var todos = await conexion.QueryAsync<Todo>(query);

// EFCore: LINQ
var pizzas = await db.Pizzas
    .Where(p => p.Precio > 4000)
    .OrderBy(p => p.Nombre)
    .ToListAsync();
```

**¿Por qué EFCore?**
- Menos código boilerplate
- Seguridad: parametrización automática (previene SQL injection)
- Productividad: cambios en modelos se reflejan en DB
- Ideal para dominios con relaciones complejas (Cliente → Pedido → Pizza)

---

### 6. Manejo de Fallos en Entornos Distribuidos (2 min)

**"En un sistema distribuido, los fallos son la norma, no la excepción."**

Estrategias implementadas:

| Escenario | Mecanismo |
|-----------|-----------|
| Socket se cae (Cocina/Reparto) | Try-catch + logging, Backend sigue operativo |
| Backend se cae | Reconexión automática con backoff (5s) |
| Payload inválido | Validación en endpoints + `400 Bad Request` |
| Timeout de red | `HttpClient` lanza `HttpRequestException` |

**Demo de inyección de fallos:**
1. Matar Cocina durante preparación → Backend intacto
2. Reiniciar Cocina → Reconexión automática
3. Backend caído → Cliente muestra error claro, no crash

---

### 7. Estructura del Proyecto (1 min)

```
BearPizzeria.slnx
├── src/
│   ├── BearPizzeria.Api/     (API + EFCore + Sockets)
│   ├── BearPizzeria.Kitchen/ (App consola - Cocina)
│   ├── BearPizzeria.Delivery/ (App consola - Reparto)
│   └── BearPizzeria.Client/  (App consola - Cliente)
├── docs/                     (Documentación técnica)
├── script.sql                (Esquema MySQL)
└── README.md
```

---

### 8. Conclusión (1 min)

**"BearPizzeria demuestra los conceptos fundamentales de la programación en red:..."**

1. Comunicación REST para clientes externos (síncrona, stateless)
2. Sockets TCP para comunicación interna (persistente, bidireccional)
3. Async/await para escalabilidad vertical
4. Entity Framework Core para persistencia con cambio tracking
5. Manejo de fallos como parte integral del diseño

**"El sistema es extensible: podrían agregarse nuevos módulos (por ejemplo, una app móvil) conectándolos al backend por REST, o nuevos servicios internos mediante sockets TCP."**

---

### Preguntas Frecuentes para la Defensa

**P: ¿Por qué no usaste SignalR en vez de sockets TCP?**
R: SignalR está pensado para HTTP (WebSockets sobre HTTP). El objetivo era implementar sockets TCP a nivel de transporte para demostrar el manejo directo del protocolo.

**P: ¿Cómo manejás la concurrencia en el servidor de sockets?**
R: Cada cliente conectado se maneja en una tarea independiente (`Task.Run`). Los diccionarios de clientes usan `ConcurrentDictionary` para acceso thread-safe.

**P: ¿Qué pasa si el backend se cae mientras Cocina está preparando?**
R: Cocina detecta la desconexión y entra en un bucle de reconexión. Al volver el backend, se reconecta automáticamente. El pedido queda persistido en MySQL.

**P: ¿Por qué usaste `MySql.EntityFrameworkCore` y no `Pomelo`?**
R: `MySql.EntityFrameworkCore` 10.0.7 tiene soporte nativo para .NET 10, mientras que Pomelo aún no tenía una versión estable compatible.
