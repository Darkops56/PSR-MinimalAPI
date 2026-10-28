# Diagrama de Arquitectura — BearPizzeria

> [!NOTE]
> Este documento detalla la **arquitectura de software distribuida** de **BearPizzeria**, explicando el propósito interno de cada uno de los **4 proyectos .NET 10** que componen la solución (`BearPizzeria.Mvc`, `BearPizzeria.Api`, `BearPizzeria.Kitchen` y `BearPizzeria.Delivery`), su integración con **MySQL**, los protocolos de transporte híbridos (**HTTP/REST + Cookies/JWT + Sockets TCP NDJSON**) y la desestructuración parte por parte de cada subsistema.

---

## 1. Visión General de la Arquitectura Distribuida

**BearPizzeria** está diseñado bajo una arquitectura **cliente-servidor desacoplada y orientada a eventos en tiempo real**, donde cada proyecto corre como un proceso independiente con responsabilidades claramente delimitadas:

1. **`BearPizzeria.Mvc` (Puerto HTTP `5082` / HTTPS `7188`)**: Aplicación Web Frontend en ASP.NET Core MVC que renderiza la interfaz de usuario mediante vistas Razor (`.cshtml`), administra la sesión del comprador con cookies encriptadas (`BearPizzeria.Auth`) y ejecuta módulos JavaScript ES6 (`cart.js`, `customizer.js`, `tracker.js`, `addressDrawer.js`) en el navegador.
2. **`BearPizzeria.Api` (Puerto HTTP `5250` / HTTPS `7225` + Puerto TCP `5050`)**: Núcleo Backend construido con Minimal APIs en .NET 10. Centraliza las reglas de negocio, el control de stock, la criptografía (`PBKDF2` + `JWT HMAC-SHA256`), el acceso transaccional a datos mediante **Entity Framework Core** y aloja en segundo plano el servidor de sockets TCP (`SocketServerService`).
3. **`BearPizzeria.Kitchen` (Cliente TCP hacia `:5050`)**: Worker de consola independiente que representa la **Cocina Automatizada**. Mantiene un socket TCP abierto con el backend, recibe los pedidos recién confirmados, simula los 5 segundos de horneado y notifica cuando están listos para salir a la calle (`EnViaje`).
4. **`BearPizzeria.Delivery` (Cliente TCP hacia `:5050`)**: Worker de consola independiente que representa el **Reparto Automatizado**. Escucha por socket TCP los pedidos que acaban de salir de cocina (`PedidoEnViaje`), simula los 5 segundos de traslado en moto hasta el domicilio y confirma la entrega final (`Entregado`).
5. **Base de Datos Relacional `MySQL` (`5to_Todos`, Puerto `3306`)**: Motor de persistencia relacional donde residen las 8 tablas del sistema (`Clientes`, `Usuarios`, `DireccionesCliente`, `Pizzas`, `Carritos`, `CarritoItems`, `Pedidos`, `PedidoPizzas`).

---

## 2. Diagrama Maestro de Arquitectura e Interconexión de Proyectos

