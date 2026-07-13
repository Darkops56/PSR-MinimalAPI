-- ============================================================================
-- INSERTS.sql — BearPizzeria
-- Datos de prueba para el entorno de desarrollo
-- ============================================================================

USE 5to_Todos;

-- -----------------------------------------------------------
-- Pizzas del catálogo (seed inicial)
-- -----------------------------------------------------------
INSERT INTO Pizzas (Nombre, Descripcion, Precio, Tamano) VALUES
    ('Muzzarella',  'Muzzarella, aceitunas y orégano',      4500.00, 'Grande'),
    ('Napolitana',  'Muzzarella, tomate, ajo y aceitunas',  5000.00, 'Grande'),
    ('Fugazzeta',   'Muzzarella, cebolla y aceitunas',      4800.00, 'Grande'),
    ('Especial',    'Muzzarella, jamón, morrón y aceitunas', 5500.00, 'Grande'),
    ('Calabresa',   'Muzzarella, longaniza calabresa y aceitunas', 5200.00, 'Grande');

-- -----------------------------------------------------------
-- Clientes de prueba
-- -----------------------------------------------------------
INSERT INTO Clientes (Nombre, Direccion, Telefono, Email, Usuario) VALUES
    ('Juan Pérez',     'Av. Siempre Viva 123',     '11-1234-5678', 'juan@email.com',     'juanp'),
    ('María García',   'Calle Falsa 456',          '11-2345-6789', 'maria@email.com',    'mariag'),
    ('Carlos López',   'Belgrano 789',             '11-3456-7890', 'carlos@email.com',   'carlosl'),
    ('Ana Martínez',   'San Martín 321',           '11-4567-8901', 'ana@email.com',      'anam'),
    ('Pedro Rodríguez','Rivadavia 654',            '11-5678-9012', 'pedro@email.com',    'pedror');

-- -----------------------------------------------------------
-- Pedidos de prueba
-- -----------------------------------------------------------
INSERT INTO Pedidos (ClienteId, Estado, Total) VALUES
    (1, 'EnPreparacion', 9000.00),
    (2, 'EnViaje',       5000.00),
    (3, 'Entregado',     4800.00);

-- -----------------------------------------------------------
-- Detalle (items) de los pedidos de prueba
-- -----------------------------------------------------------
INSERT INTO PedidoPizzas (PedidoId, PizzaId, Cantidad, PrecioUnitario) VALUES
    (1, 1, 2, 4500.00),   -- Pedido 1: 2x Muzzarella
    (2, 2, 1, 5000.00),   -- Pedido 2: 1x Napolitana
    (3, 3, 1, 4800.00);   -- Pedido 3: 1x Fugazzeta
