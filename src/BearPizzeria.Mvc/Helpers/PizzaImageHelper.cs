namespace BearPizzeria.Mvc.Helpers;

public static class PizzaImageHelper
{
    public static string GetImagePath(string? pizzaNombre)
    {
        if (string.IsNullOrWhiteSpace(pizzaNombre))
        {
            return "/img/pisademuzzarella.jpg";
        }

        var nombre = pizzaNombre.Trim().ToLowerInvariant();

        if (nombre.Contains("calabresa"))
            return "/img/pisadecalabresa.webp";
        if (nombre.Contains("fugazzeta"))
            return "/img/pisadefugazzeta.webp";
        if (nombre.Contains("napolitana"))
            return "/img/pisanapolitana.jpg";
        if (nombre.Contains("especial"))
            return "/img/pisaespecial.jpeg";
        if (nombre.Contains("muzzarella"))
            return "/img/pisademuzzarella.jpg";

        return "/img/pisademuzzarella.jpg";
    }
}
