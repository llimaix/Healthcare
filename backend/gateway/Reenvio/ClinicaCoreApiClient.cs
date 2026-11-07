using System.Net.Http.Headers;

namespace ClinicaCore.Gateway.Reenvio;

/// <summary>
/// Cliente HTTP tipado hacia el servicio de dominio ClinicaCore.Api. El Gateway (BFF)
/// reenvia la superficie publica a traves de el, propagando el header Authorization
/// para que la Api aplique la autorizacion por JWT y rol.
/// </summary>
public class ClinicaCoreApiClient
{
    private readonly HttpClient _http;

    public ClinicaCoreApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Reenvia la peticion entrante a la Api conservando metodo, ruta, query, cuerpo y
    /// el header Authorization, y devuelve la respuesta tal cual para que el Gateway la
    /// reproduzca hacia el cliente.
    /// </summary>
    public async Task<HttpResponseMessage> ReenviarAsync(HttpRequest entrante, CancellationToken ct)
    {
        var destino = entrante.Path + entrante.QueryString;
        var solicitud = new HttpRequestMessage(new HttpMethod(entrante.Method), destino);

        if (TieneCuerpo(entrante))
        {
            var contenido = new StreamContent(entrante.Body);
            if (MediaTypeHeaderValue.TryParse(entrante.ContentType, out var tipo))
            {
                contenido.Headers.ContentType = tipo;
            }
            solicitud.Content = contenido;
        }

        if (entrante.Headers.TryGetValue("Authorization", out var autorizacion))
        {
            solicitud.Headers.TryAddWithoutValidation("Authorization", autorizacion.ToArray());
        }

        return await _http.SendAsync(solicitud, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    /// <summary>
    /// Decide si la peticion entrante trae un cuerpo que debe reenviarse. Ademas de los
    /// casos con Content-Length conocido (&gt; 0) o Transfer-Encoding explicito, cubre el
    /// reenvio de cuerpos chunked: cuando el metodo admite cuerpo y la longitud es
    /// desconocida (Content-Length nulo), el cuerpo se envia igualmente para no omitirlo.
    /// Los metodos sin cuerpo (GET/HEAD/DELETE/OPTIONS/TRACE/CONNECT) no adjuntan contenido.
    /// </summary>
    private static bool TieneCuerpo(HttpRequest entrante)
    {
        if (entrante.ContentLength is > 0 || entrante.Headers.ContainsKey("Transfer-Encoding"))
        {
            return true;
        }

        // Content-Length desconocido (nulo): puede ser un cuerpo chunked. Solo se reenvia
        // en metodos que admiten cuerpo para no adjuntar contenido vacio en GET/HEAD/etc.
        return entrante.ContentLength is null && MetodoAdmiteCuerpo(entrante.Method);
    }

    private static bool MetodoAdmiteCuerpo(string metodo) =>
        !(HttpMethods.IsGet(metodo)
            || HttpMethods.IsHead(metodo)
            || HttpMethods.IsDelete(metodo)
            || HttpMethods.IsOptions(metodo)
            || HttpMethods.IsTrace(metodo)
            || HttpMethods.IsConnect(metodo));
}
