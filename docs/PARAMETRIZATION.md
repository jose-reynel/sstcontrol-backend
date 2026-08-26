# Manual de Parametrización y Administración
**Proyecto:** SSTControl Backend  
**Base de Datos:** MySQL / MariaDB  

---

## 1. Orden de Despliegue de Base de Datos

Para inicializar la base de datos de manera limpia y consistente, debe respetarse el orden de ejecución de los archivos SQL de la raíz:

1. **`ddl.sql`**: Define las tablas del sistema y la estructura RBAC.
2. **`dml.sql`**: Siembras de datos de módulos, permisos y roles base en formato UUID.

---

## 2. Matriz de Parametrización Inicial

### Módulos Registrados (`modules`)
- `GESTION_USUARIOS`: Administración de usuarios y accesos al sistema.
- `INCIDENTES_ACCIDENTES`: Registro, clasificación y reporte de incidentes SST.
- `CAPACITACIONES`: Formaciones, cronogramas y registro de asistencias.

### Permisos Iniciales (`permissions`)
| Código (`code`) | Permiso | Módulo Asociado | Descripción |
| :--- | :--- | :--- | :--- |
| `USER_READ` | Ver Usuarios | `GESTION_USUARIOS` | Consultar la lista de usuarios |
| `USER_WRITE` | Crear/Editar Usuarios | `GESTION_USUARIOS` | Crear o modificar datos de usuarios |
| `INCIDENT_READ` | Ver Incidentes | `INCIDENTES_ACCIDENTES` | Consultar reportes de incidentes |
| `INCIDENT_CREATE` | Registrar Incidente | `INCIDENTES_ACCIDENTES` | Reportar un nuevo incidente SST |
| `TRAINING_READ` | Ver Capacitaciones | `CAPACITACIONES` | Consultar programa de capacitaciones |

### Roles Predeterminados (`roles`)
- **`ROLE_ADMIN`**: Posee la asignación total de los permisos del sistema.
- **`ROLE_COORDINADOR_SST`**: Asignado con permisos de consulta y edición en el área operativa de SST.
- **`ROLE_EMPLEADO`**: Asignado con permisos de reporte e interacción básica.

---

## 3. Comandos de Sincronización

```bash
git add docs/PARAMETRIZATION.md
git commit -m "docs: agregar manual de parametrizacion en /docs"
git push origin main