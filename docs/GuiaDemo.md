# Guía de Demo Funcional — BearPizzeria

## Prerrequisitos

- SDK .NET 10
- MySQL Server 8.0+ corriendo
- 4 terminales disponibles

## Pasos para la Demo

### 1. Preparar la Base de Datos

```bash
mysql -u 5to_agbd -p < script.sql
```

### 2. Iniciar el Backend (Terminal 1)

```bash
dotnet run --project src/BearPizzeria.Api
```

Esperar mensaje: `SocketServer iniciado en puerto 5050`

### 3. Iniciar Cocina (Terminal 2)

```bash
dotnet run --project src/BearPizzeria.Kitchen
```

Verificar: `Conectado al backend. Esperando pedidos...`

### 4. Iniciar Reparto (Terminal 3)

```bash
dotnet run --project src/BearPizzeria.Delivery
```

Verificar: `Conectado al backend. Esperando pedidos para entregar...`

### 5. Ejecutar Cliente (Terminal 4)

```bash
dotnet run --project src/BearPizzeria.Client
```

## Flujo Feliz (Happy Path)

1. **Registrar cliente** — Opción 1 en el menú del Cliente
2. **Ver catálogo** — Opción 2
3. **Realizar pedido** — Opción 3, ingresar IDs de pizzas (ej: 1,2,3)
4. **Consultar estado** — Opción 4, ingresar el número de pedido

### Observar en las terminales:

| Terminal | Evento |
|----------|--------|
| **Cliente** | Recibe `201 Created` con ID y estado `EnPreparacion` |
| **Cocina** | Muestra "NUEVO PEDIDO #X", simula 5s de preparación |
| **Cocina** | Envía "EnViaje" |
| **Reparto** | Muestra "PEDIDO EN VIAJE #X", simula 5s de entrega |
| **Reparto** | Envía "Entregado" |
| **Cliente** | Consulta estado y ve "Entregado" |

## Inyección de Fallos

### Prueba 1: Caída de Cocina

1. Iniciar el flujo feliz
2. Matar la Terminal 2 (Cocina) con `Ctrl+C` durante la preparación
3. Verificar que el Backend sigue operativo (Terminal 1 sin errores)
4. El pedido queda en estado `EnPreparacion`
5. Reiniciar Cocina (se reconecta automáticamente)
6. Los nuevos pedidos se procesan normalmente

### Prueba 2: Reconexión de Reparto

1. Matar la Terminal 3 (Reparto) con `Ctrl+C`
2. Crear un nuevo pedido desde el Cliente
3. Cocina procesa y lo marca como "EnViaje"
4. Ver en Backend: "Cliente removido por desconexión"
5. Reiniciar Reparto (se reconecta automáticamente)
6. Backend detecta la reconexión en el próximo pedido

### Prueba 3: Backend caído

1. Matar la Terminal 1 (Backend)
2. Cliente muestra "Error de conexión"
3. Cocina y Reparto muestran "Reconectando en 5 segundos..."
4. Reinciar Backend
5. Cocina y Reparto se reconectan automáticamente

## Validaciones

| Escenario | Endpoint | Respuesta Esperada |
|-----------|----------|-------------------|
| Cliente sin nombre | `POST /api/clientes` | `400 Bad Request` — "El nombre es obligatorio" |
| Pedido sin ítems | `POST /api/pedidos` | `400 Bad Request` — "debe tener al menos una pizza" |
| Cantidad = 0 | `POST /api/pedidos` | `400 Bad Request` — "mayores a cero" |
| Pizza inexistente | `POST /api/pedidos` | `400 Bad Request` — "no existen" |
| Estado inválido | `PATCH /api/pedidos/1/estado` | `400 Bad Request` — "Estado inválido" |
| Pedido inexistente | `GET /api/pedidos/999` | `404 Not Found` |
