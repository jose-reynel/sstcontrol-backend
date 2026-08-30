# Manual de funcionalidad — Autenticación y sesión

## ¿Para qué sirve?
Controla quién entra al sistema y qué puede hacer una vez adentro. Toda la
aplicación (Web y móvil) exige haber iniciado sesión, excepto la propia
pantalla de acceso.

## Iniciar sesión
1. Abre la aplicación (web o móvil) — te recibe la pantalla de acceso.
2. Ingresa tu **usuario** y **contraseña** (te los entrega el Administrador
   SST al crear tu cuenta — ver el manual de *Administración y control de
   acceso*).
3. Si son correctos, entras directo al Panel. Si te equivocaste, verás el
   mensaje "Usuario o contraseña incorrectos" — puedes reintentar, pero el
   sistema **bloquea temporalmente** los intentos después de 5 fallos
   seguidos en un minuto (protección contra adivinar contraseñas).

## Qué ves después de entrar
La app te muestra solo lo que tu rol te permite:
- **Documentos** y **Empresas**: todo usuario autenticado los ve.
- **Actas**: solo si tu rol tiene el permiso de ver actas.
- **Administración** (Usuarios/Roles/Perfiles/Permisos/Grupos): solo si tu
  rol puede administrar el control de acceso — normalmente, solo el
  Administrador SST.

Ver el manual de *Administración y control de acceso* para el detalle exacto
de qué puede hacer cada rol (Administrador SST, Asesor SST, Auditor SST).

## Tu sesión no se corta de golpe
Si dejas la aplicación abierta varias horas, no tienes que volver a escribir
tu contraseña: el sistema renueva tu sesión automáticamente por detrás,
mientras la sigas usando. Solo te pedirá iniciar sesión de nuevo si:
- Estuviste sin usar la aplicación varias semanas seguidas.
- Cerraste sesión manualmente (ver abajo).
- Iniciaste sesión en otro dispositivo y el sistema detectó algo inusual con
  tu sesión anterior (medida de seguridad automática).

## Cerrar sesión
Botón **"Cerrar sesión"** en la barra superior. Esto invalida tu sesión en el
servidor — no solo en tu dispositivo — así que si alguien más tuviera acceso
a tu computador o celular, no podría seguir usando tu cuenta después de que
cierres sesión.

## Preguntas frecuentes
**¿Puedo estar conectado en el celular y en la computadora a la vez?**
Sí, cada dispositivo mantiene su propia sesión de forma independiente.

**Olvidé mi contraseña, ¿qué hago?**
Pide a un Administrador SST que verifique tu usuario — la recuperación de
contraseña por autoservicio no está disponible todavía (ver manual técnico
de backend, sección de pendientes).

**¿Por qué no veo el módulo de Actas / Administración?**
Porque tu rol no incluye ese permiso. Contacta a un Administrador SST si
crees que deberías tenerlo.
