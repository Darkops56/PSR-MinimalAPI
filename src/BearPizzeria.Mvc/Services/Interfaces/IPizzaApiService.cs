using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Services.Interfaces;

public interface IPizzaApiService
{
    Task<List<PizzaDto>> GetPizzasAsync();
    Task<PizzaDto?> GetPizzaByIdAsync(int id);
}
