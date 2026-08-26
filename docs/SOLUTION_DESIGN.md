# Diseño de la Solución (Solution Design Document)
**Proyecto:** SSTControl Backend  
**Modulo:** Control de Acceso (RBAC)  

---

## 1. Estrategia de Identificadores Únicos (UUID)
Para asegurar escalabilidad distribuida, desconfiabilidad en predicción de secuencias y compatibilidad con microservicios o clientes externos, todos los campos clave de las tablas del módulo RBAC utilizan **UUID de 36 caracteres (`VARCHAR(36)`)**.

---

## 2. Mecanismo de Autorización en C# / .NET

### 2.1. Carga de Claims y Permisos
Al autenticar una solicitud vía Token JWT, los roles del usuario se mapean desde `user_roles` y sus privilegios atómicos desde `role_permissions`. Cada permiso es inyectado en el contexto de seguridad (`ClaimsPrincipal`) con el tipo de claim personalizable o como permisos customizados.

### 2.2. Protección de Endpoints
En la capa `SstControl.Api/Controllers`, el control de acceso se realiza declarativamente usando políticas o atributos de permiso:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ControlAccesoControlador : ControllerBase
{
    [HttpGet("usuarios")]
    [AutorizacionPorPermiso("USER_READ")]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        // Lógica de consulta
        return Ok();
    }
}