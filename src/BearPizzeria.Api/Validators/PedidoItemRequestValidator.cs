using FluentValidation;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class PedidoItemRequestValidator : AbstractValidator<PedidoItemRequest>
{
    public PedidoItemRequestValidator()
    {
        RuleFor(x => x.PizzaNombre)
            .NotEmpty().WithMessage("El nombre de la pizza es obligatorio. Elegí una pizza del catálogo.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad de pizzas debe ser al menos 1.");
    }
}
