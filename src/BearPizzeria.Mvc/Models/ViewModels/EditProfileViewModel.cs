using System.ComponentModel.DataAnnotations;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "El usuario no puede superar los 50 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
    [StringLength(100, ErrorMessage = "El email no puede superar los 100 caracteres.")]
    public string Email { get; set; } = string.Empty;
}
