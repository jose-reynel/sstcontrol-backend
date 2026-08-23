using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>Valida credenciales contra la base de datos y emite el token JWT
/// que la PWA usará en cada petición (header Authorization: Bearer &lt;token&gt;).</summary>
public class ServicioAutenticacion : IServicioAutenticacion
{
    private readonly ContextoBaseDatos _contexto;
    private readonly IConfiguration _configuracion;
    public ServicioAutenticacion(ContextoBaseDatos contexto, IConfiguration configuracion)
    { _contexto = contexto; _configuracion = configuracion; }

    public async Task<ResultadoAutenticacionDto?> IniciarSesionAsync(string nombreUsuario, string clave)
    {
        var usuario = await _contexto.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(clave, usuario.ClaveHash)) return null;

        var reclamos = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
        };
        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion["Jwt:Key"]!));
        var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Issuer"], audience: _configuracion["Jwt:Audience"],
            claims: reclamos, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credenciales);

        return new ResultadoAutenticacionDto(new JwtSecurityTokenHandler().WriteToken(token), usuario.NombreCompleto, usuario.Rol.Nombre);
    }
}