```mermaid
flowchart TB
    subgraph ClienteWeb ["🌐 Capa de Presentación en el Navegador (Client-Side)"]
        direction TB
        DOM["DOM HTML5 + Vanilla CSS (Dark Mode)"]
        JS_Modules["Módulos ES6 (app.js, cart.js, customizer.js, addressDrawer.js, repeatOrderModal.js)"]
        JS_Tracker["Motor de Polling Reactivo (tracker.js cada 3000ms)"]
    end

    subgraph ProyectoMVC ["🖥️ Proyecto 1: BearPizzeria.Mvc (ASP.NET Core MVC — :5082 / :7188)"]
        direction TB
        AuthCookie["Middleware de Autenticación Cookie HttpOnly (BearPizzeria.Auth)"]
        Controllers["Controladores MVC (HomeController, AuthController, CartController, ProfileController)"]
        RazorViews["Motor de Vistas Razor (.cshtml + Layouts + Modales Parciales)"]
        TypedClients["Servicios HttpClient Tipados (PizzaApiService, UsuarioApiService, CarritoApiService, PedidoApiService)"]

        AuthCookie --> Controllers
        Controllers --> TypedClients
        Controllers --> RazorViews
    end

    subgraph ProyectoAPI ["⚙️ Proyecto 2: BearPizzeria.Api (Minimal API .NET 10 — :5250 / :7225)"]
        direction TB
        CORS["Middleware CORS Estricto (AllowMvcClient)"]
        Endpoints["Endpoints REST (/api/auth, /api/pizzas, /api/carrito, /api/pedidos, /api/clientes)"]
        Validators["Validadores FluentValidation (Register, Login, Cliente, Pedido)"]
        Security["Servicios de Seguridad (PasswordHasherService PBKDF2 + TokenService JWT)"]
        EFContext["PizzeriaDbContext (Entity Framework Core + Transacciones ACID)"]
        ChannelQueue["Channel<Pedido> (Cola Asíncrona Unbounded en Memoria)"]
        SocketSrv["SocketServerService (BackgroundService — TcpListener :5050)"]

        CORS --> Endpoints
        Endpoints --> Validators
        Endpoints --> Security
        Endpoints --> EFContext
        Endpoints --> ChannelQueue
        ChannelQueue --> SocketSrv
        SocketSrv --> EFContext
    end

    subgraph CapaDatos ["🗄️ Capa de Persistencia Relacional (MySQL Server :3306)"]
        MySQL[("Base de Datos: 5to_Todos\n(Clientes, Usuarios, DireccionesCliente,\nPizzas, Carritos, CarritoItems,\nPedidos, PedidoPizzas)")]
    end

    subgraph WorkersTCP ["🔌 Proyectos 3 y 4: Workers Operativos por Sockets TCP"]
        direction LR
        Kitchen["👨‍🍳 Proyecto 3: BearPizzeria.Kitchen\n(App Consola C# — TcpClient)\nHandshake: {'Tipo':'COCINA'}"]
        Delivery["🛵 Proyecto 4: BearPizzeria.Delivery\n(App Consola C# — TcpClient)\nHandshake: {'Tipo':'REPARTO'}"]
    end

    %% Conexiones entre capas y proyectos
    DOM <-->|"1. Peticiones HTTP GET/POST\n+ Cookie BearPizzeria.Auth"| AuthCookie
    RazorViews -.->|"2. HTML Renderizado + Token JWT\nen data-attributes"| DOM
    TypedClients <-->|"3. HTTP REST JSON Server-to-Server\n(Header: Authorization Bearer JWT)"| CORS
    JS_Modules <-->|"4. AJAX Fetch Directo (CORS)\nMutaciones de Carrito y Direcciones"| CORS
    JS_Tracker <-->|"5. Polling AJAX cada 3s\nGET /api/pedidos/mis-pedidos-activos"| CORS
    EFContext <-->|"6. Consultas LINQ y Transacciones SQL"| MySQL
    SocketSrv <-->|"7. Socket TCP Bidireccional :5050\n(NuevoPedido / ActualizarEstado: EnViaje)"| Kitchen
    SocketSrv <-->|"8. Socket TCP Bidireccional :5050\n(PedidoEnViaje / ActualizarEstado: Entregado)"| Delivery
```

---

## 3. Desestructuración Parte por Parte de la Arquitectura

A continuación se desarma el diagrama maestro en **5 subsistemas arquitectónicos** para explicar al 100% cómo funciona cada proyecto por dentro y cómo se conecta con los demás.

---

### Parte 1: Comunicación Híbrida entre Navegador, `BearPizzeria.Mvc` y `BearPizzeria.Api`

Una de las características más potentes de **BearPizzeria** es su **arquitectura de comunicación dual** en el frontend (`BearPizzeria.Mvc`): combina el renderizado en servidor (SSR con Razor) con llamadas asíncronas directas desde el navegador (CSR con ES6 Modules).

```mermaid
flowchart LR
    subgraph Browser ["🌐 Navegador del Cliente"]
        HTML["Vistas HTML + Formularios"]
        ES6["Módulos JS (api.js, cart.js, tracker.js)"]
    end

    subgraph MVC ["🖥️ BearPizzeria.Mvc (:5082)"]
        Ctrl["Controladores C#"]
        HttpSrv["Servicios HttpClient Tipados"]
    end

    subgraph API ["⚙️ BearPizzeria.Api (:5250)"]
        Rest["Endpoints Minimal API\n+ Política CORS 'AllowMvcClient'"]
    end

    HTML -->|"Canal A: Navegación y Formularios\n(Cookie HttpOnly + AntiForgeryToken)"| Ctrl
    Ctrl --> HttpSrv
    HttpSrv -->|"Canal A.1: Server-to-Server HTTP\n(Inyecta Bearer Token desde Claims)"| Rest
    ES6 -->|"Canal B: Fetch AJAX Directo\n(Lee JWT de body[data-token])"| Rest
```

