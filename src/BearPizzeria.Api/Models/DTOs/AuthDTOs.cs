namespace BearPizzeria.Api.Models.DTOs;

public record RegisterRequest(
    string Nombre,
    string Direccion,
    string Telefono,
    string Email,
    string Username,
    string Password
);

public record LoginRequest(
    string Username,
    string Password
);

public record AuthResponse(
    int ClienteId,
    string Username,
    string Nombre,
    string Email,
    string Direccion,
    string Telefono,
    string Token
);
