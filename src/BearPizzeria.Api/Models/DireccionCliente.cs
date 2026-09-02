namespace BearPizzeria.Api.Models;

public class DireccionCliente
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej: "Mi casa", "Casa Papá"
    public string DireccionCompleta { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public bool EsPrincipal { get; set; }

    public Cliente Cliente { get; set; } = null!;
}
