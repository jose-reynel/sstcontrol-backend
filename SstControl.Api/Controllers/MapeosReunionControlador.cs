using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Integraciones;

namespace SstControl.Api.Controladores;

/// <summary>
/// Administra la correlación entre una reunión externa (Teams/Google Meet/Zoom/
/// Webex) y la empresa/sede a la que pertenece — configúralo una vez por cada
/// cuenta/organizador que vaya a sincronizarse automáticamente vía webhook. Ver
/// docs/manuales-tecnicos/backend/04-seguridad-rbac-e-integraciones.md.
/// Requiere el permiso "reuniones.sincronizar".
/// </summary>
[ApiController]
[Authorize(Policy = "reuniones.sincronizar")]
[Route("api/mapeos-reunion")]
public class MapeosReunionControlador : ControllerBase
{
    private readonly IServicioMapeoReunion _servicioMapeo;
    public MapeosReunionControlador(IServicioMapeoReunion servicioMapeo) => _servicioMapeo = servicioMapeo;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MapeoOrigenReunionDto>>> ObtenerTodos() =>
        Ok(await _servicioMapeo.ObtenerTodosAsync());

    [HttpPost]
    public async Task<ActionResult<MapeoOrigenReunionDto>> Crear(CrearMapeoOrigenReunionDto datos) =>
        Ok(await _servicioMapeo.CrearAsync(datos));
}
