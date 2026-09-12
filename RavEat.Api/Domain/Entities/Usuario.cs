namespace RavEat.Api.Domain.Entities;

// RolId es opcional a proposito: un usuario recien creado entra sin rol y no puede hacer nada
// hasta que un administrador se lo asigna. Autenticado no es lo mismo que autorizado, y el modelo
// lo dice desde el tipo.
// La contrasena nunca se guarda: se guarda su hash, generado con PasswordHasher<Usuario>.
public sealed class Usuario : EntityBase {
    public long? RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
