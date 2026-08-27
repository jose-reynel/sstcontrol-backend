using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>Datos de inicio de sesión enviados desde el frontend.</summary>
public record PeticionInicioSesion(
    [property: Required(AllowEmptyStrings = false)] string NombreUsuario,
    [property: Required(AllowEmptyStrings = false)] string Clave);

/// <summary>Token de renovación enviado para obtener un JWT nuevo, o para cerrar sesión.</summary>
public record PeticionTokenRenovacion([property: Required(AllowEmptyStrings = false)] string TokenRenovacion);

/// <summary>Autenticación de usuarios: valida credenciales, entrega y renueva el token JWT.</summary>
[ApiController]
[Route("api/autenticacion")]
public class AutenticacionControlador : ControllerBase
{
    private readonly IServicioAutenticacion _servicioAutenticacion;
    public AutenticacionControlador(IServicioAutenticacion servicioAutenticacion) => _servicioAutenticacion = servicioAutenticacion;

    /// <summary>POST /api/autenticacion/iniciar-sesion — valida usuario y contraseña.
    /// Limitado a 5 intentos por minuto por IP (política "inicio-sesion", ver
    /// Program.cs) para mitigar ataques de fuerza bruta contra credenciales.</summary>
    [HttpPost("iniciar-sesion")]
    [EnableRateLimiting("inicio-sesion")]
    public async Task<IActionResult> IniciarSesion(PeticionInicioSesion peticion)
    {
        var resultado = await _servicioAutenticacion.IniciarSesionAsync(peticion.NombreUsuario, peticion.Clave);
        if (resultado is null) return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
        return Ok(resultado);
    }

    /// <summary>POST /api/autenticacion/renovar-token — cambia un token de renovación
    /// vigente por un JWT nuevo (y un token de renovación nuevo, rotado). El
    /// frontend lo llama automáticamente cuando el JWT expira, sin pedir la
    /// contraseña de nuevo. También limitado por IP: es la misma superficie de
    /// ataque que el login si alguien intenta adivinar tokens.</summary>
    [HttpPost("renovar-token")]
    [EnableRateLimiting("inicio-sesion")]
    public async Task<IActionResult> RenovarToken(PeticionTokenRenovacion peticion)
    {
        var resultado = await _servicioAutenticacion.RenovarTokenAsync(peticion.TokenRenovacion);
        if (resultado is null) return Unauthorized(new { mensaje = "La sesión expiró. Vuelve a iniciar sesión." });
        return Ok(resultado);
    }

    /// <summary>POST /api/autenticacion/cerrar-sesion — revoca el token de renovación
    /// del lado del servidor. Sin esto, "cerrar sesión" en el cliente solo borraba
    /// el token localmente — alguien con una copia del token (ej. un dispositivo
    /// robado antes del logout) seguía pudiendo usarlo para renovar indefinidamente.
    /// Sin [Authorize] a propósito: el propio valor del token de renovación (64
    /// bytes aleatorios) ya es el secreto que autoriza revocarlo — exigir además
    /// un JWT vigente rompería el logout justo en el caso más común de querer
    /// cerrar sesión con el JWT ya expirado.</summary>
    [HttpPost("cerrar-sesion")]
    [AllowAnonymous]
    public async Task<IActionResult> CerrarSesion(PeticionTokenRenovacion peticion)
    {
        await _servicioAutenticacion.CerrarSesionAsync(peticion.TokenRenovacion);
        return NoContent();
    }
}
