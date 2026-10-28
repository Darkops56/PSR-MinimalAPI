<h1 align="center">E.T. Nº12 D.E. 1º "Libertador Gral. José de San Martín"</h1>
<p align="center">
  <img src="https://et12.edu.ar/imgs/computacion/vamoaprogramabanner.png">
</p>

# 🍕 BearPizzeria — Sistema Distribuido de Pizzería Digital

**BearPizzeria** es una plataforma distribuida desarrollada en **.NET 10** y **MySQL** que **gestiona el recorrido completo de un pedido de pizza**: desde el instante en que el cliente explora el menú interactivo en la web, personaliza el tamaño de sus pizzas y confirma su carrito, hasta que la orden se procesa automáticamente en los hornos de la **Cocina Automatizada**, se despacha en moto a través del módulo de **Reparto Automatizado** y se entrega en la puerta de su domicilio, todo monitoreado en vivo y en tiempo real desde el navegador.

El ecosistema combina una aplicación web **ASP.NET Core MVC (`BearPizzeria.Mvc`)**, un núcleo transaccional **Minimal API (`BearPizzeria.Api`)** con seguridad criptográfica (`PBKDF2` + `JWT`), persistencia relacional en **MySQL (`Entity Framework Core`)** y dos demonios operativos independientes (**`BearPizzeria.Kitchen`** y **`BearPizzeria.Delivery`**) interconectados mediante **Sockets TCP bidireccionales**.

---

## 🧭 El Funcionamiento de la App Paso por Paso (Recorrido Completo del Pedido)

A continuación se detalla de principio a fin cómo opera **BearPizzeria** en cada etapa del recorrido de un pedido:

### Paso 1: Exploración del Catálogo y Control Visual de Stock
1. El cliente ingresa desde su navegador a la tienda web (`BearPizzeria.Mvc` en `http://localhost:5082`).
2. `HomeController` consulta mediante su cliente HTTP tipado (`PizzaApiService`) el endpoint `GET /api/pizzas` de `BearPizzeria.Api`.
3. La aplicación renderiza el catálogo en modo oscuro (*Dark Mode*) con un carrusel de promociones, buscador instantáneo, filtros por categoría y tarjetas de producto que informan el **stock en tiempo real** mediante indicadores semafóricos (Disponible, Últimas unidades o Sin stock).

### Paso 2: Registro, Autenticación Criptográfica y Sesión Híbrida
1. Cuando el cliente desea armar su pedido, accede a `/Auth/Register` (si es nuevo) o `/Auth/Login`.
2. Al registrarse (`POST /api/auth/register`), el backend ejecuta una **transacción atómica en MySQL** que:
   * Crea el perfil en la tabla `Clientes`.
   * Encripta la contraseña utilizando **`PBKDF2-SHA256` con 100.000 iteraciones y Salt aleatorio de 128 bits** (`PasswordHasherService`), guardándola en `Usuarios`.
   * Inicializa automáticamente un **`Carrito` persistido en base de datos** (relación `1:1`) exclusivo para ese cliente.
3. `TokenService` firma un token **JWT (`HMAC-SHA256`)** y `BearPizzeria.Mvc` emite la cookie segura **`BearPizzeria.Auth` (`HttpOnly`)**, sincronizando la sesión tanto para la navegación en el servidor como para las peticiones AJAX del navegador.

### Paso 3: Personalización de Tamaños y Carrito 100% en Base de Datos
1. Al pulsar *"Personalizar / Agregar"* sobre una pizza, se despliega el modal interactivo (`_CustomizerModal.cshtml` controlado por `customizer.js`).
2. El cliente elige la cantidad y el **tamaño de la pizza**, cuyo precio se escala dinámicamente sobre el precio base:
   * **Personal (4 porciones)**: Multiplicador `0.70x`
   * **Mediana (6 porciones)**: Multiplicador `0.85x`
   * **Grande (8 porciones)**: Multiplicador `1.00x` *(Precio Base)*
   * **Familiar (12 porciones)**: Multiplicador `1.30x`
3. Cada vez que el cliente agrega una pizza (`POST /api/carrito/{clienteId}/items`), modifica cantidades (`PUT /api/carrito/items/{itemId}`) o elimina un renglón, el impacto se persiste directamente en las tablas `Carritos` y `CarritoItems` de MySQL, verificando siempre que la cantidad acumulada no supere el `Stock` real de la pizza.

### Paso 4: Selección de Domicilio y Confirmación Transaccional (Checkout)
1. En la vista `/Cart`, el cliente revisa el desglose de su compra y selecciona a qué domicilio enviarla usando el gestor lateral de direcciones (`_AddressSidebar.cshtml` / `addressDrawer.js`), que permite agendar múltiples domicilios (*"Mi casa"*, *"Trabajo"*, etc.) en la tabla `DireccionesCliente`.
2. Al presionar **"Confirmar Pedido"** (`POST /Cart/Checkout` -> `POST /api/carrito/{clienteId}/checkout`), `BearPizzeria.Api` abre una **transacción ACID en MySQL**:
   * Verifica por última vez que todas las pizzas del carrito tengan stock suficiente y **descuenta las unidades** de la tabla `Pizzas`.
   * Fija la dirección definitiva de entrega en el pedido.
   * Inserta la cabecera en la tabla `Pedidos` con estado inicial **`EnPreparacion`** y vuelca los ítems hacia `PedidoPizzas` congelando su `PrecioUnitario` y `Subtotal` históricos.
   * Vacía los ítems del `Carrito` del cliente y confirma (`Commit`) la transacción.
3. Sin bloquear la respuesta HTTP, la API escribe el nuevo pedido en una cola asíncrona en memoria (`Channel<Pedido>`) y redirige al cliente hacia `/Profile`.

### Paso 5: Despacho por Socket TCP a la Cocina Automatizada (`BearPizzeria.Kitchen`)
1. El servicio en segundo plano `SocketServerService` (que escucha en el puerto **TCP `5050`**) lee inmediatamente el pedido de la cola `Channel<Pedido>`.
2. Serializa los datos del pedido en formato JSON delimitado por salto de línea (`\n`) con `"TipoMensaje": "NuevoPedido"` y lo transmite por socket TCP a todas las consolas conectadas de **`BearPizzeria.Kitchen`**.
3. La consola de **Cocina Automatizada** imprime la comanda completa en pantalla (cliente, dirección, pizzas, tamaños y cantidades) y simula el horneado durante **5 segundos**.
4. Al terminar la cocción, `BearPizzeria.Kitchen` envía de regreso por el socket TCP el mensaje:
   ```json
   {"TipoMensaje": "ActualizarEstado", "PedidoId": 105, "Estado": "EnViaje"}
   ```

### Paso 6: Transición a `EnViaje` y Derivación Automática al Reparto (`BearPizzeria.Delivery`)
1. `SocketServerService` recibe el mensaje de la cocina, abre un scope de base de datos y actualiza el estado del pedido en MySQL a **`EnViaje`**.
2. Al detectar que el nuevo estado es `EnViaje`, el servidor de sockets **encadena automáticamente el despacho logístico**: construye el mensaje `"TipoMensaje": "PedidoEnViaje"` con el nombre del cliente y el domicilio de entrega, y lo transmite por socket TCP hacia **`BearPizzeria.Delivery`**.
3. La consola de **Reparto Automatizado** recibe la orden, asigna un repartidor en moto y simula el viaje hacia el domicilio durante **5 segundos**.
4. Al llegar a destino, `BearPizzeria.Delivery` transmite por el socket TCP la confirmación final:
   ```json
   {"TipoMensaje": "ActualizarEstado", "PedidoId": 105, "Estado": "Entregado"}
   ```
5. `SocketServerService` actualiza la tabla `Pedidos` en MySQL al estado final **`Entregado`**.

