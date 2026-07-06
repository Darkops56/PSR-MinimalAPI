namespace BearPizzeria.Api.Models.DTOs;

public class PizzaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string Tamano { get; set; } = string.Empty;
}
