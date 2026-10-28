---

# Plan de Trabajo por Etapas: Sistema Distribuido de Pizzería Digital

Este plan de trabajo detalla el proceso para migrar la estructura base del repositorio actual (`TodoPSR`, basado en Minimal APIs y Dapper) hacia un ecosistema distribuido, asincrónico y desacoplado que cumpla con los requerimientos de la pizzería automatizada.

---

## 📊 Resumen del Cronograma

| Fase | Descripción | Duración Estimada |
| --- | --- | --- |
| **Fase 1** | Indagación y Diseño Técnico (Documentación) | 3 Semanas |
| **Fase 2** | Producción: Refactorización del Core y Backend REST | 1.5 Semanas |
| **Fase 3** | Producción: Red Distribuida y Servicios Internos (Sockets) | 1.5 Semanas |
| **Fase 4** | Producción: Módulo Cliente C# y Resiliencia | 1 Semana |
| **Fase 5** | Estabilización, Demo Funcional y Defensa Oral | 1 Semana |

---

## 🔍 Detalle de las Fases y Tareas

### Fase 1: Indagación y Diseño Técnico

**Enfoque:** Construcción de la base analítica y arquitectónica obligatoria antes de codificar la primera línea.

* [ ] **Diseño de Modelos de Casos de Uso:**
* Identificar y diagramar las interacciones de los 4 actores principales: *Cliente Hambriento*, *Backend*, *Cocina Automatizada* y *Reparto*.


* [ ] **Esquema Jerárquico y de Responsabilidades:**
* Definir formalmente los límites de cada componente (qué lógica se procesa en el entorno HTTP de la Minimal API y qué tareas se delegan hacia los servicios por red).


* [ ] **Diagramas de Clases y de Secuencia:**
* Modelar la estructura interna de las entidades principales (`Pizza`, `Cliente`, `Pedido`).
* Trazar la línea de tiempo del ciclo de vida de un pedido a través de los componentes del sistema.


* [ ] **Esquema de Arquitectura Distribuida:**
* Mapear la red detallando los protocolos de comunicación: Cliente $\rightarrow$ Backend mediante **REST (HTTP)**; Backend $\leftrightarrow$ Cocina/Reparto mediante **Sockets TCP**.


* [ ] **Informe de Manejo de Errores:**
* Clasificar fallos potenciales propios de entornos distribuidos (caídas de sockets, timeouts de red, payloads corruptos) y proponer acciones tempranas de mitigación.



**🎯 Entregable de la Fase 1:** Carpeta de diseño técnico inicial con diagramas y matrices de error completas.

---

### Fase 2: Producción - Refactorización del Core y Backend REST

**Enfoque:** Evolucionar la base de código actual (`Program.cs`, `Todo.cs`) reemplazando el contexto de "Tareas/Todos" por las entidades reales de la pizzería.

* [ ] **Definición del Modelo de Datos y Entidades:**
* Crear clases independientes en C# para `Pizza`, `Cliente` (siguiendo el requerimiento de no gestionar contraseñas) y `Pedido`.
* Implementar el enumerador (`enum`) para los 4 estados obligatorios del ciclo de vida del pedido:
1. *Espera de confirmación*
2. *En preparación*
3. *En viaje*
4. *Entregado*




* [ ] **Evolución de la Capa de Datos (Dapper):**
* Adaptar la interfaz aislada (`IADO.cs`) y su implementación concreta (`AdoDapper.cs`) heredadas de la base del repositorio.
* Reemplazar los métodos genéricos por operaciones asincrónicas específicas para insertar nuevos pedidos y actualizar sus estados en la base de datos relacional (MySQL).


* [ ] **Exposición de Endpoints de la Minimal API (`Program.cs`):**
* Configurar las rutas REST principales del negocio:
* `POST /api/pedidos` (Recibe la orden estructurada del cliente, estado inicial: *Espera de confirmación*).
* `GET /api/pedidos/{id}` (Permite consultar el estado actual del pedido en tiempo real).


* Integrar **Swagger** en la API para facilitar la posterior exploración y pruebas.



**🎯 Entregable de la Fase 2:** Backend REST funcional, con persistencia asincrónica mediante Dapper.

