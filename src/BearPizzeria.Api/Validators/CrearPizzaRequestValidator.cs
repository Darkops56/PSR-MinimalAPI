using FluentValidation;
using BearPizzeria.Api.Models;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class CrearPizzaRequestValidator : AbstractValidator<CrearPizzaRequest>
{
    public CrearPizzaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la pizza es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre es demasiado largo. Usá máximo 100 caracteres.");

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a cero. Ingresá un precio válido.")
            .LessThan(100000).WithMessage("El precio no puede superar los $100.000.");

        RuleFor(x => x.Tamano)
            .NotEmpty().WithMessage("El tamaño es obligatorio. Elegí entre: Personal, Mediana, Grande, Familiar.")
            .Must(t => Enum.TryParse<TamanoPizza>(t, ignoreCase: true, out _))
            .WithMessage("Tamaño inválido. Valores válidos: Personal, Mediana, Grande, Familiar.");
    }
}