#### ¿Cómo funcionan ambos canales y por qué coexisten?
1. **Canal A — Comunicación Server-to-Server (Navegador -> MVC `:5082` -> API `:5250`)**:
   * Se utiliza para cargar páginas completas (`GET /`, `GET /Cart`, `GET /Profile`, `GET /Profile/Trackers`, `GET /Profile/Orders`) y procesar envíos de formularios críticos con protección CSRF (`POST /Auth/Login`, `POST /Auth/Register`, `POST /Cart/Checkout`, `POST /Profile/UpdateProfile`).
   * El navegador envía la cookie segura `BearPizzeria.Auth` (`HttpOnly = true`, inaccesible para scripts maliciosos) hacia `BearPizzeria.Mvc`.
   * El controlador MVC desencripta la cookie, extrae el claim `"Token"` y lo reenvía como cabecera `Authorization: Bearer <JWT>` a través de los clientes tipados (`PizzaApiService`, `UsuarioApiService`, `CarritoApiService`, `PedidoApiService`) hacia `BearPizzeria.Api`.
2. **Canal B — Comunicación Client-to-API Directa (Módulos JS -> API `:5250`)**:
   * Se utiliza para operaciones instantáneas que **no deben recargar la página**: agregar una pizza personalizada al carrito (`cart.js`), incrementar/decrementar cantidades, administrar direcciones en el drawer lateral (`addressDrawer.js`), repetir pedidos (`repeatOrderModal.js`) y consultar el estado del pedido cada 3 segundos (`tracker.js`).
   * Cuando el usuario inicia sesión, `_Layout.cshtml` expone en las etiquetas `data-user-id` y `data-token` del `<body>` el identificador y el token JWT del cliente. El módulo `api.js` adjunta automáticamente ese `Authorization: Bearer <JWT>` en cada petición `fetch()` hacia `http://localhost:5250`, autorizado por la política CORS `AllowMvcClient`.

---

### Parte 2: Arquitectura Interna de `BearPizzeria.Api` y Seguridad Criptográfica

El proyecto `BearPizzeria.Api` actúa como el cerebro transaccional del sistema. No depende de librerías externas pesadas para autenticación, sino que implementa sus propios servicios criptográficos de alta seguridad y validación.

```mermaid
flowchart TB
    Req["Petición HTTP Entrante\n(desde MVC C# o Navegador JS)"] --> CORS["Middleware CORS\n(Orígenes: :5082 y :7188)"]
    CORS --> Router["Enrutador Minimal API (Program.cs)"]

    Router --> Val["1. Validación de Entrada\n(FluentValidation Validators)"]
    Router --> Auth["2. Verificación de Identidad\n(TokenService HMAC-SHA256)"]

    Val --> Logic["3. Lógica de Dominio y Control de Stock"]
    Auth --> Logic

    Logic --> Hash["PasswordHasherService\n(PBKDF2-SHA256, 100.000 iteraciones, Salt 128-bit)"]
    Logic --> DB["PizzeriaDbContext (EF Core)\nTransacciones BeginTransactionAsync()"]
    Logic --> Socket["SocketServerService\nNotificarNuevoPedidoAsync()"]
```

