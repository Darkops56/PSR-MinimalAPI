<h1 align="center">E.T. Nº12 D.E. 1º "Libertador Gral. José de San Martín"</h1>
<p align="center">
  <img src="https://et12.edu.ar/imgs/computacion/vamoaprogramabanner.png">
</p>

# BearPizzeria — Sistema Distribuido de Pizzería Digital

Sistema de gestión de pedidos para pizzería con arquitectura distribuida. Implementa comunicación vía **REST (HTTP)** entre el cliente y el backend, y **Sockets TCP** entre el backend y los módulos internos de Cocina y Reparto.

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

## Pre-requisitos

- SDK .NET 10
- MySQL Server 8.0+
- Visual Studio Code / Rider / Visual Studio

## Estructura del Proyecto

```
BearPizzeria.sln
├── docs/
│   ├── PlanDeTrabajo.md
│   ├── CasosDeUso.md
│   ├── Der.md
│   └── DiagramaDeClases.md
├── src/
│   ├── BearPizzeria.Api/          ← Backend Minimal API
│   ├── BearPizzeria.Kitchen/      ← Cocina Automatizada
│   ├── BearPizzeria.Delivery/     ← Reparto
│   └── BearPizzeria.Client/       ← Cliente consumidor
├── script.sql
└── README.md
```

## Instalación y Ejecución

### 1. Base de Datos

```bash
mysql -u 5to_agbd -p < script.sql
```

### 2. Configurar conexión

Crear `src/BearPizzeria.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MySQL": "Server=localhost;User ID=5to_agbd;Password=Trigg3rs!;Database=5to_Todos;"
  }
}
```

### 3. Ejecutar el Backend

```bash
dotnet run --project src/BearPizzeria.Api
```

La API estará disponible en `http://localhost:5250` y la documentación Scalar en `/scalar`.

### 4. Ejecutar Cocina

```bash
dotnet run --project src/BearPizzeria.Kitchen
```

### 5. Ejecutar Reparto

```bash
dotnet run --project src/BearPizzeria.Delivery
```

### 6. Ejecutar Cliente

```bash
dotnet run --project src/BearPizzeria.Client
```

## Endpoints de la API

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/pizzas` | Catálogo de pizzas |
| `GET` | `/api/pizzas/{id}` | Detalle de una pizza |
| `POST` | `/api/clientes` | Registrar un cliente |
| `POST` | `/api/pedidos` | Crear un pedido |
| `GET` | `/api/pedidos/{id}` | Consultar estado de pedido |
| `PATCH` | `/api/pedidos/{id}/estado` | Actualizar estado (interno) |

## Ciclo de Vida del Pedido

```
EsperaDeConfirmacion → EnPreparacion → EnViaje → Entregado
```

## Documentación

- [Casos de Uso](docs/CasosDeUso.md)
- [Diagrama Entidad-Relación](docs/Der.md)
- [Diagrama de Clases](docs/DiagramaDeClases.md)
- [Plan de Trabajo](docs/PlanDeTrabajo.md)
