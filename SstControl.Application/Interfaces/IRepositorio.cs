namespace SstControl.Aplicacion.Interfaces;

/// <summary>
/// Puerto genérico de acceso a datos. La capa Aplicación depende solo de esta
/// abstracción, nunca de EF Core directamente — esa dependencia vive en Infraestructura.
/// </summary>
public interface IRepositorio<T> where T : class
{
    /// <summary>Busca una entidad por su identificador. Devuelve null si no existe.</summary>
    Task<T?> ObtenerPorIdAsync(int id);

    /// <summary>Devuelve todas las entidades del tipo T.</summary>
    Task<IReadOnlyList<T>> ObtenerTodosAsync();

    /// <summary>Agrega una nueva entidad (aún no persistida hasta llamar GuardarCambiosAsync).</summary>
    Task AgregarAsync(T entidad);

    /// <summary>Marca una entidad existente como modificada.</summary>
    void Actualizar(T entidad);

    /// <summary>Marca una entidad para ser eliminada.</summary>
    void Eliminar(T entidad);

    /// <summary>Persiste en la base de datos todos los cambios pendientes.</summary>
    Task<int> GuardarCambiosAsync();
}
