-- =============================================================================
-- 02_empresas_y_sedes.sql
-- Siembra las 5 empresas cliente del escenario de práctica y sus sedes.
-- Requiere haber corrido antes 01_catalogos_rbac.sql (no depende de su
-- contenido directamente, pero respeta el orden documentado en LEEME.md).
-- =============================================================================

BEGIN;

INSERT INTO "Empresas" ("Nombre") VALUES
    ('Constructora Andina S.A.S.'),
    ('Minera Altiplano Ltda.'),
    ('Agroindustrias del Valle S.A.'),
    ('Textiles Manizales S.A.S.'),
    ('Logística Caribe S.A.S.');

INSERT INTO "Sedes" ("IdEmpresa", "Nombre")
    SELECT "IdEmpresa", s.nombre
    FROM "Empresas" e
    CROSS JOIN LATERAL (VALUES
        ('Obra Torre Norte — Bogotá'),
        ('Obra Vial Cundinamarca')
    ) AS s(nombre)
    WHERE e."Nombre" = 'Constructora Andina S.A.S.';

INSERT INTO "Sedes" ("IdEmpresa", "Nombre")
    SELECT "IdEmpresa", s.nombre
    FROM "Empresas" e
    CROSS JOIN LATERAL (VALUES
        ('Planta de Beneficio — Pasto'),
        ('Campamento Alto Putumayo')
    ) AS s(nombre)
    WHERE e."Nombre" = 'Minera Altiplano Ltda.';

INSERT INTO "Sedes" ("IdEmpresa", "Nombre")
    SELECT "IdEmpresa", s.nombre
    FROM "Empresas" e
    CROSS JOIN LATERAL (VALUES
        ('Planta de Procesamiento — Palmira'),
        ('Bodega de Insumos — Buga')
    ) AS s(nombre)
    WHERE e."Nombre" = 'Agroindustrias del Valle S.A.';

INSERT INTO "Sedes" ("IdEmpresa", "Nombre")
    SELECT "IdEmpresa", 'Planta Textil — Manizales'
    FROM "Empresas" e WHERE e."Nombre" = 'Textiles Manizales S.A.S.';

INSERT INTO "Sedes" ("IdEmpresa", "Nombre")
    SELECT "IdEmpresa", s.nombre
    FROM "Empresas" e
    CROSS JOIN LATERAL (VALUES
        ('Bodega Portuaria — Cartagena'),
        ('Centro de Distribución — Barranquilla')
    ) AS s(nombre)
    WHERE e."Nombre" = 'Logística Caribe S.A.S.';

COMMIT;
