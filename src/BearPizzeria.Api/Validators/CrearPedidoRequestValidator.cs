using FluentValidation;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class CrearPedidoRequestValidator : AbstractValidator<CrearPedidoRequest>
{
    public CrearPedidoRequestValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("El ID del cliente debe ser un número positivo. Registrate como cliente primero.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("El pedido debe contener al menos una pizza. Agregá pizzas al pedido.")
            .NotEmpty().WithMessage("El pedido debe contener al menos una pizza. Agregá pizzas al pedido.");

        RuleForEach(x => x.Items)
            .SetValidator(new PedidoItemRequestValidator());
    }
}
