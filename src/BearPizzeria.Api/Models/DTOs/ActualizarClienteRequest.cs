namespace BearPizzeria.Api.Models.DTOs;

public record ActualizarClienteRequest(
    string Username,
    string Nombre,
    string Direccion,
    string Telefono,
    string Email
);
