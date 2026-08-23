-- =========================================================================
-- seed-data.sql — Datos semilla para Control Documental SST (PostgreSQL)
-- Esquema en español (coincide con las entidades de SstControl.Dominio).
-- Ejecutar DESPUÉS de aplicar las migraciones de EF Core.
--   psql -h localhost -U sst_user -d sst_control -f seed-data.sql
-- Las contraseñas de los usuarios de ejemplo están hasheadas con BCrypt
-- (equivalentes a "sst2026" y "campo2026") — son datos de DEMOSTRACIÓN;
-- cámbialas o elimina esos usuarios antes de un uso real con clientes.
-- =========================================================================

BEGIN;

-- ---------- Roles ----------
INSERT INTO "Roles" ("IdRol", "Nombre") VALUES
  (1, 'admin'),
  (2, 'colaborador')
ON CONFLICT ("IdRol") DO NOTHING;

-- ---------- Usuarios de demostración ----------
-- Hash BCrypt de ejemplo (factor 11) — genera los tuyos con BCrypt.Net antes de producción.
INSERT INTO "Usuarios" ("IdUsuario", "NombreUsuario", "ClaveHash", "NombreCompleto", "IdRol", "FechaCreacion") VALUES
  (1, 'lsastoque', '$2a$11$8K1p/a0dURXAmZmFPMuGWO8t8dO6cYGqYb7Qw1nO2gk5c2ZbFhq0S', 'Luz Ángela Sastoque', 1, now()),
  (2, 'colaborador', '$2a$11$1c2yQd6M8gk4W8v0bqk8QeQhP8G0m8b8m1QeYb9nGZC5rG2f8m3Ha', 'Colaborador de Campo', 2, now())
ON CONFLICT ("IdUsuario") DO NOTHING;

-- ---------- Empresas clientes y sedes ----------
INSERT INTO "Empresas" ("IdEmpresa", "Nombre", "FechaCreacion") VALUES
  (1, 'Constructora Andina S.A.S.', now()),
  (2, 'Alimentos del Valle Ltda.', now())
ON CONFLICT ("IdEmpresa") DO NOTHING;

INSERT INTO "Sedes" ("IdSede", "IdEmpresa", "Nombre") VALUES
  (1, 1, 'Obra Torre Norte — Bogotá'),
  (2, 1, 'Bodega Industrial — Medellín'),
  (3, 2, 'Planta de Producción'),
  (4, 2, 'Centro de Distribución')
ON CONFLICT ("IdSede") DO NOTHING;

-- ---------- Tipos de documento ----------
INSERT INTO "TiposDocumento" ("IdTipoDocumento", "Nombre") VALUES
  (1, 'Inspección de EPP'),
  (2, 'Permiso de trabajo en alturas'),
  (3, 'Examen médico ocupacional'),
  (4, 'Capacitación / Inducción'),
  (5, 'Otro')
ON CONFLICT ("IdTipoDocumento") DO NOTHING;

-- ---------- Insignias ----------
INSERT INTO "Insignias" ("IdInsignia", "Codigo", "Etiqueta", "Icono") VALUES
  (1, 'b_first',     'Primer curso',            '🥉'),
  (2, 'b_half',      '3 cursos aprobados',      '🥈'),
  (3, 'b_all',       'Experto SST',             '🥇'),
  (4, 'b_streak',    'Racha de 5',              '⚡'),
  (5, 'b_perfect',   'Puntaje perfecto',        '🎯'),
  (6, 'b_frequent',  'Jugador frecuente',       '🏆'),
  (7, 'b_top1',      'N.º 1 del ranking',       '👑')
ON CONFLICT ("IdInsignia") DO NOTHING;

-- ---------- Checklist de auditoría interna ----------
INSERT INTO "ItemsChecklist" ("IdItemChecklist", "Etiqueta") VALUES
  (1, 'Todas las sedes registradas cuentan con al menos una visita en los últimos 90 días.'),
  (2, 'Los documentos pendientes de firma se aprueban en menos de 5 días desde su captura.'),
  (3, 'Cada empresa cliente tiene al menos una capacitación registrada.'),
  (4, 'No existen documentos vencidos hace más de 30 días sin renovar.'),
  (5, 'El personal de campo tiene acceso y ha usado el módulo de capacitación (Aprende).'),
  (6, 'Existen actas de reunión periódicas (ej. COPASST) por cada empresa cliente.'),
  (7, 'Cada sede tiene un responsable claramente identificado en las actas registradas.')
ON CONFLICT ("IdItemChecklist") DO NOTHING;

-- ---------- Cursos, lecciones y examen ----------
INSERT INTO "Cursos" ("IdCurso", "Titulo", "Icono", "Resumen") VALUES
  (1, 'Uso correcto de EPP', '🦺', 'Elementos de protección personal: para qué sirven y cómo cuidarlos.'),
  (2, 'Trabajo seguro en alturas', '🪜', 'Prevención de caídas: arnés, anclaje y permisos de trabajo.'),
  (3, 'Prevención de riesgos eléctricos', '⚡', 'Identificación de riesgos y bloqueo/etiquetado de energía.'),
  (4, 'Manejo seguro de sustancias químicas', '🧪', 'Hojas de seguridad, almacenamiento y EPP específico.'),
  (5, 'Plan de emergencias y evacuación', '🚨', 'Rutas de evacuación, punto de encuentro y roles del personal.')
