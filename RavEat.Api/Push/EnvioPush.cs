using System.Text.RegularExpressions;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using RavEat.Api.Data;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Push;

// El envio de la push, hecho por la API. El telefono no recibe nada directo de nosotros: la API le
// pide a FCM que entregue el mensaje en cada token del usuario, y FCM lo lleva. Para pedirlo, la
// API se identifica con la cuenta de servicio del proyecto de Firebase (un JSON con clave privada,
// distinto del google-services.json de la app, que es publico).
//
// Sin la cuenta de servicio configurada no se envia nada y la API arranca igual: la bandeja sigue
// funcionando, que es lo que importa. El push es un aviso extra, nunca una condicion.
public sealed class EnvioPush {
    private readonly FirebaseMessaging? mensajeria;
    private readonly ILogger<EnvioPush> logger;

    public EnvioPush(IConfiguration configuracion, IHostEnvironment entorno, ILogger<EnvioPush> logger) {
        this.logger = logger;
        var ruta = configuracion["Firebase:CuentaDeServicio"];
        if(string.IsNullOrWhiteSpace(ruta)) {
            logger.LogInformation("Push apagado: falta Firebase:CuentaDeServicio. La bandeja sigue funcionando.");
            return;
        }
        // Relativa, se resuelve contra la carpeta de la API: en el hosting el archivo se sube al
        // lado de la DLL y alcanza con el nombre.
        var credencial = CredentialFactory.FromFile<ServiceAccountCredential>(Path.Combine(entorno.ContentRootPath, ruta)).ToGoogleCredential();
        mensajeria = FirebaseMessaging.GetMessaging(FirebaseApp.Create(new AppOptions {Credential = credencial}));
    }

    // Una push por cada notificacion nueva, a todos los telefonos de su destinatario. El codigo del
    // pedido viaja en los datos: es lo que la app usa para abrirlo al tocar el aviso, igual que la
    // bandeja lo saca del titulo.
    public async Task EnviarAsync(RavEatDbContext db, IReadOnlyCollection<Notificacion> notificaciones, CancellationToken cancellationToken) {
        if(mensajeria is null) return;
        var usuarios = notificaciones.Select(x => x.UsuarioId).Distinct().ToList();
        var dispositivos = await db.DispositivosPush.Where(x => usuarios.Contains(x.UsuarioId)).ToListAsync(cancellationToken);
        var vencidos = new List<DispositivoPush>();

        foreach(var notificacion in notificaciones) {
            var destino = dispositivos.Where(x => x.UsuarioId == notificacion.UsuarioId).ToList();
            if(destino.Count == 0) continue;
            var codigo = Regex.Match(notificacion.Titulo, "PED-[0-9]+").Value;
            // Prioridad alta y el canal "avisos" de la app: con los dos, el telefono la muestra como
            // banner flotante al instante, aunque este con la pantalla apagada. Sin prioridad alta,
            // Android la demora para ahorrar bateria.
            var respuesta = await mensajeria.SendEachAsync(destino.Select(dispositivo => new Message {
                Token = dispositivo.Token,
                Notification = new Notification {Title = notificacion.Titulo, Body = notificacion.Mensaje},
                Data = new Dictionary<string, string> {["pedido_codigo"] = codigo},
                Android = new AndroidConfig {Priority = Priority.High, Notification = new AndroidNotification {ChannelId = "avisos"}}
            }), cancellationToken);

            // FCM contesta token por token. El que volvio "no registrado" es de una app
            // desinstalada o de un token rotado: se borra para no seguir mandandole.
            for(var i = 0; i < respuesta.Responses.Count; i++) {
                var error = respuesta.Responses[i].Exception;
                if(error is null) continue;
                if(error.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument) vencidos.Add(destino[i]);
                else logger.LogWarning(error, "FCM no entrego la push a un dispositivo del usuario {UsuarioId}.", notificacion.UsuarioId);
            }
        }

        if(vencidos.Count == 0) return;
        db.DispositivosPush.RemoveRange(vencidos.Distinct());
        await db.SaveChangesAsync(cancellationToken);
    }
}
