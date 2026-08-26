-- =========================================================
-- DATOS INICIALES CON UUID: CONTROL DE ACCESO
-- =========================================================

INSERT INTO modules (id, name, description) VALUES
('m0000001-0000-0000-0000-000000000001', 'GESTION_USUARIOS', 'Módulo de usuarios y accesos'),
('m0000001-0000-0000-0000-000000000002', 'INCIDENTES_ACCIDENTES', 'Reporte e investigación de incidentes'),
('m0000001-0000-0000-0000-000000000003', 'CAPACITACIONES', 'Programación y asistencia a formaciones')
ON DUPLICATE KEY UPDATE description=VALUES(description);

INSERT INTO permissions (id, module_id, name, code, description) VALUES
('p0000001-0000-0000-0000-000000000001', 'm0000001-0000-0000-0000-000000000001', 'Ver Usuarios', 'USER_READ', 'Consultar lista de usuarios'),
('p0000001-0000-0000-0000-000000000002', 'm0000001-0000-0000-0000-000000000001', 'Crear/Editar Usuarios', 'USER_WRITE', 'Crear o modificar datos de usuarios'),
('p0000001-0000-0000-0000-000000000003', 'm0000001-0000-0000-0000-000000000002', 'Ver Incidentes', 'INCIDENT_READ', 'Consultar reportes de incidentes'),
('p0000001-0000-0000-0000-000000000004', 'm0000001-0000-0000-0000-000000000002', 'Registrar Incidente', 'INCIDENT_CREATE', 'Reportar un nuevo incidente SST'),
('p0000001-0000-0000-0000-000000000005', 'm0000001-0000-0000-0000-000000000003', 'Ver Capacitaciones', 'TRAINING_READ', 'Consultar programa de capacitaciones')
ON DUPLICATE KEY UPDATE description=VALUES(description);

INSERT INTO roles (id, name, description) VALUES
('r0000001-0000-0000-0000-000000000001', 'ROLE_ADMIN', 'Administrador global del sistema SST'),
('r0000001-0000-0000-0000-000000000002', 'ROLE_COORDINADOR_SST', 'Líder / Coordinador del programa SST'),
('r0000001-0000-0000-0000-000000000003', 'ROLE_EMPLEADO', 'Trabajador final que reporta y asiste')
ON DUPLICATE KEY UPDATE description=VALUES(description);

INSERT INTO role_permissions (role_id, permission_id) VALUES
('r0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000001'),
('r0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000002'),
('r0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000003'),
('r0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000004'),
('r0000001-0000-0000-0000-000000000001', 'p0000001-0000-0000-0000-000000000005')
ON DUPLICATE KEY UPDATE role_id=role_id;