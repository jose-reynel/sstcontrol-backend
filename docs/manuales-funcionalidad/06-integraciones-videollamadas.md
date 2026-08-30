# Manual de funcionalidad — Integraciones con videollamadas

## ¿Para qué sirve?
Evita transcribir a mano cada reunión de SST: si la haces por Microsoft
Teams, Google Meet, Zoom o Cisco Webex, el sistema puede traer
automáticamente el acta con sus asistentes reales y su contenido
(transcripción, resumen o grabación, según lo que la plataforma entregue).

## Cómo llega una reunión al sistema
Hay dos caminos, y no son excluyentes:

1. **Automático (webhook)**: cuando la reunión termina en la plataforma
   (Teams, Meet, Zoom o Webex), esta le avisa al sistema por sí sola y el
   acta se crea sin que nadie tenga que hacer nada — siempre que tu
   organización haya configurado esa conexión (ver el manual técnico de
   backend, sección de integraciones).
2. **Manual, bajo demanda**: un Administrador SST con el permiso
   `reuniones.sincronizar` puede pedir explícitamente que se traiga una
   reunión puntual, indicando su identificador (el sistema sabe cómo
   pedírselo a cada plataforma).

## Qué trae cada reunión sincronizada
- **Asistentes reales**: nombre, correo, hora de entrada y salida de cada
  uno (más detallado que escribir "asistieron Juan y María" a mano).
- **Contenido**: según lo que la plataforma entregue — puede ser un resumen,
  una transcripción completa, o solo el enlace a la grabación.
- **Origen**: el acta queda marcada con la plataforma de la que vino (Teams,
  Google Meet, Zoom o Webex) — se distingue de las actas registradas a mano.

Una vez sincronizada, el acta se comporta exactamente igual que una
registrada manualmente: puedes correr el bot de minutas sobre su contenido
para generar compromisos (ver el manual de *Actas, reuniones y bot de
minutas*).

## Preguntas frecuentes
**¿Qué pasa si la misma reunión se sincroniza dos veces?**
El sistema la identifica por su identificador externo — no debería
duplicarse; si ves un acta repetida, repórtalo a soporte técnico.

**¿Necesito hacer algo especial en Teams/Zoom/Meet/Webex para que esto
funcione?**
No de tu lado como usuario — la conexión la configura el equipo técnico una
sola vez a nivel de la organización (credenciales de la plataforma, permisos
de la app). Ver el manual técnico de backend para el detalle.
