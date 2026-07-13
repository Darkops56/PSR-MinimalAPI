-- ============================================================================
-- DDL.sql — BearPizzeria
-- Definición de la base de datos y tablas
-- ============================================================================

CREATE DATABASE IF NOT EXISTS 5to_Todos
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE 5to_Todos;

-- -----------------------------------------------------------
-- Tabla: Clientes
-- Almacena los datos de los clientes registrados
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS Clientes (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Direccion VARCHAR(200) NOT NULL,
    Telefono VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Usuario VARCHAR(50) NOT NULL,
    UNIQUE (Email),
    UNIQUE (Usuario)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- -----------------------------------------------------------
-- Tabla: Pizzas
-- Catálogo de pizzas disponibles
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS Pizzas (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion TEXT,
    Precio DECIMAL(10,2) NOT NULL,
    Tamano ENUM('Personal', 'Mediana', 'Grande', 'Familiar') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- -----------------------------------------------------------
-- Tabla: Pedidos
-- Cabecera de cada pedido realizado por un cliente
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS Pedidos (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    FechaPedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    Estado ENUM('EnPreparacion', 'EnViaje', 'Entregado') NOT NULL DEFAULT 'EnPreparacion',
    Total DECIMAL(10,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- -----------------------------------------------------------
-- Tabla: PedidoPizzas
-- Detalle (ítems) de cada pedido (relación N:M entre Pedidos y Pizzas)
-- -----------------------------------------------------------
CREATE TABLE IF NOT EXISTS PedidoPizzas (
    PedidoId INT NOT NULL,
    PizzaId INT NOT NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (PedidoId, PizzaId),
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    FOREIGN KEY (PizzaId) REFERENCES Pizzas(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
