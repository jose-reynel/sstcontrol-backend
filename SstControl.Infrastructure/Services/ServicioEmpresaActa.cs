using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>Administración de empresas clientes y sus sedes.</summary>
public class ServicioEmpresa : IServicioEmpresa
{
    private readonly ContextoBaseDatos _contexto;
    public ServicioEmpresa(ContextoBaseDatos contexto) => _contexto = contexto;

    /// <summary>Lista todas las empresas con sus sedes anidadas — usado en el panel de Administración.</summary>
    public async Task<IReadOnlyList<EmpresaDto>> ObtenerTodasConSedesAsync()
    {
        return await _contexto.Empresas.Include(e => e.Sedes)
            .Select(e => new EmpresaDto(e.IdEmpresa, e.Nombre,
                e.Sedes.Select(s => new SedeDto(s.IdSede, s.IdEmpresa, s.Nombre)).ToList()))
            .ToListAsync();
    }

    public async Task<EmpresaDto> CrearEmpresaAsync(string nombre)
    {
        var empresa = new Empresa { Nombre = nombre };
        _contexto.Empresas.Add(empresa);
        await _contexto.SaveChangesAsync();
        return new EmpresaDto(empresa.IdEmpresa, empresa.Nombre, new List<SedeDto>());
    }

    public async Task<SedeDto> CrearSedeAsync(int idEmpresa, string nombre)
    {
        var sede = new Sede { IdEmpresa = idEmpresa, Nombre = nombre };
        _contexto.Sedes.Add(sede);
        await _contexto.SaveChangesAsync();
        return new SedeDto(sede.IdSede, sede.IdEmpresa, sede.Nombre);
    }
}

/// <summary>Gestión de actas de reuniones y capacitaciones registradas manualmente
/// (vía el asistente guiado) — la sincronización automática vive en ServicioSincronizacionReuniones.</summary>
public class ServicioActa : IServicioActa
{
    private readonly ContextoBaseDatos _contexto;
    public ServicioActa(ContextoBaseDatos contexto) => _contexto = contexto;

    /// <summary>Lista paginada de actas, de la más reciente a la más antigua.</summary>
    public async Task<PaginaDto<ActaDto>> ObtenerPaginadoAsync(int pagina, int tamanioPagina)
    {
        pagina = Math.Max(1, pagina);
        tamanioPagina = Math.Clamp(tamanioPagina, 1, 100);

        var consulta = _contexto.Actas.AsNoTracking()
            .Include(a => a.UsuarioCreador)
            .OrderByDescending(a => a.Fecha);

        var total = await consulta.CountAsync();
        var elementos = await consulta
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(a => new ActaDto(a.IdActa, a.IdEmpresa, a.IdSede, a.Tipo.ToString(), a.Titulo, a.Fecha,
                a.Asistentes, a.Notas, a.UsuarioCreador.NombreCompleto))
            .ToListAsync();

        return new PaginaDto<ActaDto>(elementos, pagina, tamanioPagina, total);
    }

    public async Task<ActaDto> CrearAsync(CrearActaDto datos, int idUsuarioCreador)
    {
        var entidad = new Acta
        {
            IdEmpresa = datos.IdEmpresa,
            IdSede = datos.IdSede,
            Tipo = Enum.Parse<TipoActa>(datos.Tipo, ignoreCase: true),
            Titulo = datos.Titulo,
            Fecha = datos.Fecha,
            Asistentes = datos.Asistentes,
            Notas = datos.Notas,
            IdUsuarioCreador = idUsuarioCreador,
        };
        _contexto.Actas.Add(entidad);
        await _contexto.SaveChangesAsync();
        var usuario = await _contexto.Usuarios.FindAsync(idUsuarioCreador);
        return new ActaDto(entidad.IdActa, entidad.IdEmpresa, entidad.IdSede, entidad.Tipo.ToString(),
            entidad.Titulo, entidad.Fecha, entidad.Asistentes, entidad.Notas, usuario!.NombreCompleto);
    }
}
