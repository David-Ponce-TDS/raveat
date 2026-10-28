using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using RavEat.Api.Data;
using RavEat.Api.Domain.Enums;

namespace RavEat.Api.Controllers;

// La cara publica del pedido: lo que ve alguien que escaneo el QR y NO tiene la app ni una sesion.
//
// Todo este controller es anonimo, y por eso la regla que lo gobierna no es "que rol tiene" sino
// "que datos se pueden mostrar sin saber quien mira". La respuesta es: los del pedido y nada mas.
// Ni el telefono, ni la direccion, ni el apellido del cliente, ni quien lo cargo. Un QR pegado en
// una mesa lo puede leer cualquiera que pase.
//
// Lo que hace que esto no sea un agujero es el token: 16 bytes al azar en hexadecimal. Con el
// codigo (`PED-0042`) alcanzaba con sumar uno para mirar el pedido del de al lado — eso es un IDOR,
// y es el error de autorizacion mas comun que existe. Un token opaco no se recorre ni se adivina.
[AllowAnonymous]
[ApiController]
public sealed class PublicoController(RavEatDbContext db, IConfiguration configuration, IWebHostEnvironment entorno) : ControllerBase {
    // Un pedido tiene dos vistas publicas y comparten los mismos datos: el JSON lo consume la app
    // (que aterriza aca por deep link) y el HTML lo ve el navegador de quien no la tiene instalada.
    [HttpGet("api/publico/pedidos/{token}")]
    public async Task<IActionResult> Pedido(string token, CancellationToken cancellationToken) {
        var pedido = await BuscarAsync(token, cancellationToken);
        if(pedido is null) return NotFound(new {codigo = "pedido_no_encontrado", mensaje = "Ese enlace no corresponde a ningún pedido."});
        // Un pedido cancelado no muestra su ficha: el QR del comprobante queda "roto" a proposito
        // — lo que ese papel prometia ya no existe. 410 y no 404: el enlace existio, pero el
        // recurso se fue. Lo unico util que le queda al que escanea es la carta.
        if(pedido.Estado == EstadoPedido.Cancelado) {
            return StatusCode(410, new {codigo = "pedido_cancelado", mensaje = "Ese pedido fue cancelado y ya no está disponible."});
        }
        return Ok(pedido);
    }