#### Componentes Internos de `BearPizzeria.Api`:
* **`PasswordHasherService`**: Al registrar o autenticar un usuario, nunca almacena contraseñas en texto plano. Genera un `Salt` aleatorio de 16 bytes (128 bits) con `RandomNumberGenerator` y deriva una clave de 32 bytes (256 bits) aplicando `Rfc2898DeriveBytes.Pbkdf2` con **100.000 iteraciones** bajo `SHA256`. Durante el login, compara los hashes usando `CryptographicOperations.FixedTimeEquals` para prevenir ataques de temporización (*Timing Attacks*).
* **`TokenService`**: Construye y valida tokens **JWT (`HS256`)** con expiración de 7 días que contienen los claims `sub`, `clienteId`, `username` y `email`. Cada endpoint privado (`/api/carrito/*`, `/api/pedidos/mis-pedidos-activos`, `/api/clientes/{id}`) verifica que el `clienteId` firmado dentro del token coincida con el recurso solicitado, impidiendo que un cliente consulte o modifique el carrito o pedidos de otro.
* **Transacciones ACID en `PizzeriaDbContext`**: En operaciones compuestas como el **Registro** (crea `Cliente` + `Usuario` + `Carrito`) y el **Checkout** (verifica y descuenta `Stock` de cada `Pizza`, resuelve `DireccionEntrega`, crea `Pedido` + `PedidoPizzas` y limpia `CarritoItems`), `BearPizzeria.Api` utiliza `await db.Database.BeginTransactionAsync()`. Si cualquier paso falla (por ejemplo, si una de las 3 pizzas del carrito no tiene stock suficiente), ejecuta `RollbackAsync()` dejando la base de datos intacta.

---

### Parte 3: Arquitectura del Servidor de Sockets TCP (`SocketServerService`) y Cola Asíncrona

Para comunicar el backend HTTP con los módulos de **Cocina** y **Reparto** sin bloquear la respuesta HTTP del cliente que acaba de confirmar su compra, `BearPizzeria.Api` implementa un patrón **Productor-Consumidor en memoria** mediante `System.Threading.Channels.Channel<Pedido>` conectado a un `BackgroundService` de sockets TCP en el puerto `5050`.

```mermaid
flowchart LR
    subgraph HiloHTTP ["Hilo de Petición HTTP (Checkout)"]
        Checkout["POST /api/carrito/{id}/checkout"]
        Commit["Commit Transacción MySQL"]
        Checkout --> Commit
    end

    subgraph MemoriaAPI ["Memoria Interna de BearPizzeria.Api"]
        Canal[["📬 Channel<Pedido>\n(_pedidoChannel Unbounded)"]]
        DictCocina[("ConcurrentDictionary\n_kitchenClients")]
        DictReparto[("ConcurrentDictionary\n_deliveryClients")]
    end

    subgraph HiloBackground ["Hilo BackgroundService (SocketServerService :5050)"]
        Listener["TcpListener (0.0.0.0:5050)\nAcceptClientsAsync()"]
        Reader["ProcesarPedidosChannelAsync()\nReadAllAsync()"]
        Parser["HandleClientAsync() &\nProcesarMensajeAsync()"]
    end

    Commit -->|"1. WriteAsync(pedido)\n(Retorna 201 Created al instante)"| Canal
    Canal -->|"2. Desencola asíncronamente"| Reader
    Reader -->|"3. Serializa JSON 'NuevoPedido'\ny transmite con '\\n'"| DictCocina
    Listener -->|"Registra Handshake\n{'Tipo':'COCINA'} o {'Tipo':'REPARTO'}"| Parser
    Parser --> DictCocina
    Parser --> DictReparto
```

#### Cómo funciona internamente `SocketServerService`:
1. **Inicio del Servidor (`ExecuteAsync`)**: Al arrancar `BearPizzeria.Api`, `SocketServerService` levanta un `TcpListener` escuchando en `IPAddress.Any` sobre el puerto `5050` e inicia dos bucles concurrentes: `AcceptClientsAsync` (para aceptar conexiones TCP entrantes) y `ProcesarPedidosChannelAsync` (para despachar pedidos de la cola).
2. **Protocolo de Delimitación por Salto de Línea (`NDJSON` — Newline Delimited JSON)**:
   * Dado que TCP es un protocolo orientado a flujo de bytes (*stream*) donde los mensajes pueden fragmentarse o agruparse en el buffer de red (`byte[4096]`), todos los mensajes entre `BearPizzeria.Api`, `BearPizzeria.Kitchen` y `BearPizzeria.Delivery` se serializan en JSON y **se terminan con un carácter `\n`**.
   * El receptor acumula los bytes leídos en un `StringBuilder` y extrae línea por línea cada vez que detecta `\n`, garantizando que jamás se procese un JSON truncado.
