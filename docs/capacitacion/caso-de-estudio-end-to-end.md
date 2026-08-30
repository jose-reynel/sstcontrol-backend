# Caso de estudio end to end — Constructora Andina S.A.S.

Escenario real, sobre los datos que dejan sembrados los scripts de
`docs/datos-semilla/` — cada nombre, empresa y documento mencionado aquí
existe literalmente en la base de datos después de correrlos. Úsalo como
guion de demostración o como ejercicio guiado de capacitación.

## Los personajes
- **Miguel Torres Salazar** (`miguel.torres`) — Asesor SST de
  **Constructora Andina S.A.S.**, sede *Obra Torre Norte — Bogotá*.
- **Andrea Martínez Rojas** (`andrea.martinez`) — Administradora SST de la
  plataforma.
- **Ricardo Pardo Aguilar** (`ricardo.pardo`) — Auditor SST.
- Contraseña de los tres: `Practica#2026`.

## Parte 1 — Miguel registra y gestiona documentación (Asesor SST)

1. Miguel inicia sesión con `miguel.torres`. Como Asesor SST, ve Documentos,
   Actas y Empresas — no ve Administración (no tiene `accesos.administrar`).
2. Va a **Documentos** y encuentra el registro de **Diana Beltrán Ospina**
   ("Permiso de trabajo en alturas — instalación de fachada, torre norte,
   piso 14"), todavía pendiente. Como sí tiene el permiso
   `documentos.firmar`, pulsa **Firmar**.
3. Revisa el Panel: el resumen de documentos vencidos incluye a
   **Álvaro Nieto Serrano** (permiso de trabajo en alturas de la Obra Vial
   Cundinamarca, vencido). Miguel abre ese documento y pulsa **Renovar** —
   queda un registro nuevo, pendiente de firma, con 30 días de vigencia.
4. Miguel tiene en la mano la hoja física de inspección de EPP de
   **Jorge Salamanca Ruiz** (ya está en el sistema, aprobada). La fotografía
   y pulsa **Escanear evidencia** — el sistema reconoce el texto y queda
   guardada la digitalización con su porcentaje de confianza.

## Parte 2 — El comité paritario y el bot de minutas

1. En **Actas**, Miguel abre **"Comité paritario COPASST — enero"**
   (registrada manualmente, con notas de la reunión). La expande y pulsa
   **"Generar minuta con el bot"**.
2. El bot lee las notas y encuentra dos compromisos: *"renovar el permiso
   de trabajo en alturas antes de fin de mes"* (responsable: Diana Beltrán)
   y *"reforzar la señalización del perímetro de obra"* (responsable:
   Nelson Camargo, con fecha).
3. Miguel marca el primero como **cumplido** en cuanto Diana renovó el
   permiso, y lo **vincula al documento** de Diana Beltrán recién renovado
   — queda trazado que ese compromiso se cerró con ese cambio documental
   concreto, no solo "de palabra".

## Parte 3 — Una reunión que llegó sola, desde Teams

1. Paula González (compañera de Miguel en la misma empresa) tuvo la reunión
   **"Inspección de seguridad — obra vial Cundinamarca"** por Microsoft
   Teams. El sistema la sincronizó automáticamente — el acta aparece con la
   insignia de origen "Teams", con los asistentes reales (Paula González y
   Álvaro Nieto, con su hora de entrada y salida) y el resumen de la
   reunión ya cargado.
2. Sobre esa acta también corre el bot: encuentra *"reforzar la
   señalización nocturna del tramo"* y *"renovar el permiso de trabajo en
   alturas del talud km 34"* — ambos con Álvaro Nieto como responsable.

## Parte 4 — Andrea administra la plataforma (Administrador SST)

1. Andrea inicia sesión con `andrea.martinez`. A diferencia de Miguel, sí
   ve **Administración**.
2. En **Empresas**, confirma que las 5 empresas del escenario están
   registradas, cada una con sus sedes.
3. En **Usuarios**, revisa que Miguel y Paula tengan el rol **Asesor SST**
   — y aprovecha para asignarle también el rol **Auditor SST** a un tercer
   usuario que necesita empezar a revisar cumplimiento sin operar el
   sistema.
4. Solo Andrea (permiso `documentos.eliminar`) puede borrar un documento
   duplicado que alguien cargó dos veces por error — Miguel no ve ese botón
   en absoluto.

## Parte 5 — Ricardo audita (Auditor SST)

1. Ricardo inicia sesión con `ricardo.pardo`. Ve Documentos (consulta
   libre) y Actas (tiene `actas.ver`) — pero en ningún lado ve un botón de
   crear, firmar, escanear o eliminar: su rol es exclusivamente de
   consulta.
2. En el Panel, identifica cuántos documentos están vencidos hoy en todo el
   sistema (el resumen agregado, no solo lo que él haya cargado en
   pantalla) y anota cuáles empresas concentran más vencidos.
3. Abre **"Comité paritario COPASST — enero"**, revisa los compromisos ya
   generados por el bot, y confirma cuáles siguen pendientes — insumo
   directo para su informe de auditoría. No puede marcarlos ni modificarlos
   él mismo; si encuentra algo vencido sin gestionar, lo reporta a Andrea o
   a Miguel.

## Qué queda demostrado
- Los tres roles (Administrador SST, Asesor SST, Auditor SST) con permisos
  realmente distintos, no solo etiquetas.
- El ciclo documental completo: captura → firma → vencimiento → renovación
  → escaneo.
- Una reunión sincronizada automáticamente desde una plataforma externa
  (Teams, en este caso — el mismo patrón aplica a Zoom, Google Meet y
  Webex).
- El bot de minutas generando compromisos reales a partir de texto, tanto
  de una acta manual como de una sincronizada.
- Un compromiso cerrado con trazabilidad real hacia el documento que lo
  resolvió.