ON CONFLICT ("IdCurso") DO NOTHING;

INSERT INTO "Lecciones" ("IdLeccion", "IdCurso", "Titulo", "ContenidoHtml", "Orden") VALUES
  (1, 1, '¿Qué es el EPP?', '<p>El EPP reduce la exposición a riesgos; no la elimina. Debe usarse junto con otras medidas de control.</p>', 1),
  (2, 1, 'Elige el EPP según el riesgo', '<p>Casco, gafas, guantes y botas según el tipo de riesgo de la actividad.</p>', 2),
  (3, 1, 'Cuidado y mantenimiento', '<p>Revisa el EPP antes de cada uso y repórtalo si está dañado o vencido.</p>', 3),

  (4, 2, '¿Qué se considera trabajo en alturas?', '<p>Toda actividad a 1.5 metros o más sobre un nivel inferior.</p>', 1),
  (5, 2, 'Arnés y puntos de anclaje', '<p>Arnés certificado, anclaje que soporte la carga y esté por encima del trabajador.</p>', 2),
  (6, 2, 'Permiso de trabajo en alturas', '<p>Debe existir un permiso firmado antes de subir.</p>', 3),

  (7, 3, 'Identifica el riesgo eléctrico', '<p>Tableros, cableado expuesto y equipos energizados requieren personal autorizado.</p>', 1),
  (8, 3, 'Bloqueo y etiquetado (LOTO)', '<p>Desenergizar, bloquear y etiquetar antes de intervenir un equipo.</p>', 2),
  (9, 3, 'Ante una emergencia eléctrica', '<p>Cortar la energía de forma segura y pedir ayuda médica de inmediato.</p>', 3),

  (10, 4, 'Hoja de seguridad (SDS)', '<p>Toda sustancia debe contar con su SDS: riesgos, manejo y primeros auxilios.</p>', 1),
  (11, 4, 'Almacenamiento correcto', '<p>Según compatibilidad, ventilado, rotulado, lejos de fuentes de calor.</p>', 2),
  (12, 4, 'EPP para químicos', '<p>Guantes, gafas y protección respiratoria según lo indique la SDS.</p>', 3),

  (13, 5, 'Conoce tu ruta de evacuación', '<p>Identifícala al llegar a una sede nueva, sin esperar la emergencia.</p>', 1),
  (14, 5, 'Punto de encuentro', '<p>Diríjete con calma; no te devuelvas por objetos personales.</p>', 2),
  (15, 5, 'Roles durante la emergencia', '<p>Sigue siempre las instrucciones de los brigadistas.</p>', 3)
ON CONFLICT ("IdLeccion") DO NOTHING;

-- Preguntas y opciones (una EsCorrecta=true por pregunta)
INSERT INTO "PreguntasQuiz" ("IdPregunta", "IdCurso", "Texto") VALUES
  (1, 1, '¿Qué hace el EPP frente a un riesgo?'),
  (2, 1, 'Si encuentras tu casco agrietado, ¿qué debes hacer?'),
  (3, 1, '¿Cuándo debes revisar tu EPP?'),
  (4, 2, '¿Desde qué altura se considera trabajo en alturas?'),
  (5, 2, '¿Dónde debe ubicarse el punto de anclaje?'),
  (6, 2, '¿Qué debe existir antes de subir a trabajar en alturas?'),
  (7, 3, '¿Qué significa LOTO?'),
  (8, 3, 'Ante un incidente eléctrico, primero debes:'),
  (9, 3, '¿Quién puede intervenir un equipo energizado?'),
  (10, 4, '¿Qué información encuentras en la SDS?'),
  (11, 4, '¿Cómo deben almacenarse las sustancias químicas?'),
  (12, 4, 'Antes de manipular un químico nuevo, debes:'),
  (13, 5, '¿Cuándo debes identificar la ruta de evacuación?'),
  (14, 5, 'Ante una alarma, ¿qué debes hacer?'),
  (15, 5, '¿A quién debes seguir durante una evacuación?')
ON CONFLICT ("IdPregunta") DO NOTHING;

INSERT INTO "OpcionesQuiz" ("IdOpcion", "IdPregunta", "Texto", "EsCorrecta") VALUES
  (1,1,'Lo elimina por completo', false), (2,1,'Reduce la exposición al riesgo', true),
  (3,1,'No tiene ningún efecto', false), (4,1,'Solo es obligatorio en oficinas', false),
  (5,2,'Usarlo igual, no pasa nada', false), (6,2,'Reportarlo y solicitar reemplazo', true),
  (7,2,'Pintarlo para disimularlo', false), (8,2,'Guardarlo para otra ocasión', false),
  (9,3,'Solo una vez al año', false), (10,3,'Nunca, viene garantizado', false),
  (11,3,'Antes de cada uso', true), (12,3,'Solo si se ve sucio', false),
  (13,4,'3 metros', false), (14,4,'1.5 metros', true), (15,4,'10 metros', false), (16,4,'Solo en techos', false),
  (17,5,'En cualquier tubería cercana', false), (18,5,'Por encima del trabajador, certificado', true),
  (19,5,'No es necesario si hay experiencia', false), (20,5,'A nivel del suelo', false),
  (21,6,'Nada, basta con el arnés', false), (22,6,'Un permiso de trabajo verificado', true),
  (23,6,'La autorización verbal de un compañero', false), (24,6,'Un seguro médico', false),
  (25,7,'Un tipo de herramienta', false), (26,7,'Bloqueo y etiquetado antes de intervenir un equipo', true),
  (27,7,'Un permiso de alturas', false), (28,7,'Un examen médico', false),
  (29,8,'Tocar a la persona para auxiliarla', false), (30,8,'Cortar la energía de forma segura y pedir ayuda', true),
  (31,8,'Esperar a que se recupere sola', false), (32,8,'Ignorarlo si parece leve', false),
  (33,9,'Cualquier trabajador', false), (34,9,'Solo personal autorizado y capacitado', true),
  (35,9,'El primero que llegue', false), (36,9,'Nadie, nunca', false),
  (37,10,'Solo el precio del producto', false), (38,10,'Riesgos, manejo y primeros auxilios', true),
  (39,10,'El proveedor únicamente', false), (40,10,'Nada relevante para seguridad', false),
  (41,11,'Todas juntas sin distinción', false), (42,11,'Según su compatibilidad y bien rotuladas', true),
  (43,11,'En cualquier lugar cerrado', false), (44,11,'No requieren rótulo si se conocen', false),
  (45,12,'Olerlo para identificarlo', false), (46,12,'Consultar su hoja de seguridad', true),
  (47,12,'Usarlo igual que otro similar', false), (48,12,'Preguntar a un compañero cualquiera', false),
  (49,13,'Solo durante un simulacro', false), (50,13,'Al llegar a un sitio nuevo', true),
  (51,13,'Nunca es necesario', false), (52,13,'Solo si lo pide el jefe', false),
  (53,14,'Buscar tus pertenencias primero', false), (54,14,'Ir con calma al punto de encuentro', true),
  (55,14,'Quedarte donde estás', false), (56,14,'Salir corriendo por cualquier lado', false),
  (57,15,'A cualquier persona que corra', false), (58,15,'A los brigadistas asignados', true),
  (59,15,'A nadie, cada uno decide', false), (60,15,'Al primero en salir', false)
ON CONFLICT ("IdOpcion") DO NOTHING;

-- ---------- Sincroniza las secuencias tras insertar IDs explícitos ----------
SELECT setval(pg_get_serial_sequence('"Roles"', 'IdRol'), (SELECT MAX("IdRol") FROM "Roles"));
SELECT setval(pg_get_serial_sequence('"Usuarios"', 'IdUsuario'), (SELECT MAX("IdUsuario") FROM "Usuarios"));
SELECT setval(pg_get_serial_sequence('"Empresas"', 'IdEmpresa'), (SELECT MAX("IdEmpresa") FROM "Empresas"));
SELECT setval(pg_get_serial_sequence('"Sedes"', 'IdSede'), (SELECT MAX("IdSede") FROM "Sedes"));
SELECT setval(pg_get_serial_sequence('"TiposDocumento"', 'IdTipoDocumento'), (SELECT MAX("IdTipoDocumento") FROM "TiposDocumento"));
SELECT setval(pg_get_serial_sequence('"Insignias"', 'IdInsignia'), (SELECT MAX("IdInsignia") FROM "Insignias"));
SELECT setval(pg_get_serial_sequence('"ItemsChecklist"', 'IdItemChecklist'), (SELECT MAX("IdItemChecklist") FROM "ItemsChecklist"));
SELECT setval(pg_get_serial_sequence('"Cursos"', 'IdCurso'), (SELECT MAX("IdCurso") FROM "Cursos"));
SELECT setval(pg_get_serial_sequence('"Lecciones"', 'IdLeccion'), (SELECT MAX("IdLeccion") FROM "Lecciones"));
SELECT setval(pg_get_serial_sequence('"PreguntasQuiz"', 'IdPregunta'), (SELECT MAX("IdPregunta") FROM "PreguntasQuiz"));
SELECT setval(pg_get_serial_sequence('"OpcionesQuiz"', 'IdOpcion'), (SELECT MAX("IdOpcion") FROM "OpcionesQuiz"));

COMMIT;

-- Verificación rápida:
-- SELECT (SELECT count(*) FROM "Empresas") AS empresas, (SELECT count(*) FROM "Sedes") AS sedes,
--        (SELECT count(*) FROM "Cursos") AS cursos, (SELECT count(*) FROM "PreguntasQuiz") AS preguntas;
