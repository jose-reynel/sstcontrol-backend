using SstControl.Aplicacion.DTOs;

namespace SstControl.Aplicacion.Interfaces;

/// <summary>
/// Reglas de negocio del ciclo documental: captura → control de tiempo →
/// firma de aprobación → renovación.
/// </summary>
public interface IServicioDocumento
{
    /// <summary>Lista paginada (más reciente primero) — evita traer toda la tabla
    /// en una sola respuesta a medida que crece el histórico documental.</summary>
    Task<PaginaDto<DocumentoDto>> ObtenerPaginadoAsync(int pagina, int tamanioPagina);
    Task<DocumentoDto> CrearAsync(CrearDocumentoDto datos);

    /// <summary>Marca el documento como aprobado y registra quién lo firmó.</summary>
    Task<DocumentoDto> FirmarAsync(int idDocumento, int idUsuarioAprueba);

    /// <summary>Crea un nuevo registro pendiente a partir de uno vencido o por vencer,
    /// cerrando el ciclo de renovación sin perder el histórico del original.</summary>
    Task<DocumentoDto> RenovarAsync(int idDocumento);

    Task EliminarAsync(int idDocumento);
}

/// <summary>Gestión de empresas clientes y sus sedes.</summary>
public interface IServicioEmpresa
{
    Task<IReadOnlyList<EmpresaDto>> ObtenerTodasConSedesAsync();
    Task<EmpresaDto> CrearEmpresaAsync(string nombre);
    Task<SedeDto> CrearSedeAsync(int idEmpresa, string nombre);
}

/// <summary>Gestión de actas de reuniones y capacitaciones.</summary>
public interface IServicioActa
{
    Task<PaginaDto<ActaDto>> ObtenerPaginadoAsync(int pagina, int tamanioPagina);
    Task<ActaDto> CrearAsync(CrearActaDto datos, int idUsuarioCreador);
}

/// <summary>Autenticación de usuarios y emisión de tokens JWT.</summary>
public interface IServicioAutenticacion
{
    Task<ResultadoAutenticacionDto?> IniciarSesionAsync(string nombreUsuario, string clave);
}