### Paso 7: Seguimiento en Vivo (Live Tracker), Historial y Repetición Inteligente
1. Mientras Cocina y Reparto trabajan por sockets TCP, el navegador del cliente en `/Profile` o `/Profile/Trackers` ejecuta el módulo `tracker.js`, que consulta cada **3 segundos** el endpoint `GET /api/pedidos/mis-pedidos-activos`.
2. Cada vez que el estado cambia en la base de datos, la barra de progreso se anima fluidamente sin recargar la página (`0% 🔥 Cocina` -> `50% 🛵 Reparto` -> `100% ✅ Entregado`) y despliega notificaciones *Toast* informando el avance.
3. En `/Profile/Orders`, el cliente puede explorar todo su historial paginado, filtrar por fecha y pulsar **"Repetir Pedido"** (`POST /api/pedidos/{id}/repetir`): el sistema verifica el stock actual de cada pizza histórica, agrega las unidades disponibles al carrito y, si alguna pizza tiene menos stock del solicitado originalmente, abre un modal detallando el ajuste parcial.

---

## 📚 Documentación Técnica y Diagramas Desestructurados (`docs/`)

Para comprender al **100%** la ingeniería interna del sistema, dentro del directorio [`docs/`](docs/) se encuentran los documentos de análisis y diseño que **desestructuran cada diagrama parte por parte**:

| Documento en `docs/` | Qué Documenta y Cómo lo Desestructura |
| :--- | :--- |
| 🏛️ **[Diagrama de Arquitectura](docs/DiagramaDeArquitectura.md)** | Explica la topología distribuida de los **4 proyectos .NET 10** (`Mvc`, `Api`, `Kitchen`, `Delivery`) y **MySQL**. Desestructura parte por parte: la comunicación híbrida SSR/AJAX, la seguridad criptográfica (`PBKDF2` + `JWT`), la cola asíncrona `Channel<Pedido>`, el servidor de sockets TCP (`SocketServerService` en puerto `5050`), las tramas JSON de Cocina y Reparto y el Diagrama Entidad-Relación (DER). |
| 🎯 **[Diagrama de Casos de Uso](docs/DiagramaDeCasosDeUso.md)** | Desarrolla el modelado funcional UML completo desde el relevamiento del problema y los **20 Requerimientos Funcionales (`RF-01` a `RF-20`)** hasta el Diagrama General con límite del sistema y los 3 actores externos (`Cliente`, `Cocina Automatizada`, `Reparto Automatizado`). Desestructura el diagrama en 4 subsistemas explicando cada relación obligatoria **`<<include>>`**, cada extensión condicional **`<<extend>>`**, las fichas de especificación y la **Matriz de Trazabilidad**. |
| 🧩 **[Diagrama de Clases](docs/DiagramaDeClases.md)** | Modela la estructura estática UML completa del sistema (Nombre, Atributos con visibilidad y tipo, Operaciones/Responsabilidades). Explica por qué existen las clases asociativas `CarritoItem` y `PedidoPizza`, la lectura exacta de todas las **multiplicidades (`1`, `0..*`, `1..*`)**, las composiciones y la desestructuración en 5 bloques: Identidad/Domicilios, Catálogo/Carrito, Pedidos Históricos, Servicios de Infraestructura/Sockets y Controladores/Servicios MVC. |
| 🌐 **[Estructura y Rutas MVC](docs/Mvc/EstructuraMVC.md)** | Guía exhaustiva de enrutamiento de `BearPizzeria.Mvc`: detalla para cada una de las 15 URLs qué controlador la recibe, qué servicio y endpoint de la API consume, qué vista Razor construye y cómo operan los módulos JavaScript ES6. |
| 🎨 **[Layout y Diseño de Interfaz](docs/Mvc/Layout.md)** | Especificación de la interfaz de usuario, estructura de vistas parciales, modales interactivos y sistema de estilos de `BearPizzeria.Mvc`. |
| 🔐 **[Autenticación Híbrida: Cookies + JWT](docs/AutenticacionHibrida.md)** | Explica la evolución desde un esquema de solo JWT hacia el modelo de sesión híbrido: secuencia cronológica de generación (JWT primero, Cookie después), doble canal simultáneo (Server C# / Client JS Fetch) y mitigación de XSS/CSRF. |

---

## 🗂️ Estructura del Proyecto

```text
PSR-MinimalAPI/
├── docs/
│   ├── DiagramaDeArquitectura.md      ← Arquitectura distribuida desestructurada al 100%
│   ├── DiagramaDeCasosDeUso.md        ← Relevamiento, RFs, Casos de Uso UML y Trazabilidad
│   ├── DiagramaDeClases.md            ← Modelo estructural de Clases UML y multiplicidades
│   ├── AutenticacionHibrida.md        ← Flujo y arquitectura de Cookies HttpOnly + JWT
│   └── Mvc/
│       ├── EstructuraMVC.md           ← Desglose URL por URL del Frontend MVC y JS
│       └── Layout.md                  ← Arquitectura visual y componentes Razor
├── src/
│   ├── BearPizzeria.slnx              ← Solución .NET 10
│   ├── BearPizzeria.Api/              ← Backend Minimal API + EF Core + Servidor Sockets TCP (:5250 / :5050)
│   ├── BearPizzeria.Mvc/              ← Frontend Web ASP.NET Core MVC + Razor + ES6 Modules (:5082)
│   ├── BearPizzeria.Kitchen/          ← Worker Consola C# — Cocina Automatizada (Cliente TCP :5050)
│   └── BearPizzeria.Delivery/         ← Worker Consola C# — Reparto Automatizado (Cliente TCP :5050)
├── apuntes/                           ← Material teórico de cátedra (Modelado UML, Casos de Uso y Clases)
├── script.sql                         ← Script DDL y Seed inicial de Base de Datos MySQL (5to_Todos)
└── README.md                          ← Guía general del recorrido del pedido y puesta en marcha
```

---

## 🚀 Pre-requisitos e Instalación Paso a Paso

### Pre-requisitos
* **SDK .NET 10** (`dotnet --version`)
* **MySQL Server 8.0+** corriendo en `localhost:3306`

### 1. Inicializar la Base de Datos MySQL
Ejecutar el script [`script.sql`](script.sql) para crear la base de datos `5to_Todos`, sus 8 tablas relacionales y el catálogo inicial de pizzas:

```bash
mysql -u 5to_agbd -p < script.sql
```

### 2. Verificar Configuración del Backend (`BearPizzeria.Api`)
En `src/BearPizzeria.Api/appsettings.Development.json` (o `appsettings.json`), asegurarse de contar con la cadena de conexión a MySQL y la clave de firma JWT (mínimo 32 caracteres):

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
  },
  "Jwt": {
    "Key": "BearPizzeria_SuperSecretKey_2026_ET12_Computacion!"
  }
}
```

### 3. Ejecutar los 4 Proyectos del Ecosistema
Abrir 4 terminales en la raíz del repositorio y levantar cada servicio en el siguiente orden:

#### Terminal 1 — Backend Central y Servidor de Sockets TCP (`BearPizzeria.Api`)
```bash
dotnet run --project src/BearPizzeria.Api
```
* **API HTTP**: `http://localhost:5250` (Documentación interactiva Scalar en `http://localhost:5250/scalar`)
* **Servidor Sockets TCP**: Escuchando en puerto `5050`

#### Terminal 2 — Cocina Automatizada (`BearPizzeria.Kitchen`)
```bash
dotnet run --project src/BearPizzeria.Kitchen
```
* Se conecta a `localhost:5050` y envía el handshake `{"Tipo":"COCINA"}`.

#### Terminal 3 — Reparto Automatizado (`BearPizzeria.Delivery`)
```bash
dotnet run --project src/BearPizzeria.Delivery
```
* Se conecta a `localhost:5050` y envía el handshake `{"Tipo":"REPARTO"}`.