3. **Handshake de Identificación de Rol**:
   * Apenas se conecta `BearPizzeria.Kitchen`, envía `{"Tipo":"COCINA"}\n` y el servidor la registra en `_kitchenClients`.
   * Apenas se conecta `BearPizzeria.Delivery`, envía `{"Tipo":"REPARTO"}\n` y el servidor la registra en `_deliveryClients`.
4. **Limpieza Automática de Desconexiones (`RemoverCliente`)**:
   * Si una consola de Cocina o Reparto se cierra o pierde red, `SocketServerService` captura el cierre del stream y remueve inmediatamente el `TcpClient` del `ConcurrentDictionary` correspondiente. Por su parte, tanto `Kitchen` como `Delivery` poseen un bucle infinito `while (true)` con reintento automático cada **5 segundos** (`await Task.Delay(5000)`).

---

### Parte 4: Funcionamiento Interno de `BearPizzeria.Kitchen` y `BearPizzeria.Delivery`

Los proyectos `BearPizzeria.Kitchen` y `BearPizzeria.Delivery` son aplicaciones de consola en C# (`.NET 10`) diseñadas como demonios operativos autónomos.

```mermaid
sequenceDiagram
    autonumber
    participant API as ⚙️ BearPizzeria.Api (:5050)
    participant DB as 🗄️ MySQL (5to_Todos)
    participant K as 👨‍🍳 BearPizzeria.Kitchen
    participant D as 🛵 BearPizzeria.Delivery

    Note over K,D: Fase 1: Conexión e Identificación (Handshake TCP)
    K->>API: Connect TCP :5050 + Send {"Tipo":"COCINA"}\n
    API->>API: Registra en _kitchenClients
    D->>API: Connect TCP :5050 + Send {"Tipo":"REPARTO"}\n
    API->>API: Registra en _deliveryClients

    Note over API,K: Fase 2: Despacho a Cocina y Cocción
    API->>K: Send {"TipoMensaje":"NuevoPedido","Id":105,"Detalle":{...}}\n
    K->>K: Imprime comanda en consola y simula horneado (Task.Delay 5000ms)
    K->>API: Send {"TipoMensaje":"ActualizarEstado","PedidoId":105,"Estado":"EnViaje"}\n

    Note over API,D: Fase 3: Transición en BD y Despacho Automático a Reparto
    API->>DB: UPDATE Pedidos SET Estado = 'EnViaje' WHERE Id = 105
    API->>D: Send {"TipoMensaje":"PedidoEnViaje","Id":105,"Detalle":{"Cliente":"...","Direccion":"..."}}\n

    Note over D,API: Fase 4: Traslado en Moto y Confirmación de Entrega
    D->>D: Asigna conductor, viaja al domicilio (Task.Delay 5000ms)
    D->>API: Send {"TipoMensaje":"ActualizarEstado","PedidoId":105,"Estado":"Entregado"}\n
    API->>DB: UPDATE Pedidos SET Estado = 'Entregado' WHERE Id = 105
```

#### Contratos Exactos de Mensajes TCP (JSON):

1. **Identificación Inicial (Cliente TCP -> API)**:
   ```json
   {"Tipo": "COCINA"}
   {"Tipo": "REPARTO"}
   ```
2. **Orden de Preparación (API -> Cocina)**:
   ```json
   {
     "TipoMensaje": "NuevoPedido",
     "Id": 105,
     "Detalle": {
       "Cliente": "Martín Gómez",
       "Direccion": "Mi casa: Av. Libertador 1234",
       "Items": [
         {
           "Pizza": "Muzzarella",
           "Tamano": "Grande",
           "Cantidad": 2,
           "PrecioUnitario": 4500.00,
           "Subtotal": 9000.00
         }
       ],
       "Total": 9000.00
     }
   }
   ```
3. **Notificación de Cambio de Estado (Cocina / Reparto -> API)**:
   ```json
   {"TipoMensaje": "ActualizarEstado", "PedidoId": 105, "Estado": "EnViaje"}
   {"TipoMensaje": "ActualizarEstado", "PedidoId": 105, "Estado": "Entregado"}
   ```
4. **Orden de Despacho a Domicilio (API -> Reparto)**:
   ```json
   {
     "TipoMensaje": "PedidoEnViaje",
     "Id": 105,
     "Detalle": {
       "Cliente": "Martín Gómez",
       "Direccion": "Mi casa: Av. Libertador 1234"
     }
   }
   ```

---

