using System.Net.Http.Headers;

namespace RavEat.Api.Data;

// Donde viven los archivos que sube la gente. Dos destinos posibles y una sola regla para los
// controllers: se entrega el archivo y vuelve la URL que va a la base.
//
//   sin configurar -> wwwroot/uploads, como siempre (ruta relativa; el front le pone el host)
//   con Supabase   -> el bucket publico del proyecto (URL completa, se usa tal cual)
//
// El disco de la API dejo de ser un lugar para archivos al publicar: el hosting gratuito no tiene
// backup, y recrear el sitio se lleva las fotos. El almacen externo separa la vida de los archivos
// de la vida del servidor. Mismo patron que el push y el ingreso con Google: sin la clave, todo lo
// demas funciona — el build no puede depender de un secreto.
//
// La subida va por HTTP directo (POST /storage/v1/object/...), sin SDK: son dos requests con un
// Bearer, y una dependencia entera para eso no se justifica.
public sealed class AlmacenArchivos(IConfiguration configuration, IWebHostEnvironment env, IHttpClientFactory httpClientFactory) {

    public bool SupabaseConfigurado =>
        !string.IsNullOrWhiteSpace(configuration["Supabase:Url"]) &&
        !string.IsNullOrWhiteSpace(configuration["Supabase:ServiceKey"]);

    // El nombre en el almacen es un GUID por el mismo motivo de siempre: el nombre original viene
    // del cliente y no es confiable — uno con "../" escribiria fuera de la carpeta, y dos fotos
    // "pizza.jpg" se pisarian. Eso no cambia segun donde se guarde.
    public async Task<string> GuardarImagenAsync(IFormFile archivo, string carpeta, CancellationToken cancellationToken) {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        if(!SupabaseConfigurado) return await GuardarLocalAsync(archivo, carpeta, nombreArchivo, cancellationToken);
        return await SubirASupabaseAsync(archivo, $"{carpeta}/{nombreArchivo}", extension, cancellationToken);
    }

    private async Task<string> GuardarLocalAsync(IFormFile archivo, string carpeta, string nombreArchivo, CancellationToken cancellationToken) {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var carpetaFisica = Path.Combine(webRoot, "uploads", carpeta);
        Directory.CreateDirectory(carpetaFisica);
        await using var stream = File.Create(Path.Combine(carpetaFisica, nombreArchivo));
        await archivo.CopyToAsync(stream, cancellationToken);
        // Ruta relativa, sin host: si la API se muda de maquina, las imagenes locales siguen
        // valiendo. El front la completa en utils/imagenes.js.
        return $"/uploads/{carpeta}/{nombreArchivo}";
    }

    // Si Supabase esta configurado y falla, el error corta la operacion: caer al disco local en
    // silencio repartiria las fotos entre dos lugares y escondería el problema de configuracion.
    private async Task<string> SubirASupabaseAsync(IFormFile archivo, string ruta, string extension, CancellationToken cancellationToken) {
        var url = configuration["Supabase:Url"]!.TrimEnd('/');
        var clave = configuration["Supabase:ServiceKey"]!;
        var bucket = configuration["Supabase:Bucket"] ?? "raveat";

        using var contenido = new StreamContent(archivo.OpenReadStream());
        contenido.Headers.ContentType = new MediaTypeHeaderValue(MimePorExtension(extension));
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, $"{url}/storage/v1/object/{bucket}/{ruta}");
        solicitud.Content = contenido;
        // Las dos cabeceras llevan la misma clave: el gateway de Supabase mira `apikey` y el
        // servicio de Storage mira el Bearer. Con una sola de las dos, segun el tipo de clave
        // (JWT legado o sb_secret), la subida puede rebotar con 401.
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clave);
        solicitud.Headers.Add("apikey", clave);

        var cliente = httpClientFactory.CreateClient();
        HttpResponseMessage respuesta;
        try {
            respuesta = await cliente.SendAsync(solicitud, cancellationToken);
        }
        catch(HttpRequestException) {
            throw new AlmacenArchivosException("No se pudo llegar al almacén de imágenes. Probá de nuevo.");
        }
        using(respuesta) {
            if(!respuesta.IsSuccessStatusCode) {
                throw new AlmacenArchivosException($"El almacén de imágenes rechazó el archivo ({(int)respuesta.StatusCode}).");
            }
        }

        // La URL publica del objeto: el bucket es publico porque estas fotos son la carta — las ve
        // cualquiera que abra la pagina. En la base va la URL completa: el host aca es el del
        // almacen, no el de la API, y las imagenes viejas en disco (ruta relativa) conviven con
        // estas sin que nadie tenga que adivinar cual es cual.
        return $"{url}/storage/v1/object/public/{bucket}/{ruta}";
    }

    private static string MimePorExtension(string extension) => extension switch {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}

// Excepcion propia para que el controller pueda distinguir "el almacen no anduvo" (502, con
// mensaje para la pantalla) de cualquier otro error inesperado (500).
public sealed class AlmacenArchivosException(string mensaje) : Exception(mensaje);
