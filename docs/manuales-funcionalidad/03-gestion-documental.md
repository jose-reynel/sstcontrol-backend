# Manual de funcionalidad — Gestión documental

## ¿Para qué sirve?
Lleva el control de todo documento de SST que un colaborador necesita tener
vigente: inspecciones de EPP, permisos de trabajo en alturas, exámenes
médicos ocupacionales, certificados de capacitación, etc. Cada documento
recorre un ciclo de vida: **captura → pendiente de firma → aprobado
(firmado) → vigente → por vencer → vencido → renovado**.

## Ver documentos
Pantalla **Documentos** — lista todos los registros, más recientes primero,
con su estado visual (una insignia de color: pendiente, vigente, por vencer,
vencido, firmado). Puedes cargar más resultados con "Cargar más" a medida
que necesites ver los antiguos. El Panel principal también muestra un
resumen: cuántos están pendientes de firma y cuántos vencidos, en tiempo
real, sobre **todos** los documentos del sistema (no solo los que ya cargó
tu pantalla).

## Registrar un documento nuevo
Cualquier usuario autenticado puede hacerlo:
1. En **Documentos**, completa: tipo de documento, colaborador, actividad, y
   fecha de vencimiento.
2. El documento queda con estado **"Pendiente"** — todavía no está aprobado.

## Firmar (aprobar) un documento
Reservado a quien tenga el permiso de firma — típicamente el **Asesor SST**
o el **Administrador SST**, nunca el Auditor SST (su rol es solo de
consulta):
1. Abre el documento pendiente y pulsa **"Firmar"**.
2. Queda registrado quién lo aprobó y cuándo. El estado cambia a
   **"Firmado"**.

## Renovar un documento vencido o por vencer
Cualquier usuario autenticado puede renovar: crea automáticamente un
**registro nuevo**, pendiente de firma otra vez, con 30 días de vigencia
desde hoy — el documento original no se borra, queda como historial.

## Eliminar un documento
Acción destructiva, reservada al **Administrador SST** (permiso
`documentos.eliminar`). Úsala solo para corregir un registro duplicado o mal
capturado — no como forma de "cerrar" un documento vencido (para eso está
Renovar).

## Escanear un documento físico (digitalización / OCR)
Si tienes el documento físico en papel (una firma manuscrita en una hoja de
inspección, por ejemplo) y quieres tener su versión digital y buscable:
1. Abre el documento correspondiente ya registrado en el sistema.
2. Pulsa **"Escanear evidencia"** y toma una foto o sube una imagen (JPEG,
   PNG, BMP o TIFF — un PDF debe convertirse a imagen primero).
3. El sistema reconoce el texto de la imagen automáticamente (OCR) y lo deja
   guardado junto con un porcentaje de confianza del reconocimiento.
4. Puedes volver a escanear si la primera foto salió borrosa o incompleta —
   reemplaza a la anterior.

Reservado a quien tenga el permiso `documentos.escanear` — normalmente el
Asesor SST y el Administrador SST.

**Nota:** el texto reconocido puede tener errores si la foto está borrosa,
mal iluminada o el papel está deteriorado — siempre revisa el resultado
antes de confiar en él para una búsqueda o una auditoría.

## Preguntas frecuentes
**¿Por qué no veo el botón "Firmar" en un documento?**
Porque tu rol no tiene el permiso `documentos.firmar` — es el caso normal
para un Auditor SST, cuyo rol es de consulta.

**¿Renovar borra el documento vencido?**
No. Queda como historial; solo se crea un registro nuevo pendiente de firma.

**¿Puedo escanear un documento que todavía no existe en el sistema?**
No — primero regístralo con sus datos (colaborador, tipo, vencimiento), y
después adjúntale el escaneo como evidencia.
