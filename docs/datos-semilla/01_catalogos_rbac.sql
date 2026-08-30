-- =============================================================================
-- 01_catalogos_rbac.sql
-- Siembra el catálogo de permisos/perfiles/roles del control de acceso, y el
-- catálogo de tipos de documento. Ejecutar primero — todo lo demás depende
-- de que estos catálogos ya existan. Ver docs/datos-semilla/LEEME.md.
-- =============================================================================

-- Idempotente en Permisos/Perfiles/Roles/RolPerfiles/PerfilPermisos (ON CONFLICT
-- DO NOTHING sobre sus índices únicos) — correr este script dos veces no duplica
-- catálogos. TiposDocumento no tiene índice único a nivel de base de datos, así
-- que se protege con WHERE NOT EXISTS en su lugar.

BEGIN;

-- ---- Permisos: exactamente los códigos que el código de la API verifica ----
-- (ver docs/manuales-tecnicos/backend/04-seguridad-rbac-e-integraciones.md)
INSERT INTO "Permisos" ("Codigo", "Descripcion", "Modulo") VALUES
    ('accesos.administrar',    'Administrar usuarios, roles, perfiles, permisos y grupos', 'Administración'),
    ('empresas.gestionar',     'Crear empresas clientes y sus sedes',                        'Empresas'),
    ('documentos.firmar',      'Aprobar (firmar) un documento pendiente',                    'Documentos'),
    ('documentos.eliminar',    'Eliminar un documento registrado',                           'Documentos'),
    ('documentos.escanear',    'Adjuntar y digitalizar (OCR) evidencia física de un documento','Documentos'),
    ('actas.ver',              'Consultar actas y sus compromisos',                          'Actas'),
    ('actas.crear',            'Registrar actas, generar minutas y gestionar compromisos',   'Actas'),
    ('reuniones.sincronizar',  'Sincronizar reuniones desde Teams/Meet/Zoom/Webex',          'Integraciones')
ON CONFLICT ("Codigo") DO NOTHING;

-- ---- Perfiles: paquetes reutilizables de permisos ----
INSERT INTO "Perfiles" ("Nombre", "Descripcion") VALUES
    ('Administración total',            'Todos los permisos del sistema — plataforma y operación SST completas.'),
    ('Gestión documental y de actas',   'Operación diaria de campo: documentos, actas, bot de minutas e integraciones de reunión.'),
    ('Consulta y auditoría',            'Solo lectura sobre actas y compromisos — sin capacidad de crear, modificar ni administrar.')
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO "PerfilPermisos" ("IdPerfil", "IdPermiso")
    SELECT pf."IdPerfil", pm."IdPermiso"
    FROM "Perfiles" pf CROSS JOIN "Permisos" pm
    WHERE pf."Nombre" = 'Administración total'  -- todos los permisos, sin excepción
ON CONFLICT DO NOTHING;

INSERT INTO "PerfilPermisos" ("IdPerfil", "IdPermiso")
    SELECT pf."IdPerfil", pm."IdPermiso"
    FROM "Perfiles" pf CROSS JOIN "Permisos" pm
    WHERE pf."Nombre" = 'Gestión documental y de actas'
      AND pm."Codigo" IN ('actas.crear', 'actas.ver', 'documentos.firmar', 'documentos.escanear', 'reuniones.sincronizar')
ON CONFLICT DO NOTHING;

INSERT INTO "PerfilPermisos" ("IdPerfil", "IdPermiso")
    SELECT pf."IdPerfil", pm."IdPermiso"
    FROM "Perfiles" pf CROSS JOIN "Permisos" pm
    WHERE pf."Nombre" = 'Consulta y auditoría'
      AND pm."Codigo" IN ('actas.ver')
ON CONFLICT DO NOTHING;
      -- Los documentos ya son de consulta libre para cualquier usuario autenticado
      -- (ver GET /api/documentos) — no necesita un permiso extra para el Auditor SST.

-- ---- Roles de negocio, compuestos de los perfiles anteriores ----
INSERT INTO "Roles" ("Nombre", "Descripcion") VALUES
    ('Administrador SST', 'Gestiona la plataforma y tiene visibilidad/control total sobre la operación SST.'),
    ('Asesor SST',        'Profesional de campo: registra y firma documentos, gestiona actas y compromisos, sincroniza reuniones.'),
    ('Auditor SST',       'Revisa el cumplimiento — rol exclusivamente de consulta, sin capacidad de operar el sistema.')
ON CONFLICT ("Nombre") DO NOTHING;

INSERT INTO "RolPerfiles" ("IdRol", "IdPerfil")
    SELECT r."IdRol", pf."IdPerfil" FROM "Roles" r, "Perfiles" pf
    WHERE r."Nombre" = 'Administrador SST' AND pf."Nombre" = 'Administración total'
ON CONFLICT DO NOTHING;

INSERT INTO "RolPerfiles" ("IdRol", "IdPerfil")
    SELECT r."IdRol", pf."IdPerfil" FROM "Roles" r, "Perfiles" pf
    WHERE r."Nombre" = 'Asesor SST' AND pf."Nombre" = 'Gestión documental y de actas'
ON CONFLICT DO NOTHING;

INSERT INTO "RolPerfiles" ("IdRol", "IdPerfil")
    SELECT r."IdRol", pf."IdPerfil" FROM "Roles" r, "Perfiles" pf
    WHERE r."Nombre" = 'Auditor SST' AND pf."Nombre" = 'Consulta y auditoría'
ON CONFLICT DO NOTHING;

-- ---- Catálogo de tipos de documento (coincide con el usado en el demo AppSST) ----
-- Sin índice único a nivel de BD -> se protege con WHERE NOT EXISTS.
INSERT INTO "TiposDocumento" ("Nombre")
    SELECT nombre FROM (VALUES
        ('Inspección de EPP'),
        ('Permiso de trabajo en alturas'),
        ('Examen médico ocupacional'),
        ('Certificado de capacitación'),
        ('Otro documento SST')
    ) AS v(nombre)
    WHERE NOT EXISTS (SELECT 1 FROM "TiposDocumento" td WHERE td."Nombre" = v.nombre);

COMMIT;
