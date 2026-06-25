
using System.Data;
using Dapper;

namespace TodoPSR;

public class AdoDapper : IADO
{
    private readonly IDbConnection Conexion;

    public AdoDapper(IDbConnection conexion) => Conexion = conexion;

    public Task ActualizarTodoAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public async Task AgregarTodoAsync(Todo todo)
    {
        var query = @"INSERT INTO Todo (name, isComplete)
                    VALUE (@name, @isComplete);
                    SELECT last_insert_id();";

        var parametros = new DynamicParameters();
        parametros.Add("name", todo.Name);
        parametros.Add("isComplete", todo.IsComplete);

        todo.Id = await Conexion.QuerySingleAsync<int>(query, parametros);
    }

    public Task EliminarTodoAsync(Todo todo)
    {
        throw new NotImplementedException();
    }

    public Task<Todo?> ObtenerTodoPorIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Todo>> ObtenerTodosAsync()
    {
        var query = @"SELECT * FROM Todo";
        var todos = await Conexion.QueryAsync<Todo>(query);
        return todos;
    }

    public Task<IEnumerable<Todo>> ObtenerTodosFinalizadosAsync()
    {
        throw new NotImplementedException();
    }
}
