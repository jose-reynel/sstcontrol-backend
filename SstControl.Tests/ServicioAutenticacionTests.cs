using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Servicios;
using Xunit;

namespace SstControl.Tests;

/// <summary>
/// Prueba el flujo completo de autenticación con refresh tokens: emisión,
/// rotación al renovar, y la medida de contención ante reutilización de un
/// token ya revocado (indicio de robo) — la parte más delicada del Bucle 3.
/// </summary>
public class ServicioAutenticacionTests
{
    private const string ClaveDePrueba = "clave-de-prueba-con-al-menos-32-caracteres-para-firmar-el-jwt";

    private static IConfiguration CrearConfiguracion() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "SstControlApi.Tests",
            ["Jwt:Audience"] = "SstControlPwa.Tests",
            ["Jwt:Key"] = ClaveDePrueba,
            ["Jwt:MinutosExpiracion"] = "60",
            ["Jwt:DiasVigenciaTokenRenovacion"] = "30",
        })
        .Build();

    private static async Task<(ContextoBaseDatos Contexto, Usuario Usuario)> SembrarUsuarioAsync(string clave = "Sst2026!")
    {
        var opciones = new DbContextOptionsBuilder<ContextoBaseDatos>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contexto = new ContextoBaseDatos(opciones);

        var usuario = new Usuario
        {
            NombreUsuario = "prueba.usuario",
            NombreCompleto = "Usuario de Prueba",
            ClaveHash = BCrypt.Net.BCrypt.HashPassword(clave),
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        return (contexto, usuario);
    }

    [Fact]
    public async Task IniciarSesionAsync_ConCredencialesValidas_DevuelveJwtYTokenDeRenovacion()
    {
        var (contexto, _) = await SembrarUsuarioAsync("Sst2026!");
        var servicio = new ServicioAutenticacion(contexto, CrearConfiguracion());

        var resultado = await servicio.IniciarSesionAsync("prueba.usuario", "Sst2026!");

        Assert.NotNull(resultado);
        Assert.False(string.IsNullOrWhiteSpace(resultado!.Token));
        Assert.False(string.IsNullOrWhiteSpace(resultado.TokenRenovacion));
    }

    [Fact]
    public async Task IniciarSesionAsync_ConClaveIncorrecta_DevuelveNull()
    {
        var (contexto, _) = await SembrarUsuarioAsync("Sst2026!");
        var servicio = new ServicioAutenticacion(contexto, CrearConfiguracion());

        var resultado = await servicio.IniciarSesionAsync("prueba.usuario", "clave-equivocada");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task RenovarTokenAsync_ConTokenVigente_RotaElToken_YElAnteriorQuedaInvalido()
    {
        var (contexto, _) = await SembrarUsuarioAsync();
        var servicio = new ServicioAutenticacion(contexto, CrearConfiguracion());
        var inicioSesion = await servicio.IniciarSesionAsync("prueba.usuario", "Sst2026!");

        var renovado = await servicio.RenovarTokenAsync(inicioSesion!.TokenRenovacion);
        var segundoIntentoConElAnterior = await servicio.RenovarTokenAsync(inicioSesion.TokenRenovacion);

        Assert.NotNull(renovado);
        Assert.NotEqual(inicioSesion.TokenRenovacion, renovado!.TokenRenovacion);
        Assert.Null(segundoIntentoConElAnterior); // el token viejo ya fue rotado — no sirve más
    }

    [Fact]
    public async Task RenovarTokenAsync_ReusandoUnTokenYaRevocado_RevocaTambienElTokenVigenteActual()
    {
        var (contexto, _) = await SembrarUsuarioAsync();
        var servicio = new ServicioAutenticacion(contexto, CrearConfiguracion());
        var inicioSesion = await servicio.IniciarSesionAsync("prueba.usuario", "Sst2026!");
        var primeraRenovacion = await servicio.RenovarTokenAsync(inicioSesion!.TokenRenovacion);

        // Alguien reutiliza el token de renovación original (ya rotado) — señal de robo.
        var intentoConTokenRobado = await servicio.RenovarTokenAsync(inicioSesion.TokenRenovacion);
        // Como medida de contención, el token vigente actual (legítimo) también debería quedar inservible.
        var intentoConElTokenLegitimoVigente = await servicio.RenovarTokenAsync(primeraRenovacion!.TokenRenovacion);

        Assert.Null(intentoConTokenRobado);
        Assert.Null(intentoConElTokenLegitimoVigente);
    }

    [Fact]
    public async Task CerrarSesionAsync_RevocaElToken_YaNoSirveParaRenovar()
    {
        var (contexto, _) = await SembrarUsuarioAsync();
        var servicio = new ServicioAutenticacion(contexto, CrearConfiguracion());
        var inicioSesion = await servicio.IniciarSesionAsync("prueba.usuario", "Sst2026!");

        await servicio.CerrarSesionAsync(inicioSesion!.TokenRenovacion);
        var intentoDeRenovar = await servicio.RenovarTokenAsync(inicioSesion.TokenRenovacion);

        Assert.Null(intentoDeRenovar);
    }
}
