-- =============================================================================
-- 05_mapeos_reunion.sql
-- Configura la correlación webhook -> empresa/sede para las cuentas de Teams,
-- Google Meet, Zoom y Webex de cada empresa del escenario — sin esto, un
-- webhook de "reunión terminada" no sabría a qué cliente pertenece. Ver
-- docs/manuales-tecnicos/backend/04-seguridad-rbac-e-integraciones.md.
--
-- Requiere haber corrido antes 01, 02 y 03. Idempotente (ON CONFLICT DO
-- NOTHING sobre el índice único (Origen, TokenCorrelacion)).
--
-- El token de correlación de cada plataforma es un valor que TÚ eliges al
-- configurar la integración (el "clientState" de la suscripción de Microsoft
-- Graph, o el "X-Goog-Channel-Token" del canal de Google Calendar) — o, para
-- Zoom/Webex (que no ofrecen ese campo libre), el correo del anfitrión que la
-- plataforma sí envía en cada evento. Los valores de abajo son ilustrativos.
-- =============================================================================

BEGIN;

WITH datos_mapeo(origen, token, empresa, sede, responsable, descripcion) AS (
    VALUES
    -- Constructora Andina S.A.S. — Miguel Torres organiza por Teams
    ('Teams', 'andina-torre-norte-2026', 'Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'miguel.torres',
     'Suscripción Graph del calendario de Miguel Torres (clientState elegido al crearla)'),
    -- Minera Altiplano Ltda. — Javier Ramírez organiza por Zoom y Google Meet
    ('Zoom', 'javier.ramirez@mineraaltiplano.demo', 'Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'javier.ramirez',
     'Correo de anfitrión Zoom de Javier Ramírez'),
    ('GoogleMeet', 'altiplano-planta-beneficio-2026', 'Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'javier.ramirez',
     'Canal de Calendar de la planta de beneficio (X-Goog-Channel-Token elegido al crearlo)'),
    -- Agroindustrias del Valle S.A. — Esteban Peña organiza por Webex y Teams
    ('Webex', 'esteban.pena@agroindustriasvalle.demo', 'Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'esteban.pena',
     'Correo de anfitrión Webex de Esteban Peña'),
    ('Teams', 'agrovalle-palmira-2026', 'Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'esteban.pena',
     'Suscripción Graph del calendario de Esteban Peña'),
    -- Textiles Manizales S.A.S. — Felipe Castaño organiza por Zoom y Google Meet
    ('Zoom', 'felipe.castano@textilesmanizales.demo', 'Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'felipe.castano',
     'Correo de anfitrión Zoom de Felipe Castaño'),
    ('GoogleMeet', 'textiles-manizales-planta-2026', 'Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'felipe.castano',
     'Canal de Calendar de la planta textil'),
    -- Logística Caribe S.A.S. — Santiago López organiza por Webex
    ('Webex', 'santiago.lopez@logisticacaribe.demo', 'Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'santiago.lopez',
     'Correo de anfitrión Webex de Santiago López')
)
INSERT INTO "MapeosOrigenReunion" ("Origen", "TokenCorrelacion", "IdEmpresa", "IdSede", "IdUsuarioResponsable", "Descripcion")
SELECT dm.origen, dm.token, e."IdEmpresa", s."IdSede", u."IdUsuario", dm.descripcion
FROM datos_mapeo dm
JOIN "Empresas" e ON e."Nombre" = dm.empresa
JOIN "Sedes" s ON s."IdEmpresa" = e."IdEmpresa" AND s."Nombre" = dm.sede
JOIN "Usuarios" u ON u."NombreUsuario" = dm.responsable
ON CONFLICT ("Origen", "TokenCorrelacion") DO NOTHING;

COMMIT;
