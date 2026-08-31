namespace BearPizzeria.Mvc.Models.DTOs;

public record ActualizarClienteDto(
    string Username,
    string Nombre,
    string Direccion,
    string Telefono,
    string Email
);
