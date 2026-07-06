using FluentValidation;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class PedidoItemRequestValidator : AbstractValidator<PedidoItemRequest>
{
    public PedidoItemRequestValidator()
    {
        RuleFor(x => x.PizzaId)
            .GreaterThan(0).WithMessage("El ID de la pizza no es válido. Elegí una pizza del catálogo.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad de pizzas debe ser al menos 1.");
    }
}
