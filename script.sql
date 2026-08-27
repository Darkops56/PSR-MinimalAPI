DROP DATABASE IF EXISTS 5to_Todos;
CREATE DATABASE 5to_Todos;
USE 5to_Todos;

-- 1. Tabla de Clientes (Datos de contacto y entrega)
CREATE TABLE Clientes (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Direccion VARCHAR(200) NOT NULL,
    Telefono VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    UNIQUE (Email)
);

-- 2. Tabla de Usuarios (Credenciales con encriptación fuerte)
CREATE TABLE Usuarios (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    Username VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FechaCreacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    UltimoAcceso DATETIME NULL,
    UNIQUE (Username),
    UNIQUE (ClienteId),
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE
);

-- 3. Tabla de Pizzas (Catálogo base)
CREATE TABLE Pizzas (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion TEXT,
    Precio DECIMAL(10,2) NOT NULL,
    Tamano ENUM('Personal', 'Mediana', 'Grande', 'Familiar') NOT NULL DEFAULT 'Grande'
);

-- 4. Tabla de Carritos (Persistencia 1:1 por Cliente en BD)
CREATE TABLE Carritos (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    FechaActualizacion DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    Total DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    UNIQUE (ClienteId),
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id) ON DELETE CASCADE
);

-- 5. Tabla de Items del Carrito
CREATE TABLE CarritoItems (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    CarritoId INT NOT NULL,
    PizzaId INT NOT NULL,
    Tamano ENUM('Personal', 'Mediana', 'Grande', 'Familiar') NOT NULL DEFAULT 'Grande',
    Cantidad INT NOT NULL DEFAULT 1,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (CarritoId) REFERENCES Carritos(Id) ON DELETE CASCADE,
    FOREIGN KEY (PizzaId) REFERENCES Pizzas(Id) ON DELETE RESTRICT
);

-- 6. Tabla de Pedidos
CREATE TABLE Pedidos (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ClienteId INT NOT NULL,
    FechaPedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    Estado ENUM('EnPreparacion', 'EnViaje', 'Entregado') NOT NULL DEFAULT 'EnPreparacion',
    Total DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
);

-- 7. Tabla Intermedia Pedido-Pizza
CREATE TABLE PedidoPizzas (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    PedidoId INT NOT NULL,
    PizzaId INT NOT NULL,
    Tamano ENUM('Personal', 'Mediana', 'Grande', 'Familiar') NOT NULL DEFAULT 'Grande',
    Cantidad INT NOT NULL DEFAULT 1,
    PrecioUnitario DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE,
    FOREIGN KEY (PizzaId) REFERENCES Pizzas(Id)
);

-- Datos iniciales de pizzas
INSERT INTO Pizzas (Nombre, Descripcion, Precio, Tamano) VALUES
    ('Muzzarella', 'Muzzarella artesanal, aceitunas verdes seleccionadas y orégano fresco', 4500.00, 'Grande'),
    ('Napolitana', 'Muzzarella, rodajas de tomate natural, ajo picado y aceitunas negras', 5000.00, 'Grande'),
    ('Fugazzeta', 'Abundante muzzarella, cebolla caramelizada crujiente y orégano', 4800.00, 'Grande'),
    ('Especial', 'Muzzarella, jamón cocido premium, morrón asado y aceitunas', 5500.00, 'Grande'),
    ('Calabresa', 'Muzzarella, longaniza calabresa picante y toque de ají molido', 5200.00, 'Grande');
