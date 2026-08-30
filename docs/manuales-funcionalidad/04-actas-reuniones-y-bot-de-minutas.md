# Manual de funcionalidad — Actas, reuniones y bot de minutas

## ¿Para qué sirve?
Registra reuniones (ej. comité paritario COPASST) y capacitaciones, ya sea a
mano o trayéndolas automáticamente desde Microsoft Teams, Google Meet, Zoom
o Cisco Webex. Y, sobre esa acta, un **bot** que lee su contenido y
propone los compromisos/tareas de seguimiento que se acordaron — sin que
tengas que releer toda la transcripción para no perder nada.

Requiere el permiso `actas.ver` para consultar y `actas.crear` para
registrar/gestionar — el Auditor SST solo tiene el primero.

## Registrar un acta manualmente
1. Ve a **Actas** → "Registrar acta".
2. Completa: empresa, sede, tipo (Reunión o Capacitación), título, fecha,
   asistentes y notas.
3. Guarda. El acta queda visible en la lista, más recientes primero.

## Traer una reunión ya realizada desde Teams / Meet / Zoom / Webex
En vez de escribir el acta a mano, el sistema puede sincronizarla
automáticamente desde la plataforma de videollamada donde ocurrió — trae el
título, los asistentes reales (con hora de entrada/salida) y el resumen o
transcripción de la reunión. Esto lo dispara:
- **Automáticamente**, cuando la reunión termina en la plataforma (si tu
  organización configuró el webhook correspondiente — ver el manual técnico
  de backend).
- **A mano**, si un Administrador SST con el permiso `reuniones.sincronizar`
  la trae bajo demanda indicando el identificador de la reunión.

## El bot de minutas: generar compromisos automáticamente
Una vez que un acta tiene contenido (transcripción o notas con suficiente
detalle), puedes pedirle al bot que la lea y proponga los compromisos:
1. Abre el acta y pulsa **"Generar minuta con el bot"**.
2. El bot busca en el texto frases que suenan a acuerdo o tarea pendiente
   (por ejemplo, que empiecen con "Acuerdo:", "Tarea:", "Pendiente:" o
   "Compromiso:") y, de cada una, intenta identificar quién es el
   responsable y para cuándo, si el texto lo dice explícitamente.
3. Cada compromiso encontrado queda listado en el acta, marcado como
   generado por el bot.

**Importante — qué SÍ y qué NO hace el bot:** es un asistente basado en
patrones de texto, no una inteligencia artificial que "entiende" la
conversación. Si el texto no usa esas palabras clave, o el responsable/fecha
no están escritos explícitamente, el bot no los inventa — simplemente no
encuentra nada, o encuentra el compromiso sin responsable/fecha asignados.
Siempre revisa lo que el bot generó antes de darlo por bueno; puedes
agregar compromisos a mano en cualquier momento si el bot no captó algo.

## Dar seguimiento a un compromiso
- **Marcar como cumplido**: cuando la tarea ya se hizo, márcalo en la lista
  de compromisos del acta — queda con la fecha de cumplimiento.
- **Vincular a un documento**: si el compromiso se cierra con un cambio
  documental concreto (por ejemplo, "renovar el procedimiento de trabajo en
  alturas"), vincúlalo al Documento correspondiente ya registrado en el
  sistema — así queda trazado hasta el cambio real que lo cerró, no solo
  marcado como "listo" de palabra.

## Preguntas frecuentes
**¿El bot puede generar compromisos de una acta manual sin transcripción?**
Sí, si las notas que escribiste a mano usan esas palabras clave
("Acuerdo:", "Tarea:"...). Si las notas son muy libres, probablemente no
encuentre nada — en ese caso, agrega los compromisos a mano.

**¿Puedo correr el bot varias veces sobre la misma acta?**
Sí, es seguro — no duplica los compromisos que ya había generado antes sobre
el mismo texto.

**Como Auditor SST, ¿puedo generar minutas o marcar compromisos?**
No — tu rol solo puede consultar actas y compromisos, no crearlos ni
modificarlos. Repórtalo al Administrador SST o al Asesor SST responsable si
detectas algo pendiente en una auditoría.
