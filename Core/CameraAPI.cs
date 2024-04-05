namespace pesada_hacienda.Core;

using OpenCvSharp;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Clase encargada de administrar las conexiones a las
/// transmisiones de las camaras mediante la API de Hik.
/// </summary>
public class CameraAPI
{
    /// <summary>
    /// Handler interno que sobrepasa la validacion de certificados.
    /// 
    /// No es buena practica hacer esto, pero como estamos dentro de una red local sin certificados
    /// no tenemos otra opcion.
    /// </summary>
    internal class HttpClientHandlerInsecure : HttpClientHandler
    {
        internal HttpClientHandlerInsecure()
        {
            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
        }
    }

    /// <summary>
    /// Clase que modela una respuesta de OpenAPI. 
    /// </summary>
    /// <typeparam name="T">Clase que modele el cuerpo del parametro "data".</typeparam>
    /// <param name="Code">Codigo de error, 0 si fue exitoso.</param>
    /// <param name="Msg">Mensaje detallando el significaado de "Code".</param>
    /// <param name="Data">Datos que devuelve la respuesta.</param>
    public record class Response<T>(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("msg")] string Msg,
        [property: JsonPropertyName("data")] T Data
    );

    /// <summary>
    /// Clase que modela la respuesta del llamado a la api <b>cameras/previewURLs</b>
    /// </summary>
    /// <param name="Url">URL de la transmision pedida.</param>
    public record class UrlData([property: JsonPropertyName("url")] string Url);

    /// <summary>
    /// Calidad con la que se quiere reproducir la transmision.
    /// </summary>
    public enum StreamQuality
    {
        /// <summary>
        /// Transmision en alta calidad, consume mas ancho de banda.
        /// </summary>
        High,
        /// <summary>
        /// Transmision en baja calidad, ideal para ahorrar ancho de banda.
        /// </summary>
        Low,
    }

    private const string DEFAULT_CAMERAID = "##-<CAMERA ID>-##";

    private readonly HttpClient _client = new(new HttpClientHandlerInsecure())
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private StreamQuality CurrentQuality;
    private string? CameraIdx;

    public string? Frame { get; private set; }
    public VideoCapture? CurrentStream { get; private set; }

    /// <summary>
    /// Adquiere el enlace a la transmision de una camara.
    /// </summary>
    /// <param name="quality">Calidad de la transmision.</param>
    /// <param name="cameraIdx">Id de la camara a reproducir.</param>
    /// <returns>Enlace a la transmision.</returns>
    /// <exception cref="HttpRequestException"/>
    private async Task<string> GetStreamUrl(StreamQuality quality, string cameraIdx)
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Accept", "*/*");

        using StringContent jsonBody =
            new(
                JsonSerializer.Serialize(
                    new
                    {
                        cameraIndexCode = cameraIdx,
                        streamType = quality,
                        protocol = "rtsp_s",
                        transmode = 1
                    }
                ),
                Encoding.UTF8,
                "application/json"
            );

        jsonBody.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        jsonBody.Headers.Add("x-ca-key", "##-<API KEY>-##");
        jsonBody.Headers.Add("x-ca-signature-headers", "x-ca-key");
        jsonBody.Headers.Add("x-ca-signature", "##-<REQUEST HASH>-##");

        HttpResponseMessage response;
        try
        {
            response = await _client.PostAsync(
                "##-<API URI>-##",
                jsonBody
            );
            response.EnsureSuccessStatusCode();
        }
        catch (Exception err)
        {
            Console.Error.WriteLine($"{err}");
            throw new HttpRequestException($"{err}");
        }

        var jsonResponse = await response.Content.ReadFromJsonAsync<Response<UrlData>>();
        return jsonResponse?.Data.Url ?? throw new HttpRequestException("Respuesta vacía");
    }

    /// <summary>
    /// Inicializa la transmision de la camara indicada en la calidad deseada.
    /// </summary>
    /// <param name="cameraIdx">Id de la camara a reproducir.</param>
    /// /// <param name="quality">Calidad de la transmision.</param>
    /// <returns>Devuelve un booleano si fallo o no.</returns>
    public async Task<bool> InitStreams(string cameraIdx = DEFAULT_CAMERAID, StreamQuality quality = StreamQuality.Low)
    {
        try
        {
            string url = await GetStreamUrl(quality, cameraIdx);
            CurrentQuality = quality;
            CurrentStream = new VideoCapture(@$"{url}")
            {
                BufferSize = 10
            };
            CameraIdx = cameraIdx;
        }
        catch (Exception err)
        {
            Console.Error.WriteLine($"{err}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Lee el siguiente frame de la transmision y lo guarda en el attributo 'Frame'.
    /// </summary>
    /// <returns>Devuelve falso si no pudo leer el proximo frame.</returns>
    public bool ReadFrame()
    {
        var frame = new Mat();
        bool rc = CurrentStream != null && CurrentStream.Read(frame);

        if (rc)
        {
            var base64 = Convert.ToBase64String(frame.ToBytes());
            Frame = $"data:image/jpg;base64,{base64}";
        }

        return rc;
    }

    /// <summary>
    /// Guarda un frame de la transmision.
    /// </summary>
    /// <returns>Devuelve el Frame o null si no pudo guardarlo</returns>
    public SavedFrame? SaveFrame()
    {
        try
        {
            var frame = new Mat();

            if (CurrentStream != null && CurrentStream.Read(frame))
            {
                string date = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
                string name = string.Concat(date, string.Format("_{0}", CurrentQuality.ToString().ToLower()), ".jpg");

                //frame = frame.Resize(new Size(1920.0, 1080.0));
                //File.WriteAllBytes(name, frame.ToBytes());

                return new SavedFrame(name, frame.ToMemoryStream());
            }
        }
        catch (Exception err)
        {
            Console.Error.WriteLine($"{err}");
            return null;
        }

        return null;
    }
}
