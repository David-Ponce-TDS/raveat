using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RavEat.Api.Auth;
using RavEat.Api.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("No se configuro ConnectionStrings:Default.");
const string corsPolicy = "IonicDevelopment";
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

if(builder.Environment.IsDevelopment() && corsAllowedOrigins.Length == 0){
    corsAllowedOrigins = [
        "http://localhost:8100",
        "http://127.0.0.1:8100",
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "capacitor://localhost",
        "ionic://localhost"
    ];
}

var jwtOpciones = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();
if(string.IsNullOrWhiteSpace(jwtOpciones.Key)) {
    // En Production la clave es obligatoria: preferimos no arrancar antes que firmar con un secreto
    // conocido. En Development se genera una efimera para que el taller corra sin configurar nada.
    if(!builder.Environment.IsDevelopment()) throw new InvalidOperationException("No se configuro Jwt:Key. Definila en User Secrets o en la variable de entorno Jwt__Key antes de publicar.");
    jwtOpciones.Key = TokenService.GenerarClaveEfimera();
}
var tokenService = new TokenService(jwtOpciones);
builder.Services.AddSingleton(jwtOpciones);
builder.Services.AddSingleton(tokenService);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidIssuer = jwtOpciones.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOpciones.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = tokenService.ClaveFirma,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents {
        // Un token firmado no se puede revocar: vale hasta que vence. Por eso en cada request se
        // confirma contra la base que el usuario siga activo y que su rol no haya cambiado desde
        // que se emitio. Una baja o un cambio de rol invalidan el token al instante, en vez de
        // seguir valiendo hasta ocho horas.
        OnTokenValidated = async contexto => {
            var db = contexto.HttpContext.RequestServices.GetRequiredService<RavEatDbContext>();
            var usuarioId = contexto.Principal?.UsuarioId() ?? 0;
            if(usuarioId <= 0) {
                contexto.Fail("Token sin usuario valido.");
                return;
            }
            var usuario = await db.Usuarios.AsNoTracking()
                .Where(x => x.Id == usuarioId)
                .Select(x => new {x.Activo, x.RolId})
                .FirstOrDefaultAsync(contexto.HttpContext.RequestAborted);
            if(usuario is null || !usuario.Activo) {
                contexto.Fail("El usuario ya no esta activo.");
                return;
            }
            var rolActual = usuario.RolId.HasValue
                ? await db.Roles.AsNoTracking().Where(x => x.Id == usuario.RolId.Value && x.Activo).Select(x => x.Codigo).FirstOrDefaultAsync(contexto.HttpContext.RequestAborted)
                : null;
            if(rolActual != contexto.Principal?.RolCodigo()) contexto.Fail("El rol del usuario cambio; volve a iniciar sesion.");
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options => ConfigurarJson(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ConfigurarJson(options.SerializerOptions));
builder.Services.AddDbContext<RavEatDbContext>(options => options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))).UseSnakeCaseNamingConvention());
if(builder.Environment.IsDevelopment()) {
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy => policy.SetIsOriginAllowed(EsOrigenDesarrolloPermitido).AllowAnyHeader().AllowAnyMethod()));
}
else if(corsAllowedOrigins.Length > 0) {
    builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy => policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

var app = builder.Build();

if(app.Environment.IsDevelopment() || corsAllowedOrigins.Length > 0) app.UseCors(corsPolicy);
// Las imagenes de la carta se sirven desde wwwroot/seed/productos.
app.UseStaticFiles();
// El orden importa: autenticacion (quien sos) antes que autorizacion (que podes hacer).
app.UseAuthentication();
app.UseAuthorization();

if(args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)){
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RavEatDbContext>();
    await db.Database.MigrateAsync();
}

app.MapControllers();
// El primer endpoint que se prueba: si /health no responde, no hay nada mas que revisar del lado
// del frontend. Tambien es lo que confirma que el telefono llega hasta la maquina de desarrollo.
app.MapGet("/health", () => Results.Ok(new {status = "ok", utc = DateTimeOffset.UtcNow}));
app.Run();

// El JSON que intercambian API y frontend va en snake_case, igual que las columnas de MySQL: una
// sola convencion de nombres de punta a punta evita mapear a mano en cada service del front.
static void ConfigurarJson(JsonSerializerOptions options){
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
}

// En desarrollo el origen no se puede listar a mano: el telefono entra por la IP de la red local,
// que cambia de casa en casa. Se acepta cualquier IP privada, localhost y los esquemas propios de
// Capacitor; nunca una direccion publica.
static bool EsOrigenDesarrolloPermitido(string origin) {
    if(!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
    if(uri.Scheme is "capacitor" or "ionic" && uri.Host == "localhost") return true;
    if(uri.Scheme is not ("http" or "https")) return false;
    if(uri.Host is "localhost" or "127.0.0.1") return true;
    return IPAddress.TryParse(uri.Host, out var ip) && EsIpPrivada(ip);
}

static bool EsIpPrivada(IPAddress ip) {
    if(IPAddress.IsLoopback(ip)) return true;
    var bytes = ip.GetAddressBytes();
    return ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && (
        bytes[0] == 10 ||
        bytes[0] == 192 && bytes[1] == 168 ||
        bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31
    );
}
