-- ============================================================================
-- USERS.sql — BearPizzeria
-- Actores del sistema con permisos mínimos y requeridos en la base de datos
-- ============================================================================
--
-- ACTORES:
--   1. admin_app    → admin (API)       → CRUD completo en todas las tablas
--   2. cliente_app    → Cliente Hambriento  → SELECT en Pizzas, INSERT/SELECT en Clientes y Pedidos
--   3. cocina_app     → Cocina Automatizada → SELECT/UPDATE en Pedidos
--   4. reparto_app    → Reparto             → SELECT/UPDATE en Pedidos
--
-- Uso: mysql -u root -p < bd/USERS.sql
-- ============================================================================

USE mysql;

-- ============================================================================
-- 1. admin (API)
--    Necesita: INSERT, SELECT, UPDATE, DELETE en todas las tablas
--    Permisos mínimos: DDL no necesita (la BD se crea con DDL.sql),
--    pero sí necesita DML completo.
-- ============================================================================
CREATE USER IF NOT EXISTS 'admin_app'@'localhost'
    IDENTIFIED BY 'Back3nd_P55!';

GRANT SELECT, INSERT, UPDATE, DELETE ON `5to_Todos`.`Clientes` TO 'admin_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON `5to_Todos`.`Pizzas`   TO 'admin_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON `5to_Todos`.`Pedidos`  TO 'admin_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON `5to_Todos`.`PedidoPizzas` TO 'admin_app'@'localhost';

-- ============================================================================
-- 2. CLIENTE HAMBRIENTO
--    Necesita: SELECT en Pizzas (catálogo), INSERT/SELECT en Clientes
--    (registrarse/ver su perfil), INSERT/SELECT en Pedidos (crear/ver pedidos).
--    NO necesita UPDATE ni DELETE (lo maneja el backend internamente).
-- ============================================================================
CREATE USER IF NOT EXISTS 'cliente_app'@'localhost'
    IDENTIFIED BY 'Cl1ente_App!';

GRANT SELECT ON `5to_Todos`.`Pizzas`   TO 'cliente_app'@'localhost';
GRANT SELECT, INSERT ON `5to_Todos`.`Clientes` TO 'cliente_app'@'localhost';
GRANT SELECT, INSERT ON `5to_Todos`.`Pedidos`  TO 'cliente_app'@'localhost';
GRANT SELECT          ON `5to_Todos`.`PedidoPizzas` TO 'cliente_app'@'localhost';

-- ============================================================================
-- 3. COCINA AUTOMATIZADA
--    Necesita: SELECT en Pedidos (consultar pedidos pendientes),
--    UPDATE en Pedidos.Estado (marcar como EnViaje cuando esté listo).
-- ============================================================================
CREATE USER IF NOT EXISTS 'cocina_app'@'localhost'
    IDENTIFIED BY 'C0c1na_App!';

GRANT SELECT, UPDATE ON `5to_Todos`.`Pedidos` TO 'cocina_app'@'localhost';
GRANT SELECT ON `5to_Todos`.`PedidoPizzas`     TO 'cocina_app'@'localhost';

-- ============================================================================
-- 4. REPARTO
--    Necesita: SELECT en Pedidos (consultar pedidos en viaje),
--    UPDATE en Pedidos.Estado (marcar como Entregado).
-- ============================================================================
CREATE USER IF NOT EXISTS 'reparto_app'@'localhost'
    IDENTIFIED BY 'R3parto_App!';

GRANT SELECT, UPDATE ON `5to_Todos`.`Pedidos` TO 'reparto_app'@'localhost';
GRANT SELECT ON `5to_Todos`.`PedidoPizzas`     TO 'reparto_app'@'localhost';

-- ============================================================================
-- Aplicar cambios
-- ============================================================================
FLUSH PRIVILEGES;

-- ============================================================================
-- Verificación (opcional, descomentar para probar):
-- SELECT User, Host FROM mysql.user WHERE User IN ('admin_app','cliente_app','cocina_app','reparto_app');
-- SHOW GRANTS FOR 'admin_app'@'localhost';
-- SHOW GRANTS FOR 'cliente_app'@'localhost';
-- SHOW GRANTS FOR 'cocina_app'@'localhost';
-- SHOW GRANTS FOR 'reparto_app'@'localhost';
-- ============================================================================
