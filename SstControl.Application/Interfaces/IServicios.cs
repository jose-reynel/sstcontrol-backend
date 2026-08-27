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

    /// <summary>Cambia un token de renovación vigente por un JWT nuevo + un token
    /// de renovación nuevo (rotación) — null si el token no existe, ya expiró, o
    /// ya fue usado antes (en cuyo caso además se revocan todos los tokens
    /// activos de ese usuario, ver implementación).</summary>
    Task<ResultadoAutenticacionDto?> RenovarTokenAsync(string tokenRenovacion);

    /// <summary>Revoca un token de renovación específico — cierre de sesión real
    /// del lado del servidor, no solo borrar el token en el cliente.</summary>
    Task CerrarSesionAsync(string tokenRenovacion);
}
