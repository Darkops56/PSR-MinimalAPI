# Guía de Pruebas por Actor — BearPizzeria

> Esta guía detalla paso a paso cómo probar el sistema desde la perspectiva de cada actor,
> los posibles errores que pueden surgir y sus soluciones.

---

## Índice

1. [Cliente Hambriento (API REST)](#1-cliente-hambriento-api-rest)
2. [Backend (API)](#2-backend-api)
3. [Cocina Automatizada (TCP)](#3-cocina-automatizada-tcp)
4. [Reparto (TCP)](#4-reparto-tcp)
5. [Errores Comunes y Soluciones](#5-errores-comunes-y-soluciones)

---

## 1. Cliente Hambriento (API REST)

### Flujo de pruebas

| Paso | Acción | Comando / Detalle | Resultado Esperado |
|------|--------|-------------------|--------------------|
| 1.1 | Registrar cliente | `POST /api/clientes` con `{ usuario, nombre, direccion, telefono, email }` | `201 Created` + ID asignado |
| 1.2 | Consultar catálogo | `GET /api/pizzas` | `200 OK` + lista de pizzas |
| 1.3 | Crear pedido | `POST /api/pedidos` con `{ clienteUsuario, items: [{ pizzaNombre, cantidad }] }` | `201 Created` + pedido en `EnPreparacion` |
| 1.4 | Consultar estado | `GET /api/pedidos/{id}` | `200 OK` + detalle del pedido |
| 1.5 | Consultar cliente | `GET /api/clientes/{id}` | `200 OK` + datos del cliente |
| 1.6 | Listar clientes | `GET /api/clientes` | `200 OK` + array de clientes |

### Casos borde

| Escenario | Acción | Resultado Esperado |
|-----------|--------|--------------------|
| Cliente duplicado | `POST /api/clientes` con `usuario` existente | `400 Bad Request` + error `CLIENTE-400` |
| Email duplicado | `POST /api/clientes` con `email` existente | `400 Bad Request` + error `CLIENTE-400` |
| Datos inválidos | `POST /api/clientes` con teléfono incorrecto | `400 Bad Request` + errores de validación |
| Cliente inexistente | `POST /api/pedidos` con `clienteUsuario` inválido | `400 Bad Request` + error `PEDIDO-404` |
| Pizza inexistente | `POST /api/pedidos` con `pizzaNombre` inválido | `400 Bad Request` + error `PEDIDO-404` |
| Pedido inexistente | `GET /api/pedidos/9999` | `404 Not Found` + error `PEDIDO-404` |

---

## 2. Backend (API)

### Flujo de pruebas

| Paso | Acción | Detalle | Resultado Esperado |
|------|--------|---------|--------------------|
| 2.1 | Iniciar backend | `dotnet run --project src/BearPizzeria.Api` | Consola muestra "Now listening on..." + "SocketServer iniciado en puerto 5050" |
| 2.2 | Verificar Scalar | Navegar a `http://localhost:5250/scalar` | Documentación interactiva visible |
| 2.3 | Verificar OpenAPI | `GET http://localhost:5250/openapi/v1.json` | `200 OK` + JSON schema |
| 2.4 | Verificar conexión BD | Consultar cualquier endpoint que use DB | Respuesta exitosa (sin errores de conexión) |
| 2.5 | Verificar socket | Conectar Cocina/Reparto | Log muestra "Cliente conectado: ..." + "Cocina registrada" / "Reparto registrada" |
| 2.6 | Probar ciclo pedido | Crear pedido → PATCH estado → Verificar | Log muestra cada transición |

### Endpoints administrativos

| Método | Ruta | Propósito |
|--------|------|-----------|
| `POST` | `/api/pizzas` | Crear nueva pizza en el catálogo |
| `DELETE` | `/api/pizzas/{id}` | Eliminar pizza (solo si no está en pedidos) |
| `DELETE` | `/api/clientes/{id}` | Eliminar cliente (solo si no tiene pedidos) |
| `DELETE` | `/api/pedidos/{id}` | Eliminar pedido y sus items |

---

## 3. Cocina Automatizada (TCP)

### Flujo de pruebas

| Paso | Acción | Detalle | Resultado Esperado |
|------|--------|---------|--------------------|
| 3.1 | Iniciar Cocina | `dotnet run --project src/BearPizzeria.Kitchen` | Consola: "Conectando..." → "Conectado al backend. Esperando pedidos..." |
| 3.2 | Verificar registro | Revisar log del backend | Backend muestra: "Cocina registrada: ..." |
| 3.3 | Crear pedido (desde Cliente) | `POST /api/pedidos` | Cocina muestra: "NUEVO PEDIDO #X" con detalle |
| 3.4 | Esperar preparación | 5 segundos | Cocina cuenta: "Preparando pedido..." → "Pedido listo para reparto!" |
| 3.5 | Ver envío | Cocina envía `ActualizarEstado` a `EnViaje` | Cocina muestra: "Pedido #X marcado como 'En Viaje'" |

### ⚠️ Limitación conocida

El mensaje `ActualizarEstado` que envía la Cocina **es ignorado por el backend**.
El estado real del pedido **no cambia** automáticamente.
Para avanzar el estado hay que usar manualmente `PATCH /api/pedidos/{id}/estado`.

### Casos borde

| Escenario | Acción | Resultado Esperado |
|-----------|--------|--------------------|
| Cocina se conecta después del pedido | Iniciar Cocina post-creación | No recibe pedidos anteriores (no hay replay) |
| Cocina se desconecta | Matar proceso Cocina | Backend log: "Cocina removida por desconexión" |
| Reconexión | Cocina se reinicia | Se reconecta automáticamente tras 5 segundos |

---

## 4. Reparto (TCP)

### Flujo de pruebas

| Paso | Acción | Detalle | Resultado Esperado |
|------|--------|---------|--------------------|
| 4.1 | Iniciar Reparto | `dotnet run --project src/BearPizzeria.Delivery` | Consola: "Conectando..." → "Conectado al backend. Esperando pedidos para entregar..." |
| 4.2 | Verificar registro | Revisar log del backend | Backend muestra: "Reparto registrado: ..." |

### ❌ Notificaciones no implementadas

El Reparto **nunca recibe** notificaciones de pedidos en viaje porque el método
`EnviarADeliveryAsync` del backend nunca es invocado. Como consecuencia:

- El Reparto queda a la espera permanente.
- No se ejecuta el flujo de "recibir pedido → simular viaje → confirmar entrega".
- No se envía el mensaje `ActualizarEstado` a `Entregado`.

### Solución propuesta

Para que Reparto funcione correctamente, modificar `Program.cs` en el endpoint PATCH:

```csharp
// En PATCH /api/pedidos/{id}/estado, luego de actualizar el estado:
if (pedido.Estado == EstadoPedido.EnViaje)
{
    await socketServer.EnviarADeliveryAsync(id, CancellationToken.None);
}
```

---

## 5. Errores Comunes y Soluciones

### 5.1 Error de conexión a MySQL

**Síntoma:** Al iniciar el backend, aparece:

```
Unhandled exception: MySql.Data.MySqlClient.MySqlException (0x80004005):
  Unable to connect to any of the specified MySQL hosts.
```

**Causas posibles:**
1. MySQL no está corriendo.
2. Puerto incorrecto (no es 3306).
3. Host incorrecto (no es `localhost`).
4. Firewall bloqueando el puerto 3306.

**Soluciones:**

```bash
# Verificar que MySQL está activo
sudo systemctl status mysql
# o en Windows:
net start MySQL80

# Verificar puerto
sudo ss -tlnp | grep 3306

# Verificar que el usuario existe y tiene acceso
mysql -u backend_app -p -h localhost -P 3306
```

---

### 5.2 Error "Connection string 'MySQL' not found"

**Síntoma:**

```
Unhandled exception: System.InvalidOperationException:
  Connection string 'MySQL' not found
```

**Causa:** Falta el archivo `appsettings.json` o `appsettings.Development.json`, o no tiene la sección `ConnectionStrings`.

**Solución:** Verificar que existe `src/BearPizzeria.Api/appsettings.Development.json` con el contenido:

```json
{
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=5to_Todos;Uid=backend_app;Pwd=Back3nd_P55!;"
  }
}
```

> El archivo `appsettings.Development.json` tiene prioridad sobre `appsettings.json` en entorno de desarrollo.

---

### 5.3 Error "Access denied for user"

**Síntoma:**

```
MySqlException: Access denied for user 'backend_app'@'localhost'
```

**Causas posibles:**
1. Contraseña incorrecta.
2. Usuario no creado.
3. Usuario creado pero sin permisos sobre `5to_Todos`.

**Soluciones:**

```bash
# Verificar que el usuario existe
mysql -u root -p -e "SELECT User, Host FROM mysql.user WHERE User = 'backend_app';"

# Re-crear usuario y permisos
mysql -u root -p < bd/USERS.sql

# Probar conexión
mysql -u backend_app -p -D 5to_Todos -e "SHOW TABLES;"
```

---

### 5.4 Error "Table doesn't exist"

**Síntoma:**

```
MySqlException: Table '5to_Todos.Clientes' doesn't exist
```

**Causa:** No se ejecutó `DDL.sql`.

**Solución:**

```bash
mysql -u root -p < bd/DDL.sql
```

---

### 5.5 Error de puerto ocupado (backend)

**Síntoma:**

```
Unable to bind to http://localhost:5250: address already in use.
```

**Causa:** Ya hay un proceso usando el puerto 5250.

**Solución:**

```bash
# Encontrar el proceso que usa el puerto
sudo lsof -i :5250
# o
netstat -ano | findstr :5250

# Matar el proceso
kill -9 <PID>
# o en Windows:
taskkill /PID <PID> /F
```

---

### 5.6 Error de puerto ocupado (socket TCP 5050)

**Síntoma:**

El backend inicia pero el log muestra errores de conexión en Cocina/Reparto.

**Solución:**

```bash
# Verificar qué proceso usa el puerto 5050
sudo lsof -i :5050
```

---

### 5.7 Cocina o Reparto no reciben notificaciones

**Síntoma:** Se crea un pedido pero Cocina no muestra nada.

**Causas posibles:**
1. El backend no está corriendo.
2. Cocina no está conectada (o se conectó después de crear el pedido).
3. Firewall bloqueando el puerto 5050.

**Soluciones:**

```bash
# 1. Verificar que el backend esté corriendo
curl http://localhost:5250/api/pizzas

# 2. Verificar orden de inicio:
#    Backend → Cocina → Reparto (SIEMPRE en ese orden)

# 3. Verificar que Cocina se identificó correctamente
#    En el log del backend debe aparecer:
#    "Cocina registrada: ..."
```

---

### 5.8 El pedido no avanza de estado automáticamente

**Síntoma:** La Cocina indica "Pedido listo para reparto!" pero el estado sigue `EnPreparacion`.

**Causa:** Es el comportamiento esperado. El backend ignora los mensajes `ActualizarEstado` de Cocina y Reparto.

**Solución:** Usar manualmente:

```bash
curl -X PATCH http://localhost:5250/api/pedidos/{id}/estado
```

---

### 5.9 Error de validación de datos

**Síntoma:** `400 Bad Request` con errores de validación.

**Causas comunes y soluciones:**

| Campo | Regla | Ejemplo válido |
|-------|-------|----------------|
| `usuario` | Solo alfanumérico + guión bajo | `juan_perez` |
| `nombre` | Requerido, máximo 100 chars | `Juan Pérez` |
| `telefono` | Formato telefónico | `11-1234-5678` |
| `email` | Formato email válido | `juan@email.com` |
| `direccion` | Requerido, máximo 200 chars | `Av. Siempre Viva 123` |
| `clienteUsuario` | Debe existir en BD | Registrar antes de pedir |
| `pizzaNombre` | Debe coincidir con catálogo | `Muzzarella`, `Napolitana` |
| `cantidad` | Entero positivo | `1`, `2`, `3` |

---

### 5.10 Error al eliminar cliente con pedidos

**Síntoma:** `400 Bad Request` + error `CLIENTE-409`.

**Causa:** El cliente tiene pedidos asociados y no se puede eliminar.

**Solución:** Eliminar primero los pedidos del cliente, o cambiar el `DeleteBehavior` en el DbContext.

---

### 5.11 Error de reconstrucción del proyecto

**Síntoma:**

```
error NU1101: Unable to find package MySql.EntityFrameworkCore
```

**Causas posibles:**
1. SDK .NET 10 no instalado.
2. Paquetes NuGet no restaurados.

**Soluciones:**

```bash
# Verificar versión de .NET
dotnet --version  # Debe mostrar 10.x

# Restaurar paquetes
dotnet restore src/BearPizzeria.Api

# Limpiar y reconstruir
dotnet clean src/BearPizzeria.Api
dotnet build src/BearPizzeria.Api
```

---

### 5.12 Error de esquema en appsettings.Development.json

**Síntoma:** El backend ignora la conexión configurada o no la encuentra.

**Solución:** Verificar que la propiedad se llame exactamente `"ConnectionStrings"` (plural) y la sub-propiedad se llame `"MySQL"`:

```json
{
  "ConnectionStrings": {
    "MySQL": "..."
  }
}
```

Esto debe coincidir con `Program.cs:13`:

```csharp
var connectionString = builder.Configuration.GetConnectionString("MySQL");
```

El método `GetConnectionString()` busca específicamente en la sección `ConnectionStrings`.
