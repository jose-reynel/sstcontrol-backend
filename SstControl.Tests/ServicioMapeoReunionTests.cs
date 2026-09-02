using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Integraciones;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Servicios;
using Xunit;

namespace SstControl.Tests;

/// <summary>
/// Prueba la resolución de empresa/sede a partir del token de correlación de
/// un webhook — la pieza que cierra la sincronización automática de Teams y
/// Google Meet (antes un TODO).
/// </summary>
public class ServicioMapeoReunionTests
{
    private static async Task<(ContextoBaseDatos Contexto, int IdEmpresa, int IdSede, int IdUsuario)> SembrarBaseAsync()
    {
        var opciones = new DbContextOptionsBuilder<ContextoBaseDatos>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contexto = new ContextoBaseDatos(opciones);

        var empresa = new Empresa { Nombre = "Constructora Andina S.A.S." };
        contexto.Empresas.Add(empresa);
        await contexto.SaveChangesAsync();

        var sede = new Sede { IdEmpresa = empresa.IdEmpresa, Nombre = "Obra Torre Norte — Bogotá" };
        contexto.Sedes.Add(sede);

        var usuario = new Usuario { NombreUsuario = "miguel.torres", ClaveHash = "x", NombreCompleto = "Miguel Torres Salazar" };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();

        return (contexto, empresa.IdEmpresa, sede.IdSede, usuario.IdUsuario);
    }

    [Fact]
    public async Task CrearAsync_YResolverAsync_DevuelvenLaMismaEmpresaYSede()
    {
        var (contexto, idEmpresa, idSede, idUsuario) = await SembrarBaseAsync();
        var servicio = new ServicioMapeoReunion(contexto);

        await servicio.CrearAsync(new CrearMapeoOrigenReunionDto(
            "Teams", "andina-torre-norte-2026", idEmpresa, idSede, idUsuario, "Suscripción de Miguel Torres"));

        var resuelto = await servicio.ResolverAsync(ProveedorReunion.Teams, "andina-torre-norte-2026");

        Assert.NotNull(resuelto);
        Assert.Equal(idEmpresa, resuelto!.IdEmpresa);
        Assert.Equal(idSede, resuelto.IdSede);
        Assert.Equal(idUsuario, resuelto.IdUsuarioResponsable);
    }

    [Fact]
    public async Task ResolverAsync_DevuelveNull_SiNoHayMapeoParaEseToken()
    {
        var (contexto, _, _, _) = await SembrarBaseAsync();
        var servicio = new ServicioMapeoReunion(contexto);

        var resuelto = await servicio.ResolverAsync(ProveedorReunion.Zoom, "token-que-no-existe");

        Assert.Null(resuelto);
    }

    [Fact]
    public async Task ResolverAsync_DistingueElMismoTokenEnPlataformasDistintas()
    {
        // El mismo correo puede usarse como token en Zoom y en Webex a la vez —
        // no deben mezclarse: son mapeos independientes (Origen, TokenCorrelacion).
        var (contexto, idEmpresa, idSede, idUsuario) = await SembrarBaseAsync();
        var servicio = new ServicioMapeoReunion(contexto);
        const string correo = "miguel.torres@constructoraandina.demo";

        await servicio.CrearAsync(new CrearMapeoOrigenReunionDto("Zoom", correo, idEmpresa, idSede, idUsuario, null));

        var enZoom = await servicio.ResolverAsync(ProveedorReunion.Zoom, correo);
        var enWebex = await servicio.ResolverAsync(ProveedorReunion.Webex, correo);

        Assert.NotNull(enZoom);
        Assert.Null(enWebex);
    }

    [Fact]
    public async Task CrearAsync_RechazaOrigenManual()
    {
        var (contexto, idEmpresa, idSede, idUsuario) = await SembrarBaseAsync();
        var servicio = new ServicioMapeoReunion(contexto);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            servicio.CrearAsync(new CrearMapeoOrigenReunionDto("Manual", "cualquier-token", idEmpresa, idSede, idUsuario, null)));
    }
}
