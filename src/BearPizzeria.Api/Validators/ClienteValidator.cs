using FluentValidation;
using BearPizzeria.Api.Models;

namespace BearPizzeria.Api.Validators;

public class ClienteValidator : AbstractValidator<Cliente>
{
    public ClienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio. Escribí tu nombre completo.")
            .MaximumLength(100).WithMessage("El nombre es demasiado largo. Usá máximo 100 caracteres.");

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es obligatoria. Escribí calle y número.")
            .MaximumLength(200).WithMessage("La dirección es demasiado larga. Usá máximo 200 caracteres.");

        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio. Escribí un número de contacto.")
            .MaximumLength(20).WithMessage("El teléfono es demasiado largo. Usá máximo 20 caracteres.")
            .Matches(@"^\+?[\d\s\-\(\)]+$")
            .WithMessage("El formato del teléfono no es válido. Usá solo números, espacios, guiones o paréntesis. Ej: +54 11 5555-1234");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio. Escribí tu correo electrónico.")
            .MaximumLength(100).WithMessage("El email es demasiado largo. Usá máximo 100 caracteres.")
            .EmailAddress().WithMessage("El formato del email no es válido. Ej: usuario@correo.com");
    }
}
