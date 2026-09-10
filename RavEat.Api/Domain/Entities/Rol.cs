namespace RavEat.Api.Domain.Entities;

// El rol es una fila de la base y no un enum en C#: un administrador tiene que poder verlos y
// asignarlos desde la app sin recompilar. El `Codigo` es lo que viaja en el token y lo que
// nombran las politicas de autorizacion.
public sealed class Rol : EntityBase {
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
