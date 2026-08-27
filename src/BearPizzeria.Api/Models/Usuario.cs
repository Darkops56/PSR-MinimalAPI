namespace BearPizzeria.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }

    public Cliente Cliente { get; set; } = null!;
}
