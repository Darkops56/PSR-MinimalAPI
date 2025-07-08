namespace TodoPSR;

public interface IADO
{
    Task<IEnumerable<Todo>> ObtenerTodosAsync();
    Task<IEnumerable<Todo>> ObtenerTodosFinalizadosAsync();
    Task<Todo?> ObtenerTodoPorIdAsync(int id);
    Task AgregarTodoAsync(Todo todo);
    Task ActualizarTodoAsync(Todo todo);
    Task EliminarTodoAsync(Todo todo);
}
