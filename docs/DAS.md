# Documento de Arquitectura de Software (DAS)
**Proyecto:** SSTControl Backend  
**Framework:** .NET / C#  
**Patrón Arquitectónico:** Clean Architecture  

---

## 1. Visión General de la Arquitectura
El sistema adopta una arquitectura desacoplada por capas:

- **`SstControl.Api`**: Capa de presentación (Controllers REST, Middlewares, Filtros de Autorización).
- **`SstControl.Application`**: Casos de uso, Servicios de aplicación, DTOs e Interfaces de servicios.
- **`SstControl.Domain`**: Entidades del negocio, Agregados y Lógica de dominio pura.
- **`SstControl.Infrastructure`**: Persistencia de datos (EF Core, ContextoBaseDatos), Repositorios e Implementación de Servicios.
- **`SstControl.Integrations`**: Conectores externos para reuniones (Google Meet, Teams, Zoom).

---

## 2. Modelo de Control de Acceso (RBAC)

El modelo de seguridad se fundamenta en un esquema RBAC granular con identificadores de tipo **UUID (`VARCHAR(36)`)**:

- **Módulos (`modules`)**: Agrupan funcionalmente el sistema.
- **Permisos (`permissions`)**: Privilegios específicos atómicos vinculados a un código unívoco (ej. `USER_READ`, `INCIDENT_CREATE`).
- **Roles (`roles`)**: Perfiles de usuario que integran múltiples permisos.
- **Asignaciones (`role_permissions`, `user_roles`)**: Tablas intermedias de relación N:M con trazabilidad temporal (`assigned_at`).

```plantuml
@startuml
entity "modules" {
    * id : VARCHAR(36) [PK]
    --
    * name : VARCHAR(100) [UQ]
    description : VARCHAR(255)
    * is_active : BOOLEAN
    * created_at : TIMESTAMP
}

entity "permissions" {
    * id : VARCHAR(36) [PK]
    --
    module_id : VARCHAR(36) [FK]
    * name : VARCHAR(100)
    * code : VARCHAR(100) [UQ]
    description : VARCHAR(255)
    * is_active : BOOLEAN
    * created_at : TIMESTAMP
}

entity "roles" {
    * id : VARCHAR(36) [PK]
    --
    * name : VARCHAR(50) [UQ]
    description : VARCHAR(255)
    * is_active : BOOLEAN
    * created_at : TIMESTAMP
    * updated_at : TIMESTAMP
}

entity "role_permissions" {
    * role_id : VARCHAR(36) [PK, FK]
    * permission_id : VARCHAR(36) [PK, FK]
    --
    * assigned_at : TIMESTAMP
}

entity "user_roles" {
    * user_id : VARCHAR(36) [PK, FK]
    * role_id : VARCHAR(36) [PK, FK]
    --
    * assigned_at : TIMESTAMP
}

modules ||--o{ permissions
roles ||--|{ role_permissions
permissions ||--|{ role_permissions
roles ||--|{ user_roles
@enduml