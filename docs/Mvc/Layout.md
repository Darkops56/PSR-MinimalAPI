---

# 🛡️ 1. Arquitectura de Seguridad HTTP y Mitigación de Vulnerabilidades

Al habilitar endpoints HTTP REST para la comunicación entre el frontend ASP.NET Core MVC y el backend Minimal API, se implementan los siguientes controles de seguridad:

| Vulnerabilidad | Riesgo en e-Commerce | Estrategia de Mitigación en el Frontend / MVC / Backend |
| --- | --- | --- |
| **XSS (Cross-Site Scripting)** | Inyección de scripts maliciosos en descripciones o campos de perfil para robar tokens. | • Escape automático de HTML en Razor Views (`@Model.Descripcion`).<br><br>• Manipulación segura del DOM en módulos JavaScript (evitando `innerHTML` con datos de usuario).<br><br>• Implementación de **Content Security Policy (CSP)** vía cabeceras HTTP. |
| **CSRF (Cross-Site Request Forgery)** | Ejecución de compras o modificaciones de cuenta no autorizadas mediante enlaces externos. | • Inyección de `@Html.AntiForgeryToken()` en todos los formularios mutativos (`POST`).<br><br>• Cookies de autenticación `HttpOnly`, `SameSite=Lax` y `Secure`. |
| **Inyección de Parámetros / SQLi** | Manipulación de campos de búsqueda o IDs de producto. | • Validación estricta con **FluentValidation** en el backend y DataAnnotations en ViewModels.<br><br>• Uso de Entity Framework Core parametrizado (LINQ). |
| **CORS (Cross-Origin Resource Sharing)** | Peticiones HTTP no autorizadas desde dominios de terceros hacia la API de la pizzería. | • **Aislamiento Estricto por Puertos**: Configuración de `AllowMvcClient` en la API permitiendo exclusivamente orígenes `http://localhost:5082` y `https://localhost:7188`. |
| **Robo de Credenciales** | Almacenamiento inseguro de contraseñas de clientes. | • **Encriptación Fuerte**: Hashing criptográfico estándar PBKDF2 con SHA-256 (100.000 iteraciones + salt aleatorio de 128 bits).<br><br>• Separación de entidades `Clientes` (contacto/envío) y `Usuarios` (credenciales). |
| **Pérdida de Datos en Carrito** | Pérdida de productos seleccionados al recargar o cambiar de pestaña. | • **Persistencia 100% en Base de Datos**: Tablas `Carritos` y `CarritoItems` vinculadas al cliente autenticado. Cero uso de `LocalStorage` volátil. |

---

## 🔒 2. Aislamiento de Red y Puertos

```
┌────────────────────────────────────────────────────────┐
│ 🌐 FRONTEND: BearPizzeria.Mvc                          │
│ Puertos: http://localhost:5082 | https://localhost:7188│
└────────────────────────┬───────────────────────────────┘
                         │ 🔒 HTTP REST (CORS Estricto & BFF)
                         ▼
┌────────────────────────────────────────────────────────┐
│ ⚙️ BACKEND: BearPizzeria.Api                            │
│ Puertos: http://localhost:5250 | https://localhost:7225│
└──────────────┬──────────────────────────┬──────────────┘
               │ 🗄️ MySQL (EF Core)       │ 🔌 Sockets TCP (Puerto 5050)
               ▼                          ▼
┌────────────────────────┐      ┌────────────────────────┐
│ Tablas:                │      │ 👨‍🍳 Cocina Automatizada│
│ • Clientes             │      │  (BearPizzeria.Kitchen)│
│ • Usuarios             │      │                        │
│ • Pizzas               │      │ 🛵 Reparto             │
│ • Carritos             │      │  (BearPizzeria.Delivery)│
│ • CarritoItems         │      └────────────────────────┘
│ • Pedidos & PedidoPizzas│
└────────────────────────┘
```

---

# 🎨 3. Tokens del Sistema de Diseño (Dark Mode Elegante)

```css
:root {
  /* Canvas y Superficies */
  --bg-canvas: #0b0b0e;
  --bg-surface: #141419;
  --bg-surface-elevated: #1e1e26;
  --bg-surface-hover: #282833;
  --bg-overlay: rgba(11, 11, 14, 0.85);

  /* Glassmorphism */
  --glass-bg: rgba(20, 20, 25, 0.75);
  --glass-border: rgba(255, 255, 255, 0.08);
  --glass-blur: blur(16px);
  --glass-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.45);

  /* Acentos de Marca */
  --accent-primary: #e53935;          /* Rojo Napolitano */
  --accent-primary-hover: #f44336;
  --accent-primary-glow: rgba(229, 57, 53, 0.35);
  --accent-secondary: #f59e0b;        /* Dorado Horno */
  --accent-secondary-hover: #fbbf24;

  /* Semáforo de Stock */
  --stock-green: #10b981;             /* stock >= 6 (En Stock) */
  --stock-yellow: #f59e0b;            /* 0 < stock < 6 (Últimas unidades) */
  --stock-red: #ef4444;               /* stock === 0 (Agotado) */

  /* Tipografías */
  --font-display: 'Outfit', sans-serif;
  --font-body: 'Plus Jakarta Sans', sans-serif;
}
```

