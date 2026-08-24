using Microsoft.AspNetCore.Authorization;

namespace SstControl.Api.Seguridad;

/// <summary>Requisito de autorización: el usuario debe tener el permiso indicado
/// (código exacto, ej. "empresas.gestionar") entre sus claims de tipo "permiso".</summary>
public class RequisitoPermiso : IAuthorizationRequirement
{
    public string Codigo { get; }
    public RequisitoPermiso(string codigo) => Codigo = codigo;
}

/// <summary>Evalúa el requisito: consulta el JWT del usuario (ya validado) y confirma
/// si trae el claim de permiso correspondiente — sin volver a consultar la base de datos.</summary>
public class ManejadorPermiso : AuthorizationHandler<RequisitoPermiso>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext contexto, RequisitoPermiso requisito)
    {
        if (contexto.User.TienePermiso(requisito.Codigo))
            contexto.Succeed(requisito);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Genera políticas de autorización "sobre la marcha" a partir del nombre usado en
/// [Authorize(Policy = "modulo.accion")] — así NO hace falta registrar manualmente
/// cada permiso del catálogo en Program.cs; cualquier código de permiso nuevo que
/// se agregue a la tabla Permiso funciona automáticamente como política.
/// </summary>
public class ProveedorPoliticasPermiso : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider ProveedorPorDefecto { get; }
    public ProveedorPoliticasPermiso(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> opciones)
        => ProveedorPorDefecto = new DefaultAuthorizationPolicyProvider(opciones);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => ProveedorPorDefecto.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => ProveedorPorDefecto.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string nombrePolitica)
    {
        // Cualquier nombre de política con este formato ("modulo.accion") se trata
        // como un código de permiso y se construye dinámicamente.
        var politica = new AuthorizationPolicyBuilder()
            .AddRequirements(new RequisitoPermiso(nombrePolitica))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(politica);
    }
}