    // La pagina que abre el navegador. Va sin framework y sin archivos sueltos a proposito: es una
    // sola respuesta, se ve igual con la app instalada o sin ella, y no depende de que el hosting
    // sirva estaticos.
    [HttpGet("p/{token}")]
    public async Task<IActionResult> Pagina(string token, CancellationToken cancellationToken) {
        var pedido = await BuscarAsync(token, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        if(pedido is null) return Content(PaginaNoEncontrado(), "text/html; charset=utf-8");
        // Mismo criterio que el JSON: cancelado no muestra items ni total — ofrece la carta.
        if(pedido.Estado == EstadoPedido.Cancelado) return Content(PaginaCancelado(pedido.Codigo), "text/html; charset=utf-8");
        return Content(PaginaPedido(pedido, token), "text/html; charset=utf-8");
    }

    // El archivo que hace que Android abra la app en vez del navegador. Android lo baja de
    // https://<dominio>/.well-known/assetlinks.json al instalar y verifica que la huella del
    // certificado con el que se firmo el APK este listada aca: sin eso, el enlace queda como uno
    // cualquiera y el sistema muestra el "abrir con".
    //
    // Se sirve como endpoint y no como archivo estatico porque las carpetas que empiezan con punto
    // quedan afuera de UseStaticFiles, y porque asi la huella se configura sin recompilar.
    [HttpGet(".well-known/assetlinks.json")]
    public IActionResult AssetLinks() {
        var huellas = configuration.GetSection("Publico:HuellasSha256").Get<string[]>() ?? [];
        var paquete = Paquete();
        return Ok(new[] {
            new {
                relation = new[] {"delegate_permission/common.handle_all_urls"},
                target = new {
                    @namespace = "android_app",
                    package_name = paquete,
                    sha256_cert_fingerprints = huellas
                }
            }
        });
    }

    // El token lo genera el servidor con un generador criptografico, no con Random ni con la hora:
    // un valor predecible tiene la misma debilidad que el codigo correlativo que vino a reemplazar.
    public static string GenerarToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    // La URL que se codifica en el QR. Si `Publico:BaseUrl` esta configurada manda esa (produccion);
    // si no, se arma con el host de la request, y asi el QR generado en la maquina de desarrollo
    // apunta a la maquina de desarrollo en vez de a un dominio que todavia no tiene ese pedido.
    public static string UrlPedido(HttpRequest request, IConfiguration configuration, string token) {
        var configurada = configuration["Publico:BaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configurada) ? $"{request.Scheme}://{request.Host}" : configurada.TrimEnd('/');
        return $"{baseUrl}/p/{token}";
    }

    // Un pedido de muestra, para poder ver y mostrar la pagina sin tener que crear uno de verdad
    // (una demo en clase, una prueba despues de publicar). Su token es legible a proposito: no
    // protege nada, porque no es un pedido — no existe en la base y no sale de ella. Los reales
    // siguen siendo 32 caracteres al azar.
    private const string TokenEjemplo = "ejemplo";

    private static PedidoPublicoResponse PedidoDeEjemplo() => new(
        "PED-EJEMPLO",
        TipoPedido.Retiro,
        EstadoPedido.EnPreparacion,
        EstadoPago.Pagado,
        16800m,
        DateTimeOffset.UtcNow,
        [
            new PedidoPublicoItem("Hamburguesa completa", 2, 11800m),
            new PedidoPublicoItem("Papas fritas", 1, 3500m),
            new PedidoPublicoItem("Limonada", 1, 1500m)
        ]);

    private async Task<PedidoPublicoResponse?> BuscarAsync(string token, CancellationToken cancellationToken) {
        if(token == TokenEjemplo) return PedidoDeEjemplo();
        // Se compara el token completo y nunca por prefijo: buscar "empieza con" convertiria el
        // token en algo que se puede ir adivinando de a pedazos.
        if(string.IsNullOrWhiteSpace(token) || token.Length != 32) return null;
        var pedido = await db.Pedidos.AsNoTracking()
            .Where(x => x.TokenPublico == token)
            .Select(x => new {x.Id, x.Codigo, x.Tipo, x.Estado, x.EstadoPago, x.Total, x.PropinaImporte, x.CreadoEn})
            .FirstOrDefaultAsync(cancellationToken);
        if(pedido is null) return null;
        var items = await db.PedidosItems.AsNoTracking()
            .Where(x => x.PedidoId == pedido.Id)
            .OrderBy(x => x.Id)
            .Select(x => new PedidoPublicoItem(x.ProductoNombre, x.Cantidad, x.Subtotal))
            .ToListAsync(cancellationToken);
        // El id interno no sale: afuera el pedido se llama por su token y por su codigo.
        return new PedidoPublicoResponse(
            pedido.Codigo,
            pedido.Tipo,
            pedido.Estado,
            pedido.EstadoPago,
            pedido.Total + pedido.PropinaImporte,
            pedido.CreadoEn,
            items);
    }

    private static readonly CultureInfo CulturaPesos = CultureInfo.GetCultureInfo("es-AR");

    private static string EstadoTexto(EstadoPedido estado) => estado switch {
        EstadoPedido.Borrador => "Tomado",
        EstadoPedido.Confirmado => "Confirmado",
        EstadoPedido.EnPreparacion => "En preparación",
        EstadoPedido.Listo => "Listo",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cerrado => "Cerrado",
        EstadoPedido.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    private static string TipoTexto(TipoPedido tipo) => tipo switch {
        TipoPedido.Presencial => "En el salón",
        TipoPedido.Retiro => "Para retirar",
        TipoPedido.Entregas => "Con envío",
        _ => tipo.ToString()
    };

    private static string PagoTexto(EstadoPago pago) => pago switch {
        EstadoPago.Pagado => "Pagado",
        EstadoPago.Anulado => "Anulado",
        _ => "Pendiente de pago"
    };

    private string Paquete() => Texto("Publico:PaqueteAndroid") ?? "app.raveat";

    // Los caminos para bajar la app salen de `wwwroot/distribucion.json`, el MISMO archivo que lee
    // la carta con `fetch`. Antes esto era una constante en el HTML y una clave en appsettings: dos
    // configuraciones para una sola cosa, que tarde o temprano dicen distinto. Ahora la pagina del
    // pedido y la carta ofrecen exactamente lo mismo.
    //
    // Se lee en cada request y no se cachea a proposito: son 100 bytes, y asi editar el JSON en el
    // hosting tiene efecto sin reiniciar la API — que es todo el punto de que sea un archivo.
    // `Grupo` tiene valor por defecto para que los tres "sin datos" de mas abajo sigan siendo una
    // linea: es el unico campo que no es un camino de descarga sino el requisito para usar uno.
    // `AppEnPedido` es el switch de la pagina del pedido: en false no ofrece la app (ni abrirla ni
    // bajarla) hasta que este lista; se pasa a true en el JSON el dia que se publica.
    private sealed record Distribucion(string? Play, string? Prueba, string? Apk, string? Grupo = null, bool AppEnPedido = false);

    private Distribucion LeerDistribucion() {
        try {
            var ruta = Path.Combine(entorno.WebRootPath ?? string.Empty, "distribucion.json");
            if(!System.IO.File.Exists(ruta)) return new Distribucion(null, null, null);
            var json = System.IO.File.ReadAllText(ruta);
            var leido = JsonSerializer.Deserialize<Distribucion>(json, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            return leido ?? new Distribucion(null, null, null);
        }
        catch(Exception) {
            // Un JSON mal escrito no puede tumbar la pagina del pedido: sin enlaces, la ficha se
            // sigue viendo, que es lo que la persona vino a mirar.
            return new Distribucion(null, null, null);
        }
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    // El destino del QR y del `browser_fallback_url`, en orden: la ficha en Play si existe, la
    // pagina de la prueba si todavia no, y si no hay ninguna, esta misma pagina. El escalon del
    // medio es el que corre hoy — la app esta en prueba y el QR lleva a sumarse a ella. El ultimo
    // no es un parche: es el paso pedagogico, el QR funciona y lleva a algo real desde el primer
    // dia, antes de que exista la ficha.
    private string UrlDescarga() {
        var distribucion = LeerDistribucion();
        var destino = Limpiar(distribucion.Play) ?? Limpiar(distribucion.Prueba);
        if(destino is not null) return destino;
        var configurada = Texto("Publico:BaseUrl");
        return configurada is not null ? configurada.TrimEnd('/') + "/" : $"{Request.Scheme}://{Request.Host}/";
    }

    // Los mismos tres botones que la carta, con los mismos textos. El que no tiene enlace no se
    // dibuja: un boton que no lleva a ningun lado es peor que no tenerlo.
    private string BotonesDistribucion() {
        var distribucion = LeerDistribucion();
        if(!distribucion.AppEnPedido) return string.Empty;
        var opciones = new (string? Url, string Texto)[] {
            (Limpiar(distribucion.Play), "Descargar en Google Play"),
            (Limpiar(distribucion.Prueba), "Entrar a la prueba interna"),
            (Limpiar(distribucion.Apk), "Descargar el APK directo")
        };
        return string.Concat(opciones
            .Where(opcion => opcion.Url is not null)
            .Select(opcion => $"""<a class="boton secundario" href="{WebUtility.HtmlEncode(opcion.Url)}" rel="noopener">{opcion.Texto}</a>"""));
    }

    // Una clave presente pero vacia en appsettings.json no es null: sin esto, dejar el valor en ""
    // (que es como quedan las plantillas) devolvia cadena vacia en vez del valor por defecto.
    private string? Texto(string clave) {
        var valor = configuration[clave];
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    // El boton "abrir en la app" usa el esquema `intent://` de Chrome en Android: si la app esta
    // instalada la abre, y si no, sigue el `browser_fallback_url` — que aca es Google Play. Es la
    // red de contencion para cuando el sistema NO verifico los App Links (Play Store todavia sin
    // publicar, APK de debug, o el usuario que eligio abrir siempre en el navegador).
    private string IntentUrl(string token) {
        var paquete = Paquete();
        var destino = new Uri(UrlPedido(Request, configuration, token));
        var fallback = WebUtility.UrlEncode(UrlDescarga());
        return $"intent://{destino.Host}{destino.PathAndQuery}#Intent;scheme={destino.Scheme};package={paquete};S.browser_fallback_url={fallback};end";
    }

    // Abrir la app solo tiene sentido cuando hay una app que bajar: con el switch apagado, el que
    // no la tiene caeria en una tienda sin ficha.
    // La carta va como boton y no como enlace al pie: sin la app es lo unico que se puede hacer
    // desde aca. Con la app ofrecida, el principal es abrirla y la carta pasa a secundario.
    private string BotonCarta() {
        var clase = LeerDistribucion().AppEnPedido ? "boton secundario" : "boton";
        return $"""<a class="{clase}" href="/">Ver la carta de RavEat</a>""";
    }

    private string BotonAbrirApp(string token) {
        if(!LeerDistribucion().AppEnPedido) return string.Empty;
        return $"""<a class="boton" href="{WebUtility.HtmlEncode(IntentUrl(token))}">Abrir en la app RavEat</a>""";
    }

    // El QR de descarga que va al pie de la carta. Se genera en el servidor y no con una libreria
    // de JavaScript: la pagina no baja nada de afuera, y el mismo generador que ya usa el
    // comprobante sirve para esto.
    [HttpGet("api/publico/qr-descarga")]
    public IActionResult QrDescarga() {
        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(UrlDescarga(), QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(datos).GetGraphic(10);
        Response.Headers.CacheControl = "no-store";
        return File(png, "image/png");
    }

    // El logo de Play como SVG embebido, el mismo dibujo que usa la carta: la pagina no baja
    // ninguna imagen de afuera y el logo se ve nitido a cualquier tamano.
    // El mismo icono de grupo que la carta: TRES figuras blancas, macizas, sobre el circulo azul.
    private const string IconoGrupo = """<svg viewBox="0 0 24 24" fill="#fff" aria-hidden="true"><circle cx="5.3" cy="10.1" r="2.3"/><path d="M5.3 13.2c-2.1 0-3.9 1.1-3.9 2.7v1.4h4.3v-.8c0-1.2.5-2.3 1.4-3.1-.6-.1-1.2-.2-1.8-.2z"/><circle cx="18.7" cy="10.1" r="2.3"/><path d="M18.7 13.2c-.6 0-1.2.1-1.8.2.9.8 1.4 1.9 1.4 3.1v.8h4.3v-1.4c0-1.6-1.8-2.7-3.9-2.7z"/><circle cx="12" cy="8.4" r="3.1"/><path d="M12 12.5c-2.8 0-5.1 1.4-5.1 3.3v2.3h10.2v-2.3c0-1.9-2.3-3.3-5.1-3.3z"/></svg>""";

    private const string LogoPlay = """<svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#00c4ff" d="M3.6 1.8c-.4.3-.6.8-.6 1.4v17.6c0 .6.2 1.1.6 1.4l.1.1L13.8 12v-.2L3.7 1.7l-.1.1z"/><path fill="#ffce00" d="M17.1 15.5 13.8 12v-.2l3.3-3.4.1.1 4 2.3c1.1.6 1.1 1.7 0 2.3l-4 2.3-.1.1z"/><path fill="#f43249" d="M17.2 15.4 13.8 12 3.6 22.2c.4.4 1 .4 1.7.1l11.9-6.9"/><path fill="#00e076" d="M17.2 8.5 5.3 1.7c-.7-.4-1.3-.3-1.7.1L13.8 12l3.4-3.5z"/></svg>""";

    // El mismo panel de descarga que dibuja el pie de la carta: icono de la app, insignia de
    // Google Play y el QR. La insignia dice "disponible" solo cuando la ficha existe de verdad —
    // prometer una tienda que todavia no esta seria mentirle al que escanea.
    private string PanelDescarga() {
        var distribucion = LeerDistribucion();
        if(!distribucion.AppEnPedido) return string.Empty;
        var hayPlay = Limpiar(distribucion.Play) is not null;
        var hayPrueba = Limpiar(distribucion.Prueba) is not null;
        var insignia = hayPlay ? "DISPONIBLE EN" : hayPrueba ? "EN PRUEBA EN" : "MUY PRONTO EN";
        var detalle = hayPlay
            ? "Escaneá el código con otra cámara o compartilo: lleva a la ficha de Google Play."
            : hayPrueba
                ? "Escaneá el código con otra cámara o compartilo: lleva a la prueba abierta de la app en Google Play."
                : "Escaneá el código con otra cámara o compartilo: por ahora abre la carta del local y, cuando la ficha exista, llevará a la tienda.";
        // El grupo va DEBAJO del panel y no adentro: Play solo habilita la prueba cerrada a quien
        // esta en la lista, asi que es el paso previo al QR, no otro camino para bajar la app. Es
        // el mismo par que la carta, en angosto: el triangulo lo dibuja la tarjeta, asi que sin
        // invitacion no queda ninguna flecha apuntando al vacio.
        var grupo = Limpiar(distribucion.Grupo);
        var invitacion = grupo is null ? string.Empty : $"""
            <section class="grupo">
              <span class="grupo_icono">{IconoGrupo}</span>
              <p class="grupo_titulo">Unite a la prueba cerrada</p>
              <p class="grupo_sub">Sumate al grupo de prueba y volvé para escanear el código.</p>
              <a class="boton secundario" href="{WebUtility.HtmlEncode(grupo)}" rel="noopener" target="_blank">Sumarme al grupo</a>
            </section>
        """;
        return $$"""
            <section class="qr_panel">
              <div class="qr_cabecera">
                <img class="qr_icono" src="/marca/web-app-manifest-192x192.png" alt="">
                <div><p class="qr_titulo">Llevate la app RavEat</p><p class="qr_sub">Pedí y seguí tu pedido desde el teléfono.</p></div>
                <span class="qr_play">{{LogoPlay}}<span><small>{{insignia}}</small><b>Google Play</b></span></span>
              </div>
              <div class="qr_cuerpo">
                <div class="qr_marco"><img src="/api/publico/qr-descarga" alt="Código QR para descargar RavEat" width="108" height="108"></div>
                <p class="qr_texto">{{detalle}}</p>
              </div>
            </section>
            {{invitacion}}
        """;
    }

    private string PaginaPedido(PedidoPublicoResponse pedido, string token) {
        var filas = string.Join(string.Empty, pedido.Items.Select(item => $$"""
            <li><span class="cantidad">{{item.Cantidad}}&times;</span><span class="nombre">{{WebUtility.HtmlEncode(item.Nombre)}}</span><span class="importe">{{item.Subtotal.ToString("C0", CulturaPesos)}}</span></li>
        """));
        var cancelado = pedido.Estado == EstadoPedido.Cancelado;
        // El ejemplo se avisa arriba de todo: una pagina de muestra que no diga que es una muestra
        // termina siendo un pedido fantasma que alguien va a reclamar en el mostrador.
        var aviso = token == TokenEjemplo
            ? """<p class="ejemplo">Pedido de ejemplo &middot; así se ve el tuyo al escanear el QR</p>"""
            : string.Empty;
        var cuerpo = $$"""
            {{aviso}}
            <p class="codigo">{{WebUtility.HtmlEncode(pedido.Codigo)}}</p>
            <p class="estado {{(cancelado ? "estado_cancelado" : "estado_activo")}}">{{EstadoTexto(pedido.Estado)}}</p>
            <p class="meta">{{TipoTexto(pedido.Tipo)}} &middot; {{PagoTexto(pedido.EstadoPago)}}</p>
            <ul class="items">{{filas}}</ul>
            <p class="total"><span>Total</span><span>{{pedido.Total.ToString("C0", CulturaPesos)}}</span></p>
            {{BotonAbrirApp(token)}}
            {{BotonCarta()}}
            {{BotonesDistribucion()}}
            {{PanelDescarga()}}
            <p class="pie">Esta página muestra el estado del pedido. Para operarlo hace falta entrar con tu cuenta.</p>
        """;
        return Envoltorio($"Pedido {pedido.Codigo}", cuerpo);
    }

    // La pagina del pedido cancelado: dice que fue cancelado y ofrece el menu, nada mas. Mostrar
    // los items y el total de algo que no va a llegar seria darle vida a un papel que ya no vale.
    private string PaginaCancelado(string codigo) => Envoltorio($"Pedido {codigo}", $$"""
        <p class="codigo">{{WebUtility.HtmlEncode(codigo)}}</p>
        <p class="estado estado_cancelado">Cancelado</p>
        <p class="meta">Este pedido fue cancelado y ya no está disponible. Si querés pedir de nuevo, la carta te espera.</p>
        <a class="boton" href="/">Ver el menú de RavEat</a>
        {{BotonesDistribucion()}}
    """);

    private string PaginaNoEncontrado() => Envoltorio("Pedido no encontrado", $$"""
        <p class="codigo">Sin resultados</p>
        <p class="meta">Este enlace no corresponde a ningún pedido. Puede que el código se haya copiado incompleto.</p>
        <a class="boton" href="/">Ver la carta de RavEat</a>
        {{BotonesDistribucion()}}
    """);

    // Una sola hoja de estilos embebida: la pagina tiene que verse bien en el telefono de alguien
    // que quizas nunca vuelva, sin bajar una fuente ni un framework.
    private static string Envoltorio(string titulo, string cuerpo) => $$"""
        <!doctype html>
        <html lang="es">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta name="robots" content="noindex, nofollow">
        <meta name="theme-color" content="#3880ff">
        <link rel="icon" href="/marca/favicon.ico" sizes="any">
        <link rel="icon" type="image/png" sizes="96x96" href="/marca/favicon-96x96.png">
        <link rel="apple-touch-icon" href="/marca/apple-touch-icon.png">
        <title>{{WebUtility.HtmlEncode(titulo)}} &middot; RavEat</title>
        <style>
        :root { color-scheme: dark; }
        * { box-sizing: border-box; }
        body { margin: 0; padding: 24px 16px 48px; background: #12141a; color: #f4f5f7;
               font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; line-height: 1.45; }
        main { max-width: 420px; margin: 0 auto; background: #1b1e26; border: 1px solid #2a2e38;
               border-radius: 18px; padding: 22px; }
        /* El logo es un estatico de wwwroot y el resto de la pagina no depende de el: si faltara,
           el texto alternativo dice "RavEat" y la ficha se lee igual. */
        .marca { display: block; text-align: center; margin: 0 0 4px; }
        .marca img { width: 132px; max-width: 55%; height: auto; }
        .marca_lema { margin: 0 0 18px; text-align: center; color: #8b93a7; font-size: .84rem; }
        .codigo { margin: 0; font-size: 1.6rem; font-weight: 700; letter-spacing: 1px; }
        .estado { display: inline-block; margin: 8px 0 0; padding: 4px 12px; border-radius: 999px;
                  font-size: .82rem; font-weight: 600; }
        .estado_activo { background: rgba(56,128,255,.16); color: #6ea8ff; }
        .estado_cancelado { background: rgba(235,68,90,.16); color: #ff7b8c; }
        .meta { margin: 10px 0 0; color: #8b93a7; font-size: .88rem; }
        .items { list-style: none; margin: 20px 0 0; padding: 16px 0 0; border-top: 1px solid #2a2e38; }
        .items li { display: flex; gap: 10px; align-items: baseline; padding: 6px 0; font-size: .95rem; }
        .cantidad { color: #8b93a7; min-width: 28px; }
        .nombre { flex: 1; }
        .importe { font-variant-numeric: tabular-nums; }
        .total { display: flex; justify-content: space-between; margin: 14px 0 0; padding-top: 14px;
                 border-top: 1px solid #2a2e38; font-size: 1.1rem; font-weight: 700; }
        .boton { display: block; margin-top: 18px; padding: 14px; border-radius: 12px; text-align: center;
                 text-decoration: none; font-weight: 600; background: #3880ff; color: #fff; }
        .boton.secundario { margin-top: 10px; background: transparent; color: #6ea8ff; border: 1px solid #33506f; }
        /* El panel de descarga, la misma ficha de tienda que el pie de la carta pero en angosto:
           dentro de una tarjeta de 420px el QR va mas chico y el cuerpo puede envolver. */
        .qr_panel { margin-top: 22px; padding: 18px; border: 1px solid #2f3542; border-radius: 16px;
                    background: radial-gradient(130% 150% at 100% 0%, rgba(56,128,255,.16) 0%, rgba(56,128,255,0) 55%), #171a22; }
        .qr_cabecera { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; }
        .qr_icono { width: 44px; height: 44px; border-radius: 12px; box-shadow: 0 5px 14px rgba(0,0,0,.4); }
        .qr_titulo { margin: 0; font-size: 1rem; font-weight: 700; }
        .qr_sub { margin: 2px 0 0; color: #8b93a7; font-size: .78rem; }
        .qr_play { margin-left: auto; display: inline-flex; align-items: center; gap: 8px; padding: 7px 12px;
                   border-radius: 11px; background: #05070b; border: 1px solid #3a4150; color: #fff; }
        .qr_play svg { width: 18px; height: 18px; }
        .qr_play small { display: block; font-size: .55rem; letter-spacing: 1.2px; color: #9aa3b5; }
        .qr_play b { display: block; font-size: .85rem; line-height: 1.15; }
        .qr_cuerpo { display: flex; flex-wrap: wrap; align-items: center; gap: 14px; margin-top: 14px; }
        .qr_marco { flex-shrink: 0; padding: 8px; background: #fff; border-radius: 12px; line-height: 0;
                    box-shadow: 0 0 0 5px rgba(56,128,255,.12), 0 6px 18px rgba(0,0,0,.35); }
        .qr_marco img { display: block; width: 108px; height: 108px; }
        .qr_texto { flex: 1; min-width: 160px; margin: 0; color: #8b93a7; font-size: .82rem; }
        /* La invitacion al grupo: tarjeta separada del panel y unida a el por el triangulo, igual
           que en la carta. El triangulo es de la tarjeta, apunta hacia arriba, lleva su color y
           mide lo mismo que la separacion. Van dos superpuestos para que tenga contorno. */
        .grupo { position: relative; margin-top: 13px; padding: 24px 18px 20px; text-align: center;
                 background: #191d26; border: 1px solid #2f3542; border-radius: 16px; }
        .grupo::before, .grupo::after { content: ""; position: absolute; left: 50%; width: 0; height: 0;
                                        transform: translateX(-50%); border-left: 13px solid transparent;
                                        border-right: 13px solid transparent; }
        .grupo::before { top: -13px; border-bottom: 13px solid #2f3542; }
        .grupo::after { top: -12px; border-bottom: 13px solid #191d26; }
        .grupo_icono { display: inline-flex; align-items: center; justify-content: center; width: 70px;
                       height: 70px; margin-bottom: 12px; border-radius: 50%; background: #1d55c4;
                       box-shadow: 0 10px 26px rgba(29,85,196,.4); }
        .grupo_icono svg { width: 40px; height: 40px; }
        .grupo_titulo { margin: 0 0 5px; font-size: 1.05rem; font-weight: 700; }
        .grupo_sub { margin: 0 0 16px; color: #8b93a7; font-size: .84rem; }
        .grupo .boton { margin-top: 0; }
        .pie { margin: 18px 0 0; color: #6f7686; font-size: .8rem; text-align: center; }
        /* Play exige que la politica de privacidad sea alcanzable desde la cara publica, no solo
           desde la ficha de la tienda: va en el envoltorio para que la lleven las tres paginas. */
        .pie_legal { margin: 22px 0 0; text-align: center; font-size: .78rem; }
        .pie_legal a { color: #6f7686; }
        .ejemplo { margin: 0 0 14px; padding: 8px 12px; border-radius: 10px; text-align: center;
                   background: rgba(255,196,9,.14); color: #ffca22; font-size: .8rem; }
        </style>
        </head>
        <body>
        <main>
        <p class="marca"><a href="/"><img src="/marca/logo-raveat.png" alt="RavEat"></a></p>
        <p class="marca_lema">Suena bien. Sabe mejor...</p>
        {{cuerpo}}
        <p class="pie_legal"><a href="/privacidad.html">Política de privacidad</a></p>
        </main>
        </body>
        </html>
    """;
}

// Lo que se muestra sin sesion: el pedido y nada del cliente. Es un `record` distinto del detalle
// interno a proposito — si fuera el mismo, agregar un campo adentro lo publicaria sin que nadie
// lo decida.
public sealed record PedidoPublicoResponse(
    string Codigo,
    TipoPedido Tipo,
    EstadoPedido Estado,
    EstadoPago EstadoPago,
    decimal Total,
    DateTimeOffset CreadoEn,
    IReadOnlyList<PedidoPublicoItem> Items);

public sealed record PedidoPublicoItem(string Nombre, int Cantidad, decimal Subtotal);
