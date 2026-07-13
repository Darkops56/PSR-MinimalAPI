using FluentValidation;
using BearPizzeria.Api.Models.DTOs;

namespace BearPizzeria.Api.Validators;

public class ActualizarEstadoRequestValidator : AbstractValidator<ActualizarEstadoRequest>
{
    public ActualizarEstadoRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio")
            .Must(BeValidEstado).WithMessage("Estado inválido. Valores válidos: EnPreparacion, EnViaje, Entregado");
    }

    private static bool BeValidEstado(string estado)
    {
        return Enum.TryParse<Models.EstadoPedido>(estado, out _);
    }
}
