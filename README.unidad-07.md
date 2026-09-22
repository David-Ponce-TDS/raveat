# U7 · Ubicación y notificaciones

> Geolocalización y notificaciones: bandeja interna, alertas locales y mensajes push.

| | |
|---|---|
| **Rama** | `version-07` |
| **Viene de** | `version-06` — cámara, código QR y archivos |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android — el push, **solo en el APK y con Firebase** |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**Tres cosas distintas se llaman «notificación»**, y confundirlas es creer que hay una sola
herramienta donde hay tres, cada una con su dueño, su alcance y su costo. La **local** la programa
la propia app; la **bandeja** es una tabla en la API con su campanita; el **push** viaja por
Firebase.

La ubicación entra por la puerta del negocio: **el reparto**. El pedido de tipo *entregas* crea una
`Entrega` que copia dirección y coordenadas del cliente, capturadas con el GPS. El modelo suma
`Entrega` y `Notificacion`: **diez entidades**. Todavía no hay offline, firma ni publicación.

---

## 🔄 Qué cambia desde la versión 6

| | `version-06` | `version-07` |
|---|---|---|
| **Modelo** | 8 entidades | 10: + `Entrega`, `Notificacion` |
| **Notificaciones** | no había | las tres: local, bandeja con campanita, push |
| **Ubicación** | no había | GPS con permiso, y el mapa **externo**: la app de mapas del teléfono |
| **Reparto** | no había | Entregas: tomar, asignar y mover estados |
| **Firebase** | no hacía falta | proyecto propio con `google-services.json` — solo para el push |
| **Permisos** | + `CAMERA` | + ubicación (fina y aproximada), notificaciones |

---

## 🧰 Qué hay que instalar

**Nada nuevo en la máquina**: los tres plugins entran con `npm install`. Lo nuevo es una **cuenta
de Firebase** para el push — gratis, y solo para el push. Ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Las tres notificaciones: local (la app), bandeja (la API) y push (Firebase)
2. La local: programarla como respuesta a un evento — funciona hasta en modo avión
3. La bandeja: una tabla más, dirigida al interesado y escrita en los cambios de estado
4. La bandeja es siempre la del token: leer la de otro tiene que ser imposible
5. La campanita: el contador sale del resumen del servidor, nunca de contar filas
6. El push necesita un intermediario: registrarse en FCM y enviar desde la consola
7. `google-services.json`: la credencial que no se versiona y el build que no depende de ella
8. Entregas: el pedido crea la entrega y copia dirección y coordenadas del cliente
9. Ubicación: capturar coordenadas con el GPS — el permiso puede decir que no, y se sigue
10. El mapa externo: abrir la app de mapas del teléfono ya resuelve la clase

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Qué hace ahora |
|---|---|
| **Notificaciones** | nueva: la bandeja del usuario, con el contador en la campanita del header |
| **Entregas** | nueva: las entregas por estado, tomarlas, asignarlas y moverlas |
| **Clientes** | el formulario captura coordenadas con el GPS — «usar mi ubicación» |
| **Pedidos** | el pedido de tipo *entregas* crea su entrega al confirmarse |

---

## 🔔 Push no es «la» notificación

El error de partida es creer que notificación = push. Ordenarlas **por costo** las acomoda:

| | Local | Bandeja | Push |
|---|---|---|---|
| Quién la produce | la propia app | la API, en el mismo `SaveChanges` | Firebase (FCM) |
| Qué necesita | nada — funciona en modo avión | una tabla y un endpoint | proyecto, credencial y permiso |
| Sobrevive a cambiar de teléfono | no | **sí** — es una tabla | no |
| Llega con la app cerrada | no | no | **sí** — es la única |

Cada una existe porque las otras dos no llegan ahí. El envío del push se hace **desde la consola de
Firebase**, no desde la API: el envío programático (tokens por usuario, colas) excede la semana.

---

## 📬 La bandeja se escribe para el interesado

La tentación es notificar todo a todos — o peor, al que hizo la acción, que ya lo sabe porque la
hizo. La regla que ordena el módulo: **cada evento tiene UN interesado** (el creador del pedido, el
repartidor asignado) y la notificación se escribe **en el mismo `SaveChanges` que la acción** — o
pasan las dos cosas, o ninguna.

Y la bandeja es **siempre la del token**: el endpoint no acepta un `usuario_id`, porque con un id
en la query cualquiera leería la bandeja de cualquiera. El contador de la campanita sale de
`resumen.no_leidas`, calculado por el servidor sobre la bandeja completa — contar las filas
descargadas mentiría apenas haya más de una página.

---

## 📍 Las coordenadas se capturan, no se tipean

Nadie sabe su latitud. El formulario captura la posición **estando ahí** o no guarda nada — y sin
coordenadas, el mapa busca por la dirección escrita, que es exactamente lo que hace una persona.
Diseñar ese respaldo vale más que el GPS: **el permiso puede decir que no, y la app sigue.**

El mapa es **externo a propósito**: abrir la app de mapas del teléfono con las coordenadas ya
resuelve la clase. El mapa embebido, dibujado por nosotros, es el tema de la versión 10.

---

## 🌐 La API

Lo que suma esta versión al contrato de la 6:

| Método | Ruta | Devuelve |
|---|---|---|
| `GET` | `/api/entregas` | `{entregas, pagina, resumen, total}` — con `estados` (CSV) y `solo_mias` |
| `PUT` | `/api/entregas/{id}/asignar` | sin `repartidor_id` la toma el que llama; el asignado debe ser delivery |
| `PUT` | `/api/entregas/{id}/estado` | la mueve su repartidor; transición validada en el diccionario |
| `GET` | `/api/notificaciones` | `{resumen, notificaciones, pagina}` — **siempre la bandeja del token** |
| `PUT` | `/api/notificaciones/leer` | marca leídas: una con `notificacion_id`, todas sin él |

El resto del contrato sigue el de la versión 6.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/                  el frontend, como en la versión 6, más:
│  └─ src/
│     ├─ services/             + geolocalizacion, notificaciones (bandeja) y notificaciones nativas
│     ├─ stores/               + entregas_store, notificaciones_store
│     └─ views/                + entregas_page, notificaciones_page
└─ RavEat.Api/
   ├─ Controllers/             + EntregasController, NotificacionesController
   ├─ Domain/Entities/         + Entrega, Notificacion — 10 entidades
   └─ Migrations/              + reparto: entregas y notificaciones
```

---

## ✅ Terminaste cuando

- [ ] Crear un pedido dispara la **notificación local** en el momento
- [ ] Una acción de un usuario suma a la **campanita del interesado** — no a la del que la hizo
- [ ] La bandeja pagina, y «marcar todas como leídas» pone el contador en cero
- [ ] El formulario de cliente captura coordenadas con el GPS, pidiendo el permiso — y con el permiso denegado, la app sigue
- [ ] La entrega se abre en la app de mapas del teléfono con las coordenadas del cliente
- [ ] Con Firebase configurado, un push enviado desde la consola **llega con la app cerrada**
- [ ] Sin `google-services.json`, todo lo demás sigue funcionando
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
