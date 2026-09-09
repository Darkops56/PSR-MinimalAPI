using System.ComponentModel.DataAnnotations;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    [Display(Name = "Nombre de Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    [Display(Name = "Nombre Completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo no válido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de contacto es obligatorio")]
    [Phone(ErrorMessage = "Formato de teléfono no válido")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;
}
