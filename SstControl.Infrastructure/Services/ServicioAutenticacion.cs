using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// Valida credenciales y emite el token JWT que el frontend usa en cada petición,
/// más un token de renovación de larga duración para obtener un JWT nuevo sin
/// pedir la contraseña de nuevo. El JWT incluye, como claims, TODOS los roles del
/// usuario y TODOS los permisos efectivos que esos roles le otorgan
/// (Usuario → Rol → Perfil → Permiso) — así la API puede autorizar por permiso
/// puntual sin volver a consultar la base de datos en cada petición.
///
/// El token de renovación se rota en cada uso: RenovarTokenAsync revoca el que
/// se usó y entrega uno nuevo. Si alguien intenta reusar uno ya revocado (señal
/// de que fue robado y el dueño legítimo ya lo usó), se revocan todos los
/// tokens activos de ese usuario como medida de contención.
/// </summary>
public class ServicioAutenticacion : IServicioAutenticacion
{
    /// <summary>Nombre del claim personalizado donde se guarda cada permiso del usuario.</summary>
    public const string TipoClaimPermiso = "permiso";
    private const int DiasVigenciaTokenRenovacionPorDefecto = 30;

    private readonly ContextoBaseDatos _contexto;
    private readonly IConfiguration _configuracion;
    public ServicioAutenticacion(ContextoBaseDatos contexto, IConfiguration configuracion)
    { _contexto = contexto; _configuracion = configuracion; }

    public async Task<ResultadoAutenticacionDto?> IniciarSesionAsync(string nombreUsuario, string clave)
    {
        var usuario = await CargarUsuarioConPermisosAsync(u => u.NombreUsuario == nombreUsuario);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(clave, usuario.ClaveHash)) return null;

        var tokenRenovacion = await CrearTokenRenovacionAsync(usuario.IdUsuario);
        return ConstruirResultado(usuario, tokenRenovacion);
    }

    public async Task<ResultadoAutenticacionDto?> RenovarTokenAsync(string tokenRenovacion)
    {
        var tokenExistente = await _contexto.TokensRenovacion.FirstOrDefaultAsync(t => t.Token == tokenRenovacion);
        if (tokenExistente is null) return null;

        if (!tokenExistente.Vigente)
        {
            if (tokenExistente.Revocado)
                await RevocarTodosLosTokensAsync(tokenExistente.IdUsuario);
            return null;
        }

        var usuario = await CargarUsuarioConPermisosAsync(u => u.IdUsuario == tokenExistente.IdUsuario);
        if (usuario is null) return null;

        var nuevoTokenRenovacion = await CrearTokenRenovacionAsync(usuario.IdUsuario);
        tokenExistente.FechaRevocacion = DateTimeOffset.UtcNow;
        tokenExistente.ReemplazadoPor = nuevoTokenRenovacion;
        await _contexto.SaveChangesAsync();

        return ConstruirResultado(usuario, nuevoTokenRenovacion);
    }

    public async Task CerrarSesionAsync(string tokenRenovacion)
    {
        var token = await _contexto.TokensRenovacion.FirstOrDefaultAsync(t => t.Token == tokenRenovacion);
        if (token is not null && !token.Revocado)
        {
            token.FechaRevocacion = DateTimeOffset.UtcNow;
            await _contexto.SaveChangesAsync();
        }
    }

    /// <summary>Trae al usuario con toda su cadena de autorización de una sola vez:
    /// sus roles → los perfiles de cada rol → los permisos de cada perfil.</summary>
    private async Task<Usuario?> CargarUsuarioConPermisosAsync(Expression<Func<Usuario, bool>> filtro) =>
        await _contexto.Usuarios
            .Include(u => u.Roles).ThenInclude(ur => ur.Rol).ThenInclude(r => r.Perfiles).ThenInclude(rp => rp.Perfil).ThenInclude(p => p.Permisos).ThenInclude(pp => pp.Permiso)
            .FirstOrDefaultAsync(filtro);

    private async Task<string> CrearTokenRenovacionAsync(int idUsuario)
    {
        var valor = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var dias = int.TryParse(_configuracion["Jwt:DiasVigenciaTokenRenovacion"], out var valorConfigurado)
            ? valorConfigurado : DiasVigenciaTokenRenovacionPorDefecto;

        _contexto.TokensRenovacion.Add(new TokenRenovacion
        {
            Token = valor,
            IdUsuario = idUsuario,
            FechaExpiracion = DateTimeOffset.UtcNow.AddDays(dias),
        });
        await _contexto.SaveChangesAsync();
        return valor;
    }

    private async Task RevocarTodosLosTokensAsync(int idUsuario)
    {
        var tokensActivos = await _contexto.TokensRenovacion
            .Where(t => t.IdUsuario == idUsuario && t.FechaRevocacion == null)
            .ToListAsync();
        foreach (var token in tokensActivos)
            token.FechaRevocacion = DateTimeOffset.UtcNow;
        await _contexto.SaveChangesAsync();
    }

    private ResultadoAutenticacionDto ConstruirResultado(Usuario usuario, string tokenRenovacion)
    {
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
        var minutosExpiracion = int.TryParse(_configuracion["Jwt:MinutosExpiracion"], out var valor) ? valor : 480;
        var token = new JwtSecurityToken(
            issuer: _configuracion["Jwt:Issuer"], audience: _configuracion["Jwt:Audience"],
            claims: reclamos, expires: DateTime.UtcNow.AddMinutes(minutosExpiracion), signingCredentials: credenciales);

        return new ResultadoAutenticacionDto(new JwtSecurityTokenHandler().WriteToken(token), tokenRenovacion,
            usuario.NombreCompleto, nombresRoles, codigosPermiso);
    }
}
