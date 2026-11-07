using System.Text;
using ClinicaCore.Gateway.Reenvio;
using Microsoft.AspNetCore.Http;

namespace ClinicaCore.Gateway.Tests;

/// <summary>
/// Pruebas del reenvio del Gateway (BFF) hacia la Api de dominio. Cubren el reenvio de
/// cuerpos sin Content-Length (chunked), preservando el comportamiento con Content-Length
/// y la propagacion del header Authorization.
/// </summary>
public class ReenvioTests
{
    /// <summary>
    /// Handler que captura la peticion saliente para poder inspeccionar en la prueba
    /// el cuerpo y las cabeceras que el Gateway reenvia a la Api.
    /// </summary>
    private sealed class CapturaHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Capturada { get; private set; }
        public string? CuerpoCapturado { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Capturada = request;
            if (request.Content is not null)
            {
                CuerpoCapturado = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }

    private static ClinicaCoreApiClient CrearCliente(CapturaHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

    private static HttpRequest CrearPeticion(
        string metodo, string cuerpo, long? contentLength, string? authorization)
    {
        var contexto = new DefaultHttpContext();
        var request = contexto.Request;
        request.Method = metodo;
        request.Path = "/api/diagnosticos";
        request.ContentType = "application/json";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(cuerpo));
        // Un cuerpo chunked llega sin Content-Length; el proxy debe reenviarlo igual.
        request.ContentLength = contentLength;
        if (authorization is not null)
        {
            request.Headers.Authorization = authorization;
        }
        return request;
    }

    [Fact]
    public async Task Reenviar_CuerpoSinContentLength_ReenviaElCuerpo()
    {
        var handler = new CapturaHandler();
        var cliente = CrearCliente(handler);
        var peticion = CrearPeticion(
            "POST", "{\"descripcion\":\"chunked\"}", contentLength: null, authorization: null);

        await cliente.ReenviarAsync(peticion, CancellationToken.None);

        Assert.NotNull(handler.Capturada!.Content);
        Assert.Equal("{\"descripcion\":\"chunked\"}", handler.CuerpoCapturado);
    }

    [Fact]
    public async Task Reenviar_CuerpoConContentLength_ReenviaElCuerpo()
    {
        var handler = new CapturaHandler();
        var cliente = CrearCliente(handler);
        var cuerpo = "{\"descripcion\":\"con-longitud\"}";
        var peticion = CrearPeticion(
            "POST", cuerpo, contentLength: Encoding.UTF8.GetByteCount(cuerpo), authorization: null);

        await cliente.ReenviarAsync(peticion, CancellationToken.None);

        Assert.NotNull(handler.Capturada!.Content);
        Assert.Equal(cuerpo, handler.CuerpoCapturado);
    }

    [Fact]
    public async Task Reenviar_Get_NoAdjuntaCuerpo()
    {
        var handler = new CapturaHandler();
        var cliente = CrearCliente(handler);
        var peticion = CrearPeticion("GET", string.Empty, contentLength: null, authorization: null);

        await cliente.ReenviarAsync(peticion, CancellationToken.None);

        Assert.Null(handler.Capturada!.Content);
    }

    [Fact]
    public async Task Reenviar_PropagaAuthorization()
    {
        var handler = new CapturaHandler();
        var cliente = CrearCliente(handler);
        var peticion = CrearPeticion(
            "POST", "{}", contentLength: null, authorization: "Bearer token-de-prueba");

        await cliente.ReenviarAsync(peticion, CancellationToken.None);

        Assert.True(handler.Capturada!.Headers.TryGetValues("Authorization", out var valores));
        Assert.Equal("Bearer token-de-prueba", string.Join(string.Empty, valores!));
    }
}