### Parte 5: Modelo de Persistencia y Diagrama Entidad-Relación en `MySQL`

Todas las operaciones de los proyectos convergen en la base de datos relacional `5to_Todos` en MySQL, cuyo esquema es garantizado al inicio por `script.sql` y verificado en caliente por `EnsureDatabaseSchemaAsync` en `BearPizzeria.Api/Program.cs`:

```mermaid
erDiagram
    Clientes ||--|| Usuarios : "1:1 (CASCADE)"
    Clientes ||--|| Carritos : "1:1 (CASCADE)"
    Clientes ||--o{ DireccionesCliente : "1:N (CASCADE)"
    Clientes ||--o{ Pedidos : "1:N (RESTRICT)"

    Carritos ||--o{ CarritoItems : "1:N (CASCADE)"
    Pizzas ||--o{ CarritoItems : "1:N (RESTRICT)"

    Pedidos ||--|{ PedidoPizzas : "1:N (CASCADE)"
    Pizzas ||--o{ PedidoPizzas : "1:N (RESTRICT)"

    Clientes {
        int Id PK
        varchar Nombre
        varchar Direccion
        varchar Telefono
        varchar Email UK
    }

    Usuarios {
        int Id PK
        int ClienteId FK, UK
        varchar Username UK
        varchar PasswordHash
        datetime FechaCreacion
        datetime UltimoAcceso
    }

    DireccionesCliente {
        int Id PK
        int ClienteId FK
        varchar Nombre
        varchar DireccionCompleta
        varchar Notas
        tinyint EsPrincipal
    }

    Pizzas {
        int Id PK
        varchar Nombre
        text Descripcion
        decimal Precio
        enum Tamano
        int Stock
    }

    Carritos {
        int Id PK
        int ClienteId FK, UK
        datetime FechaActualizacion
        decimal Total
    }

    CarritoItems {
        int Id PK
        int CarritoId FK
        int PizzaId FK
        enum Tamano
        int Cantidad
        decimal PrecioUnitario
        decimal Subtotal
    }

    Pedidos {
        int Id PK
        int ClienteId FK
        datetime FechaPedido
        enum Estado
        decimal Total
        varchar DireccionEntrega
    }

    PedidoPizzas {
        int Id PK
        int PedidoId FK
        int PizzaId FK
        enum Tamano
        int Cantidad
        decimal PrecioUnitario
        decimal Subtotal
    }
```

---

## 4. Resumen de Puertos, Protocolos y Proyectos de la Solución

| Proyecto (`.csproj`) | Tipo de Ejecutable | Puerto / Dirección | Protocolo | Rol dentro del Recorrido del Pedido |
| :--- | :--- | :--- | :--- | :--- |
| **`BearPizzeria.Mvc`** | Web App (`Microsoft.NET.Sdk.Web`) | `http://localhost:5082`<br>`https://localhost:7188` | HTTP/1.1 & HTTPS | Renderiza catálogo, formularios de login/registro, carrito, selector de direcciones y monitor en vivo (`tracker.js`). |
| **`BearPizzeria.Api`** | Web API (`Microsoft.NET.Sdk.Web`) | `http://localhost:5250`<br>`https://localhost:7225`<br>+ `TCP 0.0.0.0:5050` | HTTP REST JSON<br>+ Sockets TCP | Valida datos, encripta claves, firma JWT, administra el carrito y stock en MySQL y coordina los sockets TCP con Cocina y Reparto. |
| **`BearPizzeria.Kitchen`** | App Consola (`Microsoft.NET.Sdk`) | Conecta a `localhost:5050` | Socket TCP (`NDJSON`) | Recibe nuevos pedidos en estado `EnPreparacion`, simula los 5s de cocción y los promueve a `EnViaje`. |
| **`BearPizzeria.Delivery`** | App Consola (`Microsoft.NET.Sdk`) | Conecta a `localhost:5050` | Socket TCP (`NDJSON`) | Recibe pedidos en estado `EnViaje`, simula los 5s de viaje al domicilio del cliente y los promueve a `Entregado`. |
| **`MySQL Server`** | Motor de Base de Datos Relacional | `localhost:3306`<br>(BD: `5to_Todos`) | MySQL Wire Protocol | Persiste usuarios, direcciones, catálogo con stock, carritos activos e historial de pedidos. |

