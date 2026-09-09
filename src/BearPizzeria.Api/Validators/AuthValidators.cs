using FluentValidation;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El usuario es obligatorio. Escribí un nombre de usuario.")
            .MinimumLength(3).WithMessage("El usuario debe tener al menos 3 caracteres.")
            .MaximumLength(50).WithMessage("El usuario es demasiado largo. Usá máximo 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("El usuario solo puede contener letras, números y guiones bajos.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio. Escribí tu nombre completo.")
            .MaximumLength(100).WithMessage("El nombre es demasiado largo. Usá máximo 100 caracteres.");


        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio. Escribí un número de contacto.")
            .MaximumLength(20).WithMessage("El teléfono es demasiado largo. Usá máximo 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]+$")
            .WithMessage("El formato del teléfono no es válido. Ej: +54 11 5555-1234");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio. Escribí tu correo electrónico.")
            .MaximumLength(100).WithMessage("El email es demasiado largo. Usá máximo 100 caracteres.")
            .EmailAddress().WithMessage("El formato del email no es válido. Ej: usuario@correo.com");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