---

### Fase 3: Producción - Red Distribuida y Servicios Internos (Sockets)

**Enfoque:** Configuración de la red secundaria utilizando sockets para la comunicación en tiempo real con las áreas internas encargadas del pedido.

* [ ] **Servidor de Sockets en el Backend:**
* Implementar un servicio o tarea en segundo plano (`BackgroundService` o `Task` asincrónica dedicada) dentro del mismo proceso de la Minimal API que levante un `TcpListener`.
* Gestionar las conexiones entrantes de manera no bloqueante utilizando `AcceptTcpClientAsync`.


* [ ] **Desarrollo del Módulo de Cocina:**
* Crear una aplicación de consola en C# independiente.
* Implementar un `TcpClient` para conectarse al backend, escuchar las notificaciones de nuevos pedidos pendientes de cocina y procesar su preparación simulada.


* [ ] **Desarrollo del Módulo de Reparto:**
* Crear una aplicación de consola en C# independiente conectada por sockets TCP, encargada de recibir las alertas de los pedidos cuyo estado cambie a "En viaje".


* [ ] **Diseño del Protocolo de Mensajería:**
* Estructurar el formato de los mensajes transmitidos por el canal de sockets (por ejemplo, tramas de strings en formato JSON que finalicen con un delimitador como `\n` para evitar solapamientos en el buffer).



**🎯 Entregable de la Fase 3:** Servicios autónomos de Cocina y Reparto coordinados con el Backend central a través de sockets funcionales.

---

### Fase 4: Producción - Módulo Cliente C# y Resiliencia

**Enfoque:** Construcción de la interfaz de consumo del usuario final e implementación de la tolerancia a fallos en la red distribuida.

* [ ] **Desarrollo del Módulo Cliente:**
* Crear una aplicación en C# independiente que actúe como el consumidor de la API.
* Utilizar `HttpClient` para enviar solicitudes asincrónicas enviando datos estructurados (JSON) y recibir las confirmaciones del backend.


* [ ] **Blindaje del Sistema (Async/Await + Try-Catch):**
* Asegurar que todas las llamadas de red (tanto peticiones HTTP en el cliente como lectura/escritura de streams de sockets en los módulos internos) estén debidamente protegidas.
* *Mitigación crítica:* Validar que si la Cocina o el Reparto sufren una desconexión abrupta, el Backend capture la excepción de socket, registre el log correspondiente y mantenga la Minimal API operativa respondiendo de forma clara al cliente.


* [ ] **Pruebas de Endpoints:**
* Crear una colección de pruebas en **Postman** para verificar que los endpoints de la API respondan adecuadamente ante payloads válidos e inválidos.



**🎯 Entregable de la Fase 4:** Módulo Cliente integrado y arquitectura protegida contra interrupciones en la infraestructura de red.

---

### Fase 5: Estabilización, Demo Funcional y Defensa Oral

**Enfoque:** Control de calidad final del ecosistema distribuido y preparación de los requisitos de evaluación.

* [ ] **Simulaciones de Error en Vivo (Inyección de Fallos):**
* Ejecutar pruebas extremas de extremo a extremo: realizar el flujo feliz completo (Cliente $\rightarrow$ API $\rightarrow$ Cocina $\rightarrow$ Reparto).
* Provocar intencionalmente la caída del servicio de cocina a mitad de un proceso para demostrar a los evaluadores la robustez del manejo de excepciones implementado.


* [ ] **Consolidación de la Documentación Técnica Final:**
* Actualizar los diagramas de clases, secuencia y arquitectura de la Fase 1 con los ajustes técnicos que se hayan adoptado durante la etapa de código.


* [ ] **Preparación de la Exposición Oral:**
* Estructurar el marco de la defensa del proyecto, haciendo foco en los desafíos encontrados en la programación asincrónica (`async/await`), la persistencia de datos relacionales, el uso de sockets y la justificación técnica de la arquitectura distribuida seleccionada.



**🎯 Entregable de la Fase 5:** Repositorio finalizado con código distribuido estable, documentación actualizada y guion técnico listo para la demo funcional.