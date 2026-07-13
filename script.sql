DROP DATABASE IF EXISTS 5to_Todos;
CREATE DATABASE 5to_Todos;
USE 5to_Todos;

-- Tabla de clientes
CREATE TABLE Clientes (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Direccion VARCHAR(200) NOT NULL,
    Telefono VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Usuario VARCHAR(50) NOT NULL,
    UNIQUE (Email),
    UNIQUE (Usuario)
);

-- Tabla de pizzas
CREATE TABLE Pizzas (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion TEXT,
    Precio DECIMAL(10,2) NOT NULL,
    Tamano ENUM('Personal', 'Mediana', 'Grande', 'Familiar') NOT NULL
);

-- Tabla de pedidos
CREATE TABLE Pedidos (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    FechaPedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    Estado ENUM('EnPreparacion', 'EnViaje', 'Entregado') NOT NULL DEFAULT 'EnPreparacion',
    Total DECIMAL(10,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
);

-- Tabla intermedia Pedido-Pizza
CREATE TABLE PedidoPizzas (
    PedidoId INT NOT NULL,
    PizzaId INT NOT NULL,
    Cantidad INT NOT NULL DEFAULT 1,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (PedidoId, PizzaId),
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id),
    FOREIGN KEY (PizzaId) REFERENCES Pizzas(Id)
);

-- Datos iniciales de pizzas
INSERT INTO Pizzas (Nombre, Descripcion, Precio, Tamano) VALUES
    ('Muzzarella', 'Muzzarella, aceitunas y orégano', 4500.00, 'Grande'),
    ('Napolitana', 'Muzzarella, tomate, ajo y aceitunas', 5000.00, 'Grande'),
    ('Fugazzeta', 'Muzzarella, cebolla y aceitunas', 4800.00, 'Grande'),
    ('Especial', 'Muzzarella, jamón, morrón y aceitunas', 5500.00, 'Grande'),
    ('Calabresa', 'Muzzarella, longaniza calabresa y aceitunas', 5200.00, 'Grande');
