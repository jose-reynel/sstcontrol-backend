using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.Interfaces;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Repositorios;

/// <summary>Implementación genérica de IRepositorio&lt;T&gt; usando Entity Framework Core.</summary>
public class RepositorioEf<T> : IRepositorio<T> where T : class
{
    private readonly ContextoBaseDatos _contexto;
    private readonly DbSet<T> _conjunto;

    public RepositorioEf(ContextoBaseDatos contexto)
    {
        _contexto = contexto;
        _conjunto = contexto.Set<T>();
    }

    public async Task<T?> ObtenerPorIdAsync(int id) => await _conjunto.FindAsync(id);
    public async Task<IReadOnlyList<T>> ObtenerTodosAsync() => await _conjunto.AsNoTracking().ToListAsync();
    public async Task AgregarAsync(T entidad) => await _conjunto.AddAsync(entidad);
    public void Actualizar(T entidad) => _conjunto.Update(entidad);
    public void Eliminar(T entidad) => _conjunto.Remove(entidad);
    public async Task<int> GuardarCambiosAsync() => await _contexto.SaveChangesAsync();
}
