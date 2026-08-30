-- =============================================================================
-- 03_usuarios_y_asignacion_roles.sql
-- Siembra 15 usuarios — 3 Administradores SST, 10 Asesores SST (2 por cada
-- una de las 5 empresas) y 2 Auditores SST — con su rol asignado y, los
-- Asesores, agrupados organizativamente a la empresa donde trabajan.
-- Requiere haber corrido antes 01_catalogos_rbac.sql y 02_empresas_y_sedes.sql.
-- Ver docs/datos-semilla/LEEME.md para las credenciales de práctica.
-- Idempotente en Usuarios/UsuarioRoles/UsuarioGrupos (ON CONFLICT DO NOTHING
-- sobre sus restricciones únicas). Grupos no tiene índice único a nivel de
-- base de datos, así que se protege con WHERE NOT EXISTS en su lugar.
-- =============================================================================

BEGIN;

-- Hash BCrypt real de la contraseña de práctica "Practica#2026" (verificado
-- con BCrypt.Net-Next/passlib antes de publicar este script — no es un
-- marcador de posición, funciona para iniciar sesión tal cual).
-- Cámbiala antes de reutilizar este script fuera de un ambiente de práctica.

-- ---- Administradores SST (3) — no atados a una empresa específica ----
INSERT INTO "Usuarios" ("NombreUsuario", "ClaveHash", "NombreCompleto") VALUES
    ('andrea.martinez',  '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Andrea Martínez Rojas'),
    ('carlos.rivera',    '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Carlos Rivera Gómez'),
    ('lucia.fernandez',  '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Lucía Fernández Ortiz')
ON CONFLICT ("NombreUsuario") DO NOTHING;

-- ---- Asesores SST (10) — dos por cada una de las 5 empresas ----
INSERT INTO "Usuarios" ("NombreUsuario", "ClaveHash", "NombreCompleto") VALUES
    ('miguel.torres',    '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Miguel Torres Salazar'),
    ('paula.gonzalez',   '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Paula González Herrera'),
    ('javier.ramirez',   '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Javier Ramírez Castro'),
    ('daniela.morales',  '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Daniela Morales Vargas'),
    ('esteban.pena',     '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Esteban Peña Molina'),
    ('valentina.suarez', '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Valentina Suárez Prieto'),
    ('felipe.castano',   '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Felipe Castaño Duarte'),
    ('camila.jimenez',   '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Camila Jiménez Rueda'),
    ('santiago.lopez',   '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Santiago López Cárdenas'),
    ('mariana.diaz',     '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Mariana Díaz Bernal')
ON CONFLICT ("NombreUsuario") DO NOTHING;

-- ---- Auditores SST (2) — revisión transversal, no atados a una empresa ----
INSERT INTO "Usuarios" ("NombreUsuario", "ClaveHash", "NombreCompleto") VALUES
    ('ricardo.pardo',    '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Ricardo Pardo Aguilar'),
    ('sofia.mendez',     '$2b$11$EDkM6r27Nv.eu8VM6P0UhuS7tO8Y3xfjnN3dqKb7qTE8bUKQrWMUy', 'Sofía Méndez Cifuentes')
ON CONFLICT ("NombreUsuario") DO NOTHING;

-- ---- Asignación de roles ----
INSERT INTO "UsuarioRoles" ("IdUsuario", "IdRol")
    SELECT u."IdUsuario", r."IdRol" FROM "Usuarios" u, "Roles" r
    WHERE r."Nombre" = 'Administrador SST'
      AND u."NombreUsuario" IN ('andrea.martinez', 'carlos.rivera', 'lucia.fernandez')
ON CONFLICT DO NOTHING;

INSERT INTO "UsuarioRoles" ("IdUsuario", "IdRol")
    SELECT u."IdUsuario", r."IdRol" FROM "Usuarios" u, "Roles" r
    WHERE r."Nombre" = 'Asesor SST'
      AND u."NombreUsuario" IN ('miguel.torres', 'paula.gonzalez', 'javier.ramirez', 'daniela.morales',
                                 'esteban.pena', 'valentina.suarez', 'felipe.castano', 'camila.jimenez',
                                 'santiago.lopez', 'mariana.diaz')
ON CONFLICT DO NOTHING;

INSERT INTO "UsuarioRoles" ("IdUsuario", "IdRol")
    SELECT u."IdUsuario", r."IdRol" FROM "Usuarios" u, "Roles" r
    WHERE r."Nombre" = 'Auditor SST'
      AND u."NombreUsuario" IN ('ricardo.pardo', 'sofia.mendez')
ON CONFLICT DO NOTHING;

-- ---- Grupos organizativos: un equipo por empresa, con sus 2 asesores ----
INSERT INTO "Grupos" ("Nombre", "IdEmpresa")
    SELECT 'Equipo ' || e."Nombre", e."IdEmpresa" FROM "Empresas" e
    WHERE NOT EXISTS (
        SELECT 1 FROM "Grupos" g WHERE g."IdEmpresa" = e."IdEmpresa" AND g."Nombre" = 'Equipo ' || e."Nombre"
    );

INSERT INTO "UsuarioGrupos" ("IdUsuario", "IdGrupo")
    SELECT u."IdUsuario", g."IdGrupo"
    FROM "Usuarios" u, "Grupos" g, "Empresas" e
    WHERE g."IdEmpresa" = e."IdEmpresa" AND g."Nombre" = 'Equipo ' || e."Nombre"
      AND (
        (e."Nombre" = 'Constructora Andina S.A.S.'    AND u."NombreUsuario" IN ('miguel.torres', 'paula.gonzalez')) OR
        (e."Nombre" = 'Minera Altiplano Ltda.'         AND u."NombreUsuario" IN ('javier.ramirez', 'daniela.morales')) OR
        (e."Nombre" = 'Agroindustrias del Valle S.A.'  AND u."NombreUsuario" IN ('esteban.pena', 'valentina.suarez')) OR
        (e."Nombre" = 'Textiles Manizales S.A.S.'      AND u."NombreUsuario" IN ('felipe.castano', 'camila.jimenez')) OR
        (e."Nombre" = 'Logística Caribe S.A.S.'        AND u."NombreUsuario" IN ('santiago.lopez', 'mariana.diaz'))
      )
ON CONFLICT DO NOTHING;

COMMIT;

-- Referencia rápida — quién es quién:
--
-- Administradores SST : andrea.martinez · carlos.rivera · lucia.fernandez
-- Auditores SST       : ricardo.pardo · sofia.mendez
-- Asesores SST        : miguel.torres + paula.gonzalez     → Constructora Andina S.A.S.
--                        javier.ramirez + daniela.morales   → Minera Altiplano Ltda.
--                        esteban.pena + valentina.suarez    → Agroindustrias del Valle S.A.
--                        felipe.castano + camila.jimenez    → Textiles Manizales S.A.S.
--                        santiago.lopez + mariana.diaz      → Logística Caribe S.A.S.
-- Contraseña (todos)  : Practica#2026
