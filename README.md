<h1 align="center">E.T. Nº12 D.E. 1º "Libertador Gral. José de San Martín"</h1>
<p align="center">
  <img src="https://et12.edu.ar/imgs/computacion/vamoaprogramabanner.png">
</p>

# BearPizzeria — Sistema Distribuido de Pizzería Digital

Sistema de gestión de pedidos para pizzería con arquitectura distribuida. Implementa comunicación vía **REST (HTTP)** entre el cliente y el backend, y **Sockets TCP** entre el backend y los módulos internos de Cocina y Reparto.

## Integrantes del Grupo

- **_________________________________** <!-- Apellido, Nombre -->
- **_________________________________** <!-- Apellido, Nombre -->
- **_________________________________** <!-- Apellido, Nombre -->
- **_________________________________** <!-- Apellido, Nombre -->
- **_________________________________** <!-- Apellido, Nombre -->

---

## Arquitectura

```
Cliente (App C#)  ──HTTP──>  Backend (Minimal API + EFCore + Sockets)
                              │
                              ├──TCP──> Cocina Automatizada (App C#)
                              │
                              ├──TCP──> Reparto (App C#)
                              │
                              └──EFCore──> MySQL
```

### Actores

| Actor | Descripción | Tecnología |
|-------|-------------|------------|
| **Cliente Hambriento** | Usuario final que realiza y consulta pedidos | App consola C# (`HttpClient`) |
| **Backend** | API central que orquesta el flujo de pedidos | Minimal API .NET 10 + EFCore + Sockets |
| **Cocina Automatizada** | Recibe pedidos y simula preparación | App consola C# (`TcpClient`) |
| **Reparto** | Recibe pedidos listos y simula entrega | App consola C# (`TcpClient`) |

### Stack Tecnológico

| Componente | Tecnología |
|------------|------------|
| Lenguaje | C# 13 (.NET 10) |
| Framework Web | ASP.NET Core Minimal API |
| ORM | Entity Framework Core + `MySql.EntityFrameworkCore` |
| Base de Datos | MySQL 8+ |
| Documentación API | Scalar (`/scalar`) + OpenAPI |
| Validación | FluentValidation |
| Comunicación Interna | Sockets TCP (puerto 5050) |

---

## Pre-requisitos

- SDK .NET 10
- MySQL Server 8.0+
- Visual Studio Code / Rider / Visual Studio

## Estructura del Proyecto

```
BearPizzeria.slnx
├── bd/
│   ├── DDL.sql           ← Creación de base de datos y tablas
│   ├── INSERTS.sql       ← Datos de prueba
│   └── USERS.sql         ← Usuarios (actores) con permisos mínimos
├── docs/
│   ├── PlanDeTrabajo.md
│   ├── CasosDeUso.md
│   ├── Der.md
│   └── DiagramaDeClases.md
├── src/
│   ├── BearPizzeria.Api/          ← Backend Minimal API
│   ├── BearPizzeria.Kitchen/      ← Cocina Automatizada
│   ├── BearPizzeria.Delivery/     ← Reparto
│   └── BearPizzeria.Client/       ← Cliente consumidor (*)
├── script.sql                     ← Script legacy (reemplazado por bd/)
└── README.md
```

> **(\*)** `BearPizzeria.Client` no está incluido en la solución actual. Se puede implementar como proyecto adicional.

---

## Instalación y Ejecución (paso a paso)

### 1. Base de Datos

```bash
# 1a. Crear la base de datos y las tablas
mysql -u root -p < bd/DDL.sql

# 1b. Crear los usuarios de la aplicación con permisos mínimos
mysql -u root -p < bd/USERS.sql

# 1c. Cargar datos de prueba
mysql -u root -p < bd/INSERTS.sql
```

### 2. Configurar conexión a MySQL

Editar `src/BearPizzeria.Api/appsettings.Development.json` (o crear `appsettings.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=5to_Todos;Uid=backend_app;Pwd=Back3nd_P55!;"
  }
}
```

> **Nota:** El usuario `5to_agbd` del script legacy fue reemplazado por `backend_app`. Ajustá el `Uid` y `Pwd` según el usuario que hayas configurado.

### 3. Ejecutar el Backend (API)

```bash
dotnet run --project src/BearPizzeria.Api
```

La API estará disponible en:
- **HTTP:** `http://localhost:5250`
- **HTTPS:** `https://localhost:7225`
- **Documentación Scalar:** `http://localhost:5250/scalar`
- **OpenAPI Spec:** `http://localhost:5250/openapi/v1.json`

> Debe estar corriendo **primero** el backend antes que Cocina o Reparto, ya que estos se conectan al socket TCP del backend.

### 4. Ejecutar Cocina Automatizada

En una terminal separada (con el backend corriendo):

```bash
dotnet run --project src/BearPizzeria.Kitchen
```

La cocina se conectará automáticamente al backend por TCP (puerto 5050) y se identificará como `"COCINA"`. Quedará a la espera de nuevos pedidos.

### 5. Ejecutar Reparto

En una terminal separada (con el backend corriendo):

```bash
dotnet run --project src/BearPizzeria.Delivery
```

El reparto se conectará automáticamente al backend por TCP (puerto 5050) y se identificará como `"REPARTO"`. Quedará a la espera de notificaciones.

### 6. Probar con un Cliente HTTP

Ejemplo con `curl` (o desde Scalar en `http://localhost:5250/scalar`):

```bash
# Registrar un cliente
curl -X POST http://localhost:5250/api/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "usuario": "testuser",
    "nombre": "Test User",
    "direccion": "Calle 123",
    "telefono": "11-1234-5678",
    "email": "test@email.com"
  }'

# Ver catálogo de pizzas
curl http://localhost:5250/api/pizzas

# Crear un pedido (debe haber Cocina conectada para ver la notificación)
curl -X POST http://localhost:5250/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{
    "clienteUsuario": "testuser",
    "items": [
      { "pizzaNombre": "Muzzarella", "cantidad": 2 }
    ]
  }'

# Consultar estado del pedido
curl http://localhost:5250/api/pedidos/1

# Avanzar estado manualmente (EnPreparacion → EnViaje)
curl -X PATCH http://localhost:5250/api/pedidos/1/estado

# Avanzar estado nuevamente (EnViaje → Entregado)
curl -X PATCH http://localhost:5250/api/pedidos/1/estado
```

---

## Endpoints de la API

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/pizzas` | Catálogo de pizzas |
| `GET` | `/api/pizzas/{id}` | Detalle de una pizza |
| `POST` | `/api/pizzas` | Crear una nueva pizza |
| `DELETE` | `/api/pizzas/{id}` | Eliminar pizza (si no está en pedidos) |
| `GET` | `/api/clientes` | Listar todos los clientes |
| `GET` | `/api/clientes/{id}` | Detalle de un cliente |
| `POST` | `/api/clientes` | Registrar un cliente |
| `DELETE` | `/api/clientes/{id}` | Eliminar cliente (si no tiene pedidos) |
| `POST` | `/api/pedidos` | Crear un pedido |
| `GET` | `/api/pedidos/{id}` | Consultar estado de pedido |
| `PATCH` | `/api/pedidos/{id}/estado` | Avanzar al siguiente estado |
| `DELETE` | `/api/pedidos/{id}` | Eliminar pedido |

## Ciclo de Vida del Pedido

```
EnPreparacion → EnViaje → Entregado
```

La transición entre estados se realiza mediante `PATCH /api/pedidos/{id}/estado`. Cada invocación avanza un estado. Si el pedido ya está `Entregado`, retorna error `400`.

---

## Guía de Pruebas por Actor

Para ver el detalle de pruebas paso a paso por cada actor, incluyendo posibles errores y soluciones, consultar:

👉 **[Guía de Pruebas — src/README.md](src/README.md)**

---

## Documentación del Proyecto

- [Casos de Uso](docs/CasosDeUso.md) — Con matriz de implementación real
- [Diagrama Entidad-Relación](docs/Der.md)
- [Diagrama de Clases](docs/DiagramaDeClases.md)
- [Plan de Trabajo](docs/PlanDeTrabajo.md)
- [Guía de Pruebas por Actor](src/README.md)
