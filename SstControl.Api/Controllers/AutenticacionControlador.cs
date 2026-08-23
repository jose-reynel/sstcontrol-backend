using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>Datos de inicio de sesión enviados desde la PWA.</summary>
public record PeticionInicioSesion(string NombreUsuario, string Clave);

/// <summary>Autenticación de usuarios: valida credenciales y entrega el token JWT.</summary>
[ApiController]
[Route("api/autenticacion")]
public class AutenticacionControlador : ControllerBase
{
    private readonly IServicioAutenticacion _servicioAutenticacion;
    public AutenticacionControlador(IServicioAutenticacion servicioAutenticacion) => _servicioAutenticacion = servicioAutenticacion;

    /// <summary>POST /api/autenticacion/iniciar-sesion — valida usuario y contraseña.</summary>
    [HttpPost("iniciar-sesion")]
    public async Task<IActionResult> IniciarSesion(PeticionInicioSesion peticion)
    {
        var resultado = await _servicioAutenticacion.IniciarSesionAsync(peticion.NombreUsuario, peticion.Clave);
        if (resultado is null) return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
        return Ok(resultado);
    }
}
