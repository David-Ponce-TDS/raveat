using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RavEat.Api.Domain.Entities;

namespace RavEat.Api.Auth;

// Opciones del token de acceso. La clave nunca tiene default: en Production, si falta, la API no
// arranca. Preferimos no levantar antes que firmar con un secreto que este en el repositorio.
public sealed class JwtOpciones {
    public const string Seccion = "Jwt";
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RavEat.Api";
    public string Audience { get; set; } = "RavEatApp";
    public int HorasVigencia { get; set; } = 8;
    // El refresh vive mucho mas que el access token: es lo que evita tener que escribir la
    // contrasena cada vez que vence la sesion corta.
    public int DiasVigenciaRefresh { get; set; } = 30;
}

public sealed class TokenService(JwtOpciones opciones) {
    // Claim propio: el JSON de la app va en snake_case, igual que el resto del contrato.
    public const string ClaimRolCodigo = "rol_codigo";

    public SecurityKey ClaveFirma { get; } = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key));

    public (string Token, DateTimeOffset ExpiraEn) Emitir(Usuario usuario, Rol? rol) {
        var expiraEn = DateTimeOffset.UtcNow.AddHours(opciones.HorasVigencia);
        var claims = new List<Claim> {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        // Sin rol, el token existe pero no habilita ninguna politica: el usuario esta autenticado
        // y no autorizado. Es exactamente la distincion del modulo.
        if(rol is not null) {
            claims.Add(new Claim(ClaimRolCodigo, rol.Codigo));
            claims.Add(new Claim(ClaimTypes.Role, rol.Codigo));
        }

        var token = new JwtSecurityToken(
            issuer: opciones.Issuer,
            audience: opciones.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEn.UtcDateTime,
            signingCredentials: new SigningCredentials(ClaveFirma, SecurityAlgorithms.HmacSha256)
        );
        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    // El refresh es un secreto **opaco**, no un JWT: no lleva datos adentro y solo sirve para pedir
    // un access token nuevo. Se devuelve en claro una unica vez; en la base queda solo el hash.
    public (string Token, string Hash, DateTimeOffset ExpiraEn) EmitirRefresh() {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return (token, HashRefresh(token), DateTimeOffset.UtcNow.AddDays(opciones.DiasVigenciaRefresh));
    }

    public static string HashRefresh(string token) {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    // Clave efimera para que el taller arranque sin configurar nada en Development. Los tokens
    // dejan de valer al reiniciar la API, que es lo que se espera de un entorno de prueba.
    public static string GenerarClaveEfimera() {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