---

# 🍕 4. Tamaños de Pizza y Factores Multiplicadores

El sistema permite configurar dinámicamente el tamaño de cualquier pizza del catálogo aplicando los factores de cálculo sobre el precio base:

| Tamaño | Porciones Estimadas | Factor Multiplicador | Ejemplo (Base $5.000) |
| --- | --- | --- | --- |
| **Personal** | 4 porciones | `0.70` (70%) | $3.500,00 |
| **Mediana** | 6 porciones | `0.85` (85%) | $4.250,00 |
| **Grande** | 8 porciones | `1.00` (Base) | $5.000,00 |
| **Familiar** | 12 porciones | `1.30` (130%) | $6.500,00 |

---

# 🏗️ 5. Estructura de Componentes y Vistas MVC

```
src/BearPizzeria.Mvc/
├── Controllers/
│   ├── HomeController.cs        # Catálogo, Banners y Home View
│   ├── CartController.cs        # Vista de Carrito y Checkout persistido
│   ├── AuthController.cs        # Registro, Login y Logout con cookies
│   └── ProfileController.cs     # Perfil, Historial y StatusTracker en vivo
├── Models/
│   ├── ViewModels/
│   │   ├── HomeViewModel.cs
│   │   ├── CartViewModel.cs
│   │   ├── ProfileViewModel.cs
│   │   └── AuthViewModels.cs
│   └── DTOs/
│       └── ApiDTOs.cs
├── Services/
│   ├── IBearApiClient.cs        # Cliente HTTP tipado hacia la Minimal API
│   └── BearApiClient.cs
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml              # Layout maestro Dark Mode
│   │   ├── _Header.cshtml              # Sticky Header (Desktop + Mobile)
│   │   ├── _Footer.cshtml              # Footer informativo con red de puertos
│   │   ├── _HamburgerDrawer.cshtml     # Menú lateral móvil
│   │   ├── _MobileBottomNav.cshtml     # Barra inferior de acceso rápido
│   │   ├── _CustomizerModal.cshtml     # Modal táctil de personalización de pizzas
│   │   ├── _AuthGuardModal.cshtml      # Modal de intercepción de invitados
│   │   └── _ToastNotifications.cshtml  # Notificaciones flotantes
│   ├── Home/
│   │   ├── Index.cshtml                # Home
│   │   ├── _HeroSlider.cshtml          # Carrusel interactivo banner
│   │   ├── _CategoryBar.cshtml         # Chips de filtrado dinámico
│   │   └── _ProductGrid.cshtml         # Grilla responsiva de cards de pizza
│   ├── Cart/
│   │   └── Index.cshtml                # Pantalla completa de carrito y checkout
│   ├── Profile/
│   │   └── Index.cshtml                # Historial de pedidos + Widget de Tracking en vivo
│   └── Auth/
│       ├── Login.cshtml
│       └── Register.cshtml
└── wwwroot/
    ├── css/
    │   ├── tokens.css                  # Tokens del sistema de diseño
    │   ├── layout.css                  # Estructura base, grid, header, drawer, bottom nav
    │   ├── components.css              # Cards, badges, sliders, modales, tracker
    │   └── animations.css              # Keyframes, transiciones y micro-interacciones
    └── js/
        ├── app.js                      # Inicializador principal y orquestador
        └── modules/
            ├── api.js                  # Cliente fetch aislado hacia BearPizzeria.Api
            ├── auth.js                 # Manejo de identidad y sesión
            ├── cart.js                 # Manejador del carrito persistido en BD
            ├── customizer.js           # Lógica modal de personalización y tamaños
            ├── tracker.js              # Polling y actualización del StatusTracker
            └── ui.js                   # Control de Drawer, modales, toasts y filtros
```

---

# 📦 6. Ciclo de Vida del Pedido y Tracking en Vivo

```
[🔥 1. En Preparación] ───────── [🛵 2. En Camino] ───────── [✅ 3. Entregado]
  Cocina Automatizada               Reparto                 Cliente Satisfecho
  (Mensaje TCP Sockets)          (Mensaje TCP Sockets)     (Notificación en UI)
```

El widget `StatusTracker` consulta periódicamente mediante **Adaptive Polling** al endpoint `GET /api/pedidos/{id}` actualizando la barra de progreso sin requerir recargar la página web.