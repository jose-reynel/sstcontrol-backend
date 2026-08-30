-- =============================================================================
-- 04_transacciones_simuladas.sql
-- Actividad ficticia end-to-end sobre las 5 empresas y los 15 usuarios ya
-- sembrados: documentos (variados estados/vencimientos), actas (manuales y
-- sincronizadas desde Teams/Zoom/Google Meet/Webex), asistentes, contenido
-- de reunión, compromisos (del bot y manuales, algunos vinculados a
-- documentos), digitalizaciones OCR de ejemplo, y bitácora de auditoría.
--
-- Requiere haber corrido antes 01, 02 y 03. Ver docs/datos-semilla/LEEME.md.
-- Las fechas se calculan relativas a CURRENT_DATE, así que el escenario
-- siempre se ve "vigente" sin importar cuándo corras el script — con una
-- mezcla deliberada de documentos vigentes, por vencer y vencidos.
-- =============================================================================

BEGIN;

-- =============================================================================
-- DOCUMENTOS (≈40 — 8 por empresa)
-- dias_captura: hace cuántos días se capturó · dias_vence: en cuántos días
-- vence desde hoy (negativo = ya vencido).
-- =============================================================================
WITH datos_doc(empresa, sede, tipo, colaborador, actividad, dias_captura, dias_vence, estado, aprueba) AS (
    VALUES
    -- Constructora Andina S.A.S.
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Inspección de EPP', 'Jorge Salamanca Ruiz', 'Inspección mensual de EPP en frente de obra', 10, 20, 'Aprobado', 'miguel.torres'),
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Permiso de trabajo en alturas', 'Diana Beltrán Ospina', 'Instalación de fachada — torre norte, piso 14', 25, -3, 'Pendiente', NULL),
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Examen médico ocupacional', 'Hernán Cuervo Pinilla', 'Ingreso de personal nuevo a cuadrilla de acero', 40, 3, 'Aprobado', 'miguel.torres'),
    ('Constructora Andina S.A.S.', 'Obra Vial Cundinamarca', 'Inspección de EPP', 'Rosa Achury Medina', 'Inspección de EPP en cuadrilla de pavimentación', 4, 26, 'Pendiente', NULL),
    ('Constructora Andina S.A.S.', 'Obra Vial Cundinamarca', 'Permiso de trabajo en alturas', 'Álvaro Nieto Serrano', 'Trabajo en talud — km 34', 60, -15, 'Pendiente', NULL),
    ('Constructora Andina S.A.S.', 'Obra Vial Cundinamarca', 'Certificado de capacitación', 'Marta Escobar Duque', 'Inducción SST obligatoria', 8, 350, 'Aprobado', 'paula.gonzalez'),
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Otro documento SST', 'Nelson Camargo Rojas', 'Registro de entrega de EPP', 2, 28, 'Pendiente', NULL),
    ('Constructora Andina S.A.S.', 'Obra Vial Cundinamarca', 'Inspección de EPP', 'Yuliana Prada Gil', 'Inspección de EPP — cuadrilla nocturna', 15, 15, 'Aprobado', 'paula.gonzalez'),
    -- Minera Altiplano Ltda.
    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Examen médico ocupacional', 'Wilson Cerón Basante', 'Examen periódico — operario de planta', 30, -8, 'Pendiente', NULL),
    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Inspección de EPP', 'Gladys Muñoz Rosero', 'Inspección de EPP en área de flotación', 6, 24, 'Aprobado', 'javier.ramirez'),
    ('Minera Altiplano Ltda.', 'Campamento Alto Putumayo', 'Permiso de trabajo en alturas', 'Fabián Ortega Villota', 'Mantenimiento de tolva elevada', 50, -20, 'Pendiente', NULL),
    ('Minera Altiplano Ltda.', 'Campamento Alto Putumayo', 'Certificado de capacitación', 'Ingrid Delgado Chamorro', 'Manejo de sustancias químicas', 12, 340, 'Aprobado', 'daniela.morales'),
    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Otro documento SST', 'Édgar Getial Bastidas', 'Registro de matriz de riesgos actualizado', 3, 27, 'Pendiente', NULL),
    ('Minera Altiplano Ltda.', 'Campamento Alto Putumayo', 'Inspección de EPP', 'Lorena Paz Enríquez', 'Inspección de EPP — turno campamento', 18, 12, 'Aprobado', 'daniela.morales'),
    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Examen médico ocupacional', 'Ramiro Bolaños Quiroz', 'Examen de retiro', 5, 25, 'Pendiente', NULL),
    ('Minera Altiplano Ltda.', 'Campamento Alto Putumayo', 'Permiso de trabajo en alturas', 'Carolina Rosero Insuasty', 'Reparación de techo de campamento', 22, -1, 'Pendiente', NULL),
    -- Agroindustrias del Valle S.A.
    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Inspección de EPP', 'Orlando Victoria Sánchez', 'Inspección de EPP línea de empaque', 7, 23, 'Aprobado', 'esteban.pena'),
    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Examen médico ocupacional', 'Piedad Collazos Zúñiga', 'Examen periódico — manipulación de alimentos', 33, -5, 'Pendiente', NULL),
    ('Agroindustrias del Valle S.A.', 'Bodega de Insumos — Buga', 'Otro documento SST', 'Harold Manzano Trujillo', 'Ficha de seguridad de insumo químico nuevo', 1, 29, 'Pendiente', NULL),
    ('Agroindustrias del Valle S.A.', 'Bodega de Insumos — Buga', 'Certificado de capacitación', 'Sandra Viveros Marín', 'Manejo seguro de montacargas', 45, 320, 'Aprobado', 'valentina.suarez'),
    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Permiso de trabajo en alturas', 'Julián Herrera Cabezas', 'Mantenimiento de silos', 28, -10, 'Pendiente', NULL),
    ('Agroindustrias del Valle S.A.', 'Bodega de Insumos — Buga', 'Inspección de EPP', 'Norma Castrillón Vega', 'Inspección de EPP — recepción de insumos', 9, 21, 'Aprobado', 'valentina.suarez'),
    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Otro documento SST', 'Bernardo Salazar Cifuentes', 'Registro de simulacro de emergencia', 14, 16, 'Pendiente', NULL),
    ('Agroindustrias del Valle S.A.', 'Bodega de Insumos — Buga', 'Examen médico ocupacional', 'Consuelo Arboleda Rentería', 'Examen de ingreso — nueva contratación', 4, 26, 'Aprobado', 'esteban.pena'),
    -- Textiles Manizales S.A.S.
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Inspección de EPP', 'Rubén Aristizábal Correa', 'Inspección de EPP — sección de tejeduría', 11, 19, 'Aprobado', 'felipe.castano'),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Examen médico ocupacional', 'Beatriz Londoño Vélez', 'Examen periódico — riesgo ergonómico', 38, -6, 'Pendiente', NULL),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Certificado de capacitación', 'Iván Zapata Restrepo', 'Capacitación en riesgo ergonómico', 20, 330, 'Aprobado', 'camila.jimenez'),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Permiso de trabajo en alturas', 'Claudia Marín Gómez', 'Mantenimiento de cubierta de la planta', 55, -18, 'Pendiente', NULL),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Otro documento SST', 'Gustavo Ocampo Ríos', 'Registro de mantenimiento de extintores', 6, 24, 'Pendiente', NULL),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Inspección de EPP', 'Amparo Grisales Botero', 'Inspección de EPP — sección de teñido', 16, 14, 'Aprobado', 'felipe.castano'),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Examen médico ocupacional', 'Fernando Cardona Isaza', 'Examen de reintegro laboral', 2, 28, 'Pendiente', NULL),
    -- Logística Caribe S.A.S.
    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Inspección de EPP', 'Yesenia Barrios Polo', 'Inspección de EPP — cuadrilla de cargue', 13, 17, 'Aprobado', 'santiago.lopez'),
    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Certificado de capacitación', 'Deivis Julio Redondo', 'Manejo de montacargas — bodega portuaria', 42, 325, 'Aprobado', 'mariana.diaz'),
    ('Logística Caribe S.A.S.', 'Centro de Distribución — Barranquilla', 'Examen médico ocupacional', 'Katherine Pacheco Vidal', 'Examen periódico — operario logístico', 35, -9, 'Pendiente', NULL),
    ('Logística Caribe S.A.S.', 'Centro de Distribución — Barranquilla', 'Otro documento SST', 'Alexánder Contreras Meza', 'Registro de matriz de riesgos actualizado', 3, 27, 'Pendiente', NULL),
    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Permiso de trabajo en alturas', 'Yolima Herrera Cantillo', 'Revisión de estructura de cubierta de bodega', 48, -12, 'Pendiente', NULL),
    ('Logística Caribe S.A.S.', 'Centro de Distribución — Barranquilla', 'Inspección de EPP', 'Reinaldo Movilla Puche', 'Inspección de EPP — turno de despachos', 8, 22, 'Aprobado', 'mariana.diaz'),
    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Otro documento SST', 'Milena Fontalvo Arrieta', 'Registro de simulacro de emergencia portuaria', 19, 11, 'Pendiente', NULL),
    ('Logística Caribe S.A.S.', 'Centro de Distribución — Barranquilla', 'Certificado de capacitación', 'Osvaldo Salcedo Manjarres', 'Inducción SST obligatoria', 24, 336, 'Aprobado', 'santiago.lopez')
)
INSERT INTO "Documentos" ("IdTipoDocumento", "IdEmpresa", "IdSede", "NombreColaborador", "Actividad",
                          "FechaCaptura", "FechaVencimiento", "Estado", "IdUsuarioAprueba", "FechaFirma")
SELECT td."IdTipoDocumento", e."IdEmpresa", s."IdSede", d.colaborador, d.actividad,
       (CURRENT_DATE - (d.dias_captura || ' days')::interval)::date,
       (CURRENT_DATE + (d.dias_vence || ' days')::interval)::date,
       d.estado,
       ua."IdUsuario",
       CASE WHEN d.estado = 'Aprobado' THEN now() - ((d.dias_captura - 1) || ' days')::interval ELSE NULL END
FROM datos_doc d
JOIN "Empresas" e ON e."Nombre" = d.empresa
JOIN "Sedes" s ON s."IdEmpresa" = e."IdEmpresa" AND s."Nombre" = d.sede
JOIN "TiposDocumento" td ON td."Nombre" = d.tipo
LEFT JOIN "Usuarios" ua ON ua."NombreUsuario" = d.aprueba;

-- =============================================================================
-- ACTAS (15 — 3 por empresa). Origen mezclado a propósito para cubrir las 5
-- plataformas (Manual, Teams, Zoom, GoogleMeet, Webex) — ver el manual de
-- integraciones. Los títulos son únicos: se usan como llave natural en los
-- INSERT siguientes (asistentes, contenido, compromisos).
-- =============================================================================
WITH datos_acta(empresa, sede, tipo, origen, titulo, dias_atras, creador, notas) AS (
    VALUES
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Reunion', 'Manual',
     'Comité paritario COPASST — enero', 12, 'miguel.torres',
     'Acuerdo: renovar el permiso de trabajo en alturas antes de fin de mes, responsable: Diana Beltrán. Pendiente: reforzar la señalización del perímetro de obra, responsable: Nelson Camargo, fecha: en dos semanas.'),
    ('Constructora Andina S.A.S.', 'Obra Vial Cundinamarca', 'Reunion', 'Teams',
     'Inspección de seguridad — obra vial Cundinamarca', 8, 'paula.gonzalez', NULL),
    ('Constructora Andina S.A.S.', 'Obra Torre Norte — Bogotá', 'Capacitacion', 'Manual',
     'Inducción SST para personal nuevo — Torre Norte', 20, 'miguel.torres',
     'Se dictó la inducción a 6 colaboradores nuevos. Tarea: programar el examen médico ocupacional de ingreso para todos, responsable: Miguel Torres, fecha: esta semana.'),

    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Reunion', 'Zoom',
     'Seguimiento de incidentes — planta de beneficio', 15, 'javier.ramirez', NULL),
    ('Minera Altiplano Ltda.', 'Campamento Alto Putumayo', 'Capacitacion', 'Manual',
     'Capacitación en manejo de sustancias químicas', 30, 'daniela.morales',
     'Acuerdo: actualizar las hojas de seguridad (MSDS) disponibles en el campamento, responsable: Ingrid Delgado. Compromiso: instalar una ducha de emergencia adicional en el área de mezclas.'),
    ('Minera Altiplano Ltda.', 'Planta de Beneficio — Pasto', 'Reunion', 'GoogleMeet',
     'Comité paritario COPASST — planta de beneficio', 6, 'javier.ramirez', NULL),

    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Reunion', 'Webex',
     'Revisión de indicadores de seguridad — planta Palmira', 9, 'esteban.pena', NULL),
    ('Agroindustrias del Valle S.A.', 'Bodega de Insumos — Buga', 'Reunion', 'Manual',
     'Inspección de bodega de insumos químicos', 18, 'valentina.suarez',
     'Pendiente: renovar la ficha de seguridad del insumo nuevo, responsable: Harold Manzano, fecha: próxima semana. Tarea: reforzar la ventilación de la bodega de insumos, responsable: Sandra Viveros.'),
    ('Agroindustrias del Valle S.A.', 'Planta de Procesamiento — Palmira', 'Capacitacion', 'Teams',
     'Capacitación en manejo seguro de maquinaria agroindustrial', 25, 'esteban.pena', NULL),

    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Reunion', 'Zoom',
     'Comité paritario COPASST — planta textil', 11, 'felipe.castano', NULL),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Capacitacion', 'Manual',
     'Inducción en riesgo ergonómico — planta textil', 22, 'camila.jimenez',
     'Acuerdo: rotar a los operarios de tejeduría cada 2 horas para reducir exposición ergonómica, responsable: Claudia Marín. Compromiso: gestionar pausas activas guiadas diarias.'),
    ('Textiles Manizales S.A.S.', 'Planta Textil — Manizales', 'Reunion', 'GoogleMeet',
     'Seguimiento de mantenimiento de maquinaria — planta textil', 4, 'felipe.castano', NULL),

    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Reunion', 'Webex',
     'Comité paritario COPASST — bodega portuaria Cartagena', 14, 'santiago.lopez', NULL),
    ('Logística Caribe S.A.S.', 'Centro de Distribución — Barranquilla', 'Reunion', 'Manual',
     'Inspección de zona de cargue — centro de distribución', 7, 'mariana.diaz',
     'Tarea: demarcar nuevamente las zonas de tránsito peatonal en el centro de distribución, responsable: Alexánder Contreras, fecha: en 10 días. Pendiente: revisar el estado de los extintores del área de cargue.'),
    ('Logística Caribe S.A.S.', 'Bodega Portuaria — Cartagena', 'Capacitacion', 'Webex',
     'Capacitación en manejo de montacargas — bodega portuaria', 28, 'santiago.lopez', NULL)
)
INSERT INTO "Actas" ("IdEmpresa", "IdSede", "Tipo", "Titulo", "Fecha", "Asistentes", "Notas",
                     "IdUsuarioCreador", "Origen", "IdReunionExterna", "UrlIngresoExterna", "FechaSincronizacion")
SELECT e."IdEmpresa", s."IdSede", da.tipo, da.titulo,
       (CURRENT_DATE - (da.dias_atras || ' days')::interval)::date,
       NULL,
       da.notas,
       uc."IdUsuario", da.origen,
       CASE WHEN da.origen <> 'Manual' THEN 'ext-' || md5(da.titulo) END,
       CASE WHEN da.origen <> 'Manual' THEN 'https://videollamada.example.com/' || md5(da.titulo) END,
       CASE WHEN da.origen <> 'Manual' THEN now() - (da.dias_atras || ' days')::interval END
FROM datos_acta da
JOIN "Empresas" e ON e."Nombre" = da.empresa
JOIN "Sedes" s ON s."IdEmpresa" = e."IdEmpresa" AND s."Nombre" = da.sede
JOIN "Usuarios" uc ON uc."NombreUsuario" = da.creador;

-- =============================================================================
-- ASISTENTES (solo actas sincronizadas — Manual usa el campo "Asistentes" en
-- texto libre, que en este set de datos se dejó vacío a propósito para que el
-- ejemplo cubra ambos estilos de registro de asistencia).
-- =============================================================================
WITH datos_asist(titulo, nombre, correo, minutos) AS (
    VALUES
    ('Inspección de seguridad — obra vial Cundinamarca', 'Paula González Herrera', 'paula.gonzalez@sstcontrol.demo', 48),
    ('Inspección de seguridad — obra vial Cundinamarca', 'Álvaro Nieto Serrano', 'a.nieto@constructoraandina.demo', 48),
    ('Seguimiento de incidentes — planta de beneficio', 'Javier Ramírez Castro', 'javier.ramirez@sstcontrol.demo', 55),
    ('Seguimiento de incidentes — planta de beneficio', 'Gladys Muñoz Rosero', 'g.munoz@mineraaltiplano.demo', 55),
    ('Comité paritario COPASST — planta de beneficio', 'Javier Ramírez Castro', 'javier.ramirez@sstcontrol.demo', 62),
    ('Comité paritario COPASST — planta de beneficio', 'Wilson Cerón Basante', 'w.ceron@mineraaltiplano.demo', 60),
    ('Revisión de indicadores de seguridad — planta Palmira', 'Esteban Peña Molina', 'esteban.pena@sstcontrol.demo', 40),
    ('Revisión de indicadores de seguridad — planta Palmira', 'Orlando Victoria Sánchez', 'o.victoria@agroindustriasvalle.demo', 40),
    ('Capacitación en manejo seguro de maquinaria agroindustrial', 'Esteban Peña Molina', 'esteban.pena@sstcontrol.demo', 75),
    ('Capacitación en manejo seguro de maquinaria agroindustrial', 'Piedad Collazos Zúñiga', 'p.collazos@agroindustriasvalle.demo', 75),
    ('Comité paritario COPASST — planta textil', 'Felipe Castaño Duarte', 'felipe.castano@sstcontrol.demo', 50),
    ('Comité paritario COPASST — planta textil', 'Rubén Aristizábal Correa', 'r.aristizabal@textilesmanizales.demo', 50),
    ('Seguimiento de mantenimiento de maquinaria — planta textil', 'Felipe Castaño Duarte', 'felipe.castano@sstcontrol.demo', 35),
    ('Seguimiento de mantenimiento de maquinaria — planta textil', 'Gustavo Ocampo Ríos', 'g.ocampo@textilesmanizales.demo', 35),
    ('Comité paritario COPASST — bodega portuaria Cartagena', 'Santiago López Cárdenas', 'santiago.lopez@sstcontrol.demo', 58),
    ('Comité paritario COPASST — bodega portuaria Cartagena', 'Yesenia Barrios Polo', 'y.barrios@logisticacaribe.demo', 58),
    ('Capacitación en manejo de montacargas — bodega portuaria', 'Santiago López Cárdenas', 'santiago.lopez@sstcontrol.demo', 80),
    ('Capacitación en manejo de montacargas — bodega portuaria', 'Deivis Julio Redondo', 'd.julio@logisticacaribe.demo', 80)
)
INSERT INTO "AsistentesReunion" ("IdActa", "Nombre", "CorreoElectronico", "HoraIngreso", "HoraSalida", "DuracionMinutos")
SELECT a."IdActa", da.nombre, da.correo,
       a."FechaSincronizacion", a."FechaSincronizacion" + (da.minutos || ' minutes')::interval, da.minutos
FROM datos_asist da
JOIN "Actas" a ON a."Titulo" = da.titulo;

-- =============================================================================
-- CONTENIDO DE REUNIÓN (transcripción/resumen — solo actas sincronizadas).
-- El texto incluye marcadores que el bot de minutas reconoce
-- ("Acuerdo:"/"Tarea:"/"Pendiente:"/"Compromiso:") para que generar-minuta
-- produzca compromisos reales sobre estas actas.
-- =============================================================================
WITH datos_contenido(titulo, resumen) AS (
    VALUES
    ('Inspección de seguridad — obra vial Cundinamarca',
     'Se revisó el avance de pavimentación del tramo km 30-36. Acuerdo: reforzar la señalización nocturna del tramo, responsable: Álvaro Nieto, fecha: esta semana. Pendiente: renovar el permiso de trabajo en alturas del talud km 34, responsable: Álvaro Nieto.'),
    ('Seguimiento de incidentes — planta de beneficio',
     'Se revisaron los cuasi-incidentes del último mes en el área de flotación. Tarea: instalar guardas adicionales en la banda transportadora 3, responsable: Gladys Muñoz, fecha: en tres semanas. Compromiso: actualizar la matriz de riesgos de la planta.'),
    ('Comité paritario COPASST — planta de beneficio',
     'Sesión ordinaria mensual del comité. Acuerdo: programar el examen médico periódico pendiente de Wilson Cerón antes de fin de mes, responsable: Wilson Cerón. Se aprobó el cronograma de inspecciones del trimestre.'),
    ('Revisión de indicadores de seguridad — planta Palmira',
     'Se presentaron los indicadores de accidentalidad del trimestre, en descenso frente al periodo anterior. Pendiente: renovar el examen médico ocupacional de Piedad Collazos, responsable: Orlando Victoria, fecha: próxima semana.'),
    ('Capacitación en manejo seguro de maquinaria agroindustrial',
     'Se capacitó a 14 operarios en el uso seguro de la línea de empaque. Tarea: reforzar la señalización de zonas de atrapamiento en la línea, responsable: Orlando Victoria. Compromiso: repetir la capacitación cada seis meses.'),
    ('Comité paritario COPASST — planta textil',
     'Se revisó el reporte de condiciones ergonómicas de la sección de tejeduría. Acuerdo: gestionar la compra de sillas ergonómicas para el área de tejeduría, responsable: Rubén Aristizábal, fecha: próximo mes.'),
    ('Seguimiento de mantenimiento de maquinaria — planta textil',
     'Revisión del plan de mantenimiento preventivo de telares. Pendiente: renovar el permiso de trabajo en alturas para el mantenimiento de cubierta, responsable: Claudia Marín, fecha: en dos semanas.'),
    ('Comité paritario COPASST — bodega portuaria Cartagena',
     'Sesión ordinaria del comité paritario de la bodega portuaria. Acuerdo: revisar el estado estructural de la cubierta antes de la temporada de lluvias, responsable: Yolima Herrera, fecha: este mes. Tarea: actualizar el plan de emergencia portuario.'),
    ('Capacitación en manejo de montacargas — bodega portuaria',
     'Se certificó a 9 operarios en manejo seguro de montacargas. Compromiso: programar recertificación anual para todo el personal certificado, responsable: Deivis Julio.')
)
INSERT INTO "ContenidosReunion" ("IdActa", "Resumen", "TipoContenido")
SELECT a."IdActa", dc.resumen, 'summary'
FROM datos_contenido dc
JOIN "Actas" a ON a."Titulo" = dc.titulo;

-- =============================================================================
-- COMPROMISOS — origen "Bot": exactamente los que produciría
-- ServicioResumenReunionHeuristico al leer Notas/ContenidoReunion.Resumen de
-- arriba (mismo patrón de extracción — ver el manual de actas y bot de
-- minutas). Origen "Manual": agregados a mano por el Asesor SST.
-- =============================================================================
WITH datos_compromiso(titulo, descripcion, responsable, dias_limite, estado, origen) AS (
    VALUES
    -- Generados por el bot (coinciden con los marcadores del texto de arriba)
    ('Comité paritario COPASST — enero', 'renovar el permiso de trabajo en alturas antes de fin de mes', 'Diana Beltrán', 5, 'Pendiente', 'Bot'),
    ('Comité paritario COPASST — enero', 'reforzar la señalización del perímetro de obra', 'Nelson Camargo', 14, 'Pendiente', 'Bot'),
    ('Inducción SST para personal nuevo — Torre Norte', 'programar el examen médico ocupacional de ingreso para todos', 'Miguel Torres', 7, 'Cumplido', 'Bot'),
    ('Inspección de seguridad — obra vial Cundinamarca', 'reforzar la señalización nocturna del tramo', 'Álvaro Nieto', 7, 'Pendiente', 'Bot'),
    ('Inspección de seguridad — obra vial Cundinamarca', 'renovar el permiso de trabajo en alturas del talud km 34', 'Álvaro Nieto', NULL, 'Pendiente', 'Bot'),
    ('Capacitación en manejo de sustancias químicas', 'actualizar las hojas de seguridad (MSDS) disponibles en el campamento', 'Ingrid Delgado', NULL, 'Cumplido', 'Bot'),
    ('Seguimiento de incidentes — planta de beneficio', 'instalar guardas adicionales en la banda transportadora 3', 'Gladys Muñoz', 21, 'Pendiente', 'Bot'),
    ('Comité paritario COPASST — planta de beneficio', 'programar el examen médico periódico pendiente de Wilson Cerón antes de fin de mes', 'Wilson Cerón', 20, 'Pendiente', 'Bot'),
    ('Inspección de bodega de insumos químicos', 'renovar la ficha de seguridad del insumo nuevo', 'Harold Manzano', 7, 'Pendiente', 'Bot'),
    ('Revisión de indicadores de seguridad — planta Palmira', 'renovar el examen médico ocupacional de Piedad Collazos', 'Orlando Victoria', 7, 'Pendiente', 'Bot'),
    ('Capacitación en manejo seguro de maquinaria agroindustrial', 'reforzar la señalización de zonas de atrapamiento en la línea', 'Orlando Victoria', NULL, 'Cumplido', 'Bot'),
    ('Comité paritario COPASST — planta textil', 'gestionar la compra de sillas ergonómicas para el área de tejeduría', 'Rubén Aristizábal', 30, 'Pendiente', 'Bot'),
    ('Inducción en riesgo ergonómico — planta textil', 'rotar a los operarios de tejeduría cada 2 horas para reducir exposición ergonómica', 'Claudia Marín', NULL, 'Cumplido', 'Bot'),
    ('Seguimiento de mantenimiento de maquinaria — planta textil', 'renovar el permiso de trabajo en alturas para el mantenimiento de cubierta', 'Claudia Marín', 14, 'Pendiente', 'Bot'),
    ('Comité paritario COPASST — bodega portuaria Cartagena', 'revisar el estado estructural de la cubierta antes de la temporada de lluvias', 'Yolima Herrera', 30, 'Pendiente', 'Bot'),
    ('Inspección de zona de cargue — centro de distribución', 'demarcar nuevamente las zonas de tránsito peatonal en el centro de distribución', 'Alexánder Contreras', 10, 'Pendiente', 'Bot'),
    ('Capacitación en manejo de montacargas — bodega portuaria', 'programar recertificación anual para todo el personal certificado', 'Deivis Julio', 300, 'Pendiente', 'Bot'),
    -- Agregados manualmente por el Asesor SST (sin marcador textual detectable)
    ('Inducción SST para personal nuevo — Torre Norte', 'Verificar entrega de EPP completo a cada colaborador nuevo antes de asignarlos a frente de obra', 'Miguel Torres', 3, 'Cumplido', 'Manual'),
    ('Inspección de bodega de insumos químicos', 'Reforzar la ventilación de la bodega de insumos', 'Sandra Viveros', 21, 'Pendiente', 'Manual'),
    ('Inspección de zona de cargue — centro de distribución', 'Revisar el estado de los extintores del área de cargue', NULL, NULL, 'Pendiente', 'Manual')
)
INSERT INTO "CompromisosActa" ("IdActa", "Descripcion", "Responsable", "FechaLimite", "Estado", "Origen")
SELECT a."IdActa", dcp.descripcion, dcp.responsable,
       CASE WHEN dcp.dias_limite IS NOT NULL THEN (CURRENT_DATE + (dcp.dias_limite || ' days')::interval)::date END,
       dcp.estado, dcp.origen
FROM datos_compromiso dcp
JOIN "Actas" a ON a."Titulo" = dcp.titulo;

-- ---- Vincula algunos compromisos al documento cuyo cambio los cierra ----
-- ("integrar cambios en documentos" a partir de una minuta — ver manual de actas).
UPDATE "CompromisosActa" c SET "IdDocumentoRelacionado" = doc_sel."IdDocumento"
FROM (
    SELECT c2."IdCompromiso", d."IdDocumento",
           ROW_NUMBER() OVER (PARTITION BY c2."IdCompromiso" ORDER BY d."IdDocumento") AS rn
    FROM "CompromisosActa" c2
    JOIN "Actas" a2 ON a2."IdActa" = c2."IdActa"
    JOIN "Documentos" d ON d."IdEmpresa" = a2."IdEmpresa"
    WHERE (c2."Responsable" = 'Álvaro Nieto' AND c2."Descripcion" LIKE '%permiso de trabajo en alturas%' AND d."NombreColaborador" = 'Álvaro Nieto Serrano')
       OR (c2."Responsable" = 'Wilson Cerón' AND d."NombreColaborador" = 'Wilson Cerón Basante')
       OR (c2."Responsable" = 'Piedad Collazos' AND d."NombreColaborador" = 'Piedad Collazos Zúñiga')
       OR (c2."Responsable" = 'Claudia Marín' AND c2."Descripcion" LIKE '%permiso de trabajo en alturas%' AND d."NombreColaborador" = 'Claudia Marín Gómez')
) AS doc_sel
WHERE c."IdCompromiso" = doc_sel."IdCompromiso" AND doc_sel.rn = 1;

-- =============================================================================
-- DIGITALIZACIÓN (OCR) — evidencia física escaneada de ejemplo, sobre 6
-- documentos ya aprobados (uno por empresa aproximadamente).
-- =============================================================================
WITH docs_para_escanear AS (
    SELECT d."IdDocumento", d."NombreColaborador", td."Nombre" AS tipo, d."FechaVencimiento"
    FROM "Documentos" d JOIN "TiposDocumento" td ON td."IdTipoDocumento" = d."IdTipoDocumento"
    WHERE d."NombreColaborador" IN (
        'Jorge Salamanca Ruiz', 'Gladys Muñoz Rosero', 'Norma Castrillón Vega',
        'Rubén Aristizábal Correa', 'Yesenia Barrios Polo', 'Sandra Viveros Marín'
    )
)
INSERT INTO "DigitalizacionesDocumento" ("IdDocumento", "NombreArchivoOriginal", "TipoContenido", "TamanioBytes", "TextoExtraido", "Confianza")
SELECT "IdDocumento",
       'escaneo_' || lower(replace("NombreColaborador", ' ', '_')) || '.jpg',
       'image/jpeg',
       (1200000 + ("IdDocumento" * 37) % 900000),
       upper(tipo) || E'\nColaborador: ' || "NombreColaborador" || E'\nVence: ' || to_char("FechaVencimiento", 'DD/MM/YYYY') ||
           E'\n\n(texto reconocido automáticamente de la imagen escaneada)',
       round((84 + ("IdDocumento" % 14))::numeric, 1)
FROM docs_para_escanear;

-- =============================================================================
-- BITÁCORA DE AUDITORÍA — ejemplo de trazabilidad de acciones clave, para que
-- el Auditor SST tenga algo real que revisar desde el primer día.
-- =============================================================================
INSERT INTO "RegistrosAuditoria" ("IdUsuario", "Accion", "Detalle", "FechaHora")
SELECT u."IdUsuario", ra.accion, ra.detalle, now() - (ra.dias_atras || ' days')::interval
FROM (VALUES
    ('miguel.torres',   'Documento firmado',       'Inspección de EPP — Jorge Salamanca Ruiz', 10),
    ('paula.gonzalez',  'Documento firmado',       'Inspección de EPP — Yuliana Prada Gil', 15),
    ('javier.ramirez',  'Documento firmado',       'Inspección de EPP — Gladys Muñoz Rosero', 6),
    ('esteban.pena',    'Documento escaneado',     'Inspección de EPP — Orlando Victoria Sánchez', 7),
    ('miguel.torres',   'Minuta generada por el bot', 'Comité paritario COPASST — enero — 2 compromiso(s)', 12),
    ('santiago.lopez',  'Minuta generada por el bot', 'Comité paritario COPASST — bodega portuaria Cartagena — 1 compromiso(s)', 14),
    ('felipe.castano',  'Compromiso cumplido',     'rotar a los operarios de tejeduría cada 2 horas para reducir exposición ergonómica', 5),
    ('andrea.martinez', 'Empresa registrada',      'Logística Caribe S.A.S.', 90),
    ('carlos.rivera',   'Rol asignado',            'Auditor SST → ricardo.pardo', 60),
    ('ricardo.pardo',   'Inicio de sesión',        NULL, 1)
) AS ra(usuario, accion, detalle, dias_atras)
JOIN "Usuarios" u ON u."NombreUsuario" = ra.usuario;

COMMIT;