#### Terminal 4 — Tienda Web Frontend (`BearPizzeria.Mvc`)
```bash
dotnet run --project src/BearPizzeria.Mvc
```
* Abrir en el navegador: **`http://localhost:5082`**

---

## 🔌 Resumen de Endpoints de `BearPizzeria.Api`

| Categoría | Método | Ruta | Auth JWT | Descripción |
| :--- | :---: | :--- | :---: | :--- |
| **Autenticación** | `POST` | `/api/auth/register` | Libre | Registra `Cliente`, `Usuario` (`PBKDF2`) y `Carrito` inicial en transacción ACID. |
| **Autenticación** | `POST` | `/api/auth/login` | Libre | Valida credenciales y retorna perfil + token `JWT`. |
| **Autenticación** | `GET` | `/api/auth/me/{clienteId}` | Sí | Valida sesión activa y refresca token `JWT`. |
| **Catálogo** | `GET` | `/api/pizzas` | Libre | Lista el catálogo completo de pizzas con precios y stock. |
| **Catálogo** | `GET` | `/api/pizzas/{id}` | Libre | Obtiene el detalle de una pizza específica. |
| **Carrito (BD)** | `GET` | `/api/carrito/{clienteId}` | Sí | Obtiene el carrito persistido del cliente con sus ítems. |
| **Carrito (BD)** | `POST` | `/api/carrito/{clienteId}/items` | Sí | Agrega o acumula una pizza personalizada validando stock. |
| **Carrito (BD)** | `PUT` | `/api/carrito/items/{itemId}` | Sí | Modifica la cantidad de un ítem del carrito validando stock. |
| **Carrito (BD)** | `DELETE` | `/api/carrito/items/{itemId}` | Sí | Elimina un ítem específico del carrito. |
| **Carrito (BD)** | `DELETE` | `/api/carrito/{clienteId}/vaciar` | Sí | Vacía todos los renglones del carrito del cliente. |
| **Checkout** | `POST` | `/api/carrito/{clienteId}/checkout` | Sí | Descuenta stock, crea el `Pedido` (`EnPreparacion`), vacía el carrito y notifica a Cocina por TCP. |
| **Pedidos & Tracking** | `GET` | `/api/pedidos/mis-pedidos-activos` | Sí | Retorna los pedidos activos del cliente para el polling en vivo de `tracker.js`. |
| **Pedidos & Tracking** | `GET` | `/api/pedidos/mi-pedido-activo` | Sí | Retorna el pedido activo más reciente del cliente. |
| **Pedidos & Tracking** | `GET` | `/api/pedidos/cliente/{clienteId}` | Sí | Historial completo de pedidos del cliente ordenados por fecha. |
| **Pedidos & Tracking** | `GET` | `/api/pedidos/{id}` | Libre | Consulta el detalle completo de un pedido por su ID. |
| **Pedidos & Tracking** | `POST` | `/api/pedidos/{id}/repetir` | Libre | Vuelca un pedido histórico al carrito actual comprobando stock disponible. |
| **Pedidos & Tracking** | `PATCH` | `/api/pedidos/{id}/estado` | Libre | Avanza manualmente el estado del pedido y dispara socket a Reparto si pasa a `EnViaje`. |
| **Clientes & Domicilios** | `GET` / `PUT` | `/api/clientes/{id}` | Sí (`PUT`) | Consulta o actualiza los datos personales y de usuario del cliente. |
| **Clientes & Domicilios** | `GET` / `POST` | `/api/clientes/{clienteId}/direcciones` | Libre | Lista o agrega nuevas direcciones en la agenda del cliente. |
| **Clientes & Domicilios** | `PUT` / `DELETE` | `/api/clientes/{clienteId}/direcciones/{id}` | Libre | Actualiza o elimina una dirección agendada. |
| **Clientes & Domicilios** | `PUT` | `/api/clientes/{clienteId}/direcciones/{id}/principal` | Libre | Establece una dirección como predeterminada (`EsPrincipal = true`). |
