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

/// <summary>
/// Valida credenciales y emite el token JWT que la PWA usará en cada petición.
/// El token incluye, como claims, TODOS los roles del usuario y TODOS los permisos
/// efectivos que esos roles le otorgan (Usuario → Rol → Perfil → Permiso) — así la
/// API puede autorizar por permiso puntual sin volver a consultar la base de datos
/// en cada petición.
/// </summary>
public class ServicioAutenticacion : IServicioAutenticacion
{
    /// <summary>Nombre del claim personalizado donde se guarda cada permiso del usuario.</summary>
    public const string TipoClaimPermiso = "permiso";

    private readonly ContextoBaseDatos _contexto;
    private readonly IConfiguration _configuracion;
    public ServicioAutenticacion(ContextoBaseDatos contexto, IConfiguration configuracion)
    { _contexto = contexto; _configuracion = configuracion; }

    public async Task<ResultadoAutenticacionDto?> IniciarSesionAsync(string nombreUsuario, string clave)
    {
        // Trae al usuario con toda su cadena de autorización de una sola vez:
        // sus roles → los perfiles de cada rol → los permisos de cada perfil.
        var usuario = await _contexto.Usuarios
            .Include(u => u.Roles).ThenInclude(ur => ur.Rol).ThenInclude(r => r.Perfiles).ThenInclude(rp => rp.Perfil).ThenInclude(p => p.Permisos).ThenInclude(pp => pp.Permiso)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(clave, usuario.ClaveHash)) return null;

        var nombresRoles = usuario.Roles.Select(ur => ur.Rol.Nombre).Distinct().ToList();

        // Aplana la jerarquía Rol → Perfil → Permiso en una lista única de códigos de permiso.
        var codigosPermiso = usuario.Roles
            .SelectMany(ur => ur.Rol.Perfiles)
            .SelectMany(rp => rp.Perfil.Permisos)
            .Select(pp => pp.Permiso.Codigo)
            .Distinct()
            .ToList();

        var reclamos = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
        };
        reclamos.AddRange(nombresRoles.Select(nombreRol => new Claim(ClaimTypes.Role, nombreRol)));
        reclamos.AddRange(codigosPermiso.Select(codigo => new Claim(TipoClaimPermiso, codigo)));

        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion["Jwt:Key"]!));
        var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Issuer"], audience: _configuracion["Jwt:Audience"],
            claims: reclamos, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credenciales);

        return new ResultadoAutenticacionDto(new JwtSecurityTokenHandler().WriteToken(token),
            usuario.NombreCompleto, nombresRoles, codigosPermiso);
    }
}
