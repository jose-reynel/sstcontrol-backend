using SstControl.Aplicacion.Integraciones;

namespace SstControl.Integraciones.Conectores;

/// <summary>Resuelve, entre todos los conectores registrados, el que corresponde
/// a la plataforma solicitada (Teams, Google Meet o Zoom).</summary>
public class FabricaConectoresReunion : IFabricaConectoresReunion
{
    private readonly IEnumerable<IConectorReunion> _conectores;
    public FabricaConectoresReunion(IEnumerable<IConectorReunion> conectores) => _conectores = conectores;

    public IConectorReunion Resolver(ProveedorReunion proveedor) =>
        _conectores.FirstOrDefault(c => c.Proveedor == proveedor)
        ?? throw new NotSupportedException($"No hay conector registrado para {proveedor}.");
}
