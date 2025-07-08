
using System.Data;

namespace TodoPSR;

public class AdoDapper : IADO
{
    private readonly IDbConnection Conexion;

    public AdoDapper(IDbConnection conexion) => Conexion = conexion;

    public Task ActualizarTodoAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public Task AgregarTodoAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public Task EliminarTodoAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public Task<Todo?> ObtenerTodoPorIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Todo>> ObtenerTodosAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Todo>> ObtenerTodosFinalizadosAsync()
    {
        throw new NotImplementedException();
    }
}
