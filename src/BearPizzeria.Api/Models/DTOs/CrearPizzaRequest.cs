namespace BearPizzeria.Api.Models.DTOs;

public class CrearPizzaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Tamano { get; set; } = string.Empty;
}
