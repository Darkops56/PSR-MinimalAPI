using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Common.Testing;
using Xunit;

namespace BearPizzeria.Api.Tests.Integration;

public class DireccionesYClientesEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DireccionesYClientesEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(int ClienteId, string Token)> RegistrarUsuarioAsync()
    {
        var username = $"user_{Guid.NewGuid():N}"[..12];
        var reg = new RegisterRequest("Cliente Dir", "+54 11 9999-0000", $"{username}@test.com", username, "pass123", "Direccion Inicial");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.ClienteId, auth.Token);
    }

    [Fact]
    public async Task Direcciones_CU06_FlujoCRUDYPrincipal()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Alta de dirección
        var nuevaDirReq = new CrearDireccionRequest("Trabajo", "Av. Libertador 5000", "Piso 4", false);
        var createRes = await _client.PostAsJsonAsync($"/api/clientes/{clienteId}/direcciones", nuevaDirReq);

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var dirCreada = await createRes.Content.ReadFromJsonAsync<DireccionResponse>();
        Assert.NotNull(dirCreada);
        Assert.Equal("Trabajo", dirCreada.Nombre);

        // 2. Marcar como principal
        var principalRes = await _client.PutAsync($"/api/clientes/{clienteId}/direcciones/{dirCreada.Id}/principal", null);
        Assert.Equal(HttpStatusCode.OK, principalRes.StatusCode);
        var dirPrincipal = await principalRes.Content.ReadFromJsonAsync<DireccionResponse>();
        Assert.True(dirPrincipal!.EsPrincipal);

        // 3. Listar direcciones (la principal debe estar primera)
        var listRes = await _client.GetAsync($"/api/clientes/{clienteId}/direcciones");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var direcciones = await listRes.Content.ReadFromJsonAsync<List<DireccionResponse>>();
        Assert.NotNull(direcciones);
        Assert.True(direcciones[0].EsPrincipal);

        // 4. Actualizar dirección
        var updateReq = new ActualizarDireccionRequest("Trabajo Central", "Av. Libertador 5000 Of 402", "Piso 4 Oficina", true);
        var updateRes = await _client.PutAsJsonAsync($"/api/clientes/{clienteId}/direcciones/{dirCreada.Id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var dirUpdated = await updateRes.Content.ReadFromJsonAsync<DireccionResponse>();
        Assert.Equal("Trabajo Central", dirUpdated!.Nombre);

        // 5. Eliminar dirección
        var deleteRes = await _client.DeleteAsync($"/api/clientes/{clienteId}/direcciones/{dirCreada.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
    }

    [Fact]
    public async Task Direcciones_CU06_AltaSinCamposObligatorios_DebeFallarConMensajeEntendible()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reqInvalido = new CrearDireccionRequest("", "", null, false);
        var res = await _client.PostAsJsonAsync($"/api/clientes/{clienteId}/direcciones", reqInvalido);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var error = await res.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("DIRECCION-400", error.Codigo);
        Assert.Equal("Nombre y Dirección completa son obligatorios.", error.Mensaje);
    }

    [Fact]
    public async Task Clientes_CU05_ActualizarPerfilConDatosValidos()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nuevoNombre = "Nombre Actualizado";
        var updateReq = new ActualizarClienteRequest("usuario_renombrado", nuevoNombre, "Nueva Dir", "+54 11 5555-9999", "nuevo@email.com");
        var res = await _client.PutAsJsonAsync($"/api/clientes/{clienteId}", updateReq);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cliente = await res.Content.ReadFromJsonAsync<ClienteResponse>();

        Assert.NotNull(cliente);
        Assert.Equal(nuevoNombre, cliente.Nombre);
        Assert.Equal("nuevo@email.com", cliente.Email);
    }

    [Fact]
    public async Task Clientes_CU05_ActualizarConEmailEnUso_DebeFallarConMensajeEntendible()
    {
        var (cliente1Id, token1) = await RegistrarUsuarioAsync();
        var (_, _) = await RegistrarUsuarioAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var cliente2Res = await _client.GetFromJsonAsync<ClienteResponse>($"/api/clientes/1");
        if (cliente2Res != null && cliente2Res.Id != cliente1Id)
        {
            var updateReq = new ActualizarClienteRequest("user_cambio", "Nombre", "Dir", "+54 11 1111-2222", cliente2Res.Email);
            var res = await _client.PutAsJsonAsync($"/api/clientes/{cliente1Id}", updateReq);

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            var error = await res.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.NotNull(error);
            Assert.Equal("CLIENTE-400", error.Codigo);
            Assert.Contains("El correo electrónico ya está registrado", error.Mensaje);
        }
    }
}
