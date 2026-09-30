# U8 · Offline, API publicada y distribución interna

> Datos guardados y cola de salida sin red, firma de release, API en producción y APK firmado en el teléfono.

| | |
|---|---|
| **Rama** | `version-08` |
| **Viene de** | `version-07` — ubicación y notificaciones |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android — el APK de release habla con la API publicada |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**La red se cae y la app tiene que seguir siendo usable.** El offline de esta versión son dos
piezas chicas: **la copia de lo último que se vio**, para seguir leyendo, y **una cola de salida**
con una sola acción, cambiar el estado de un pedido, que se envía sola cuando vuelve la conexión.
No hay base local ni fusión de conflictos.

Después viene el salto que cierra la app base: deja de ser un proyecto en una PC y pasa a ser una
**app firmada que habla con una API publicada**. El modelo sigue en
**diez entidades**: la API no suma endpoints, solo lo que hace falta para publicarla.

---

## 🔄 Qué cambia desde la versión 7

| | `version-07` | `version-08` |
|---|---|---|
| **Sin red, leer** | error con Reintentar | la carta, los pedidos y sus fichas muestran **lo último que se cargó** |
| **Sin red, trabajar** | error con Reintentar | el cambio de estado de un pedido **se encola** y se envía solo |
| **El estado de la red** | no se miraba | el plugin **Network**: lo informa el sistema |
| **El menú sin red** | siempre completo | quedan las pantallas que tienen sus datos guardados |
| **Mi cuenta** | con el diagnóstico de conexión | sin él: la app publicada ya no necesita mostrar a qué API le pide |
| **Firma** | `debug.keystore` automático | + el keystore de **release**, generado una vez |
| **Artefactos** | APK de debug | + el APK **firmado** de release |
| **Build del frontend** | `npm run build` | + `npm run build:release`: apaga el debug y apunta a producción |
| **La API** | solo local | + publicada, con los secretos en el panel del hosting |
| **Modelo** | 10 entidades | 10 — sin migraciones nuevas |

---

## 🧰 Qué hay que instalar

**Nada nuevo en la máquina.** El plugin `@capacitor/network` entra con `npm install` y `keytool`
vino con el JDK. El keystore **se genera, no se instala**. Ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. La copia local: la lectura marcada como `guardable` deja su respuesta en el teléfono
2. La cola de salida: el cambio de estado que no llegó se guarda y se envía solo al volver la red
3. Encolar no es un error: para el usuario el cambio quedó hecho
4. El cambio sin enviar en pantalla: el pedido muestra el estado nuevo, con su marca
5. El plugin Network: en el teléfono el estado de la red lo informa el sistema
6. El keystore: la identidad de la app — se genera una vez y se guarda para siempre
7. `keystore.properties`: la firma que el build usa solo si existe, igual que `google-services.json`
8. El APK firmado: `assembleRelease`, y cómo se comprueba la firma
9. Publicar la API: MonsterASP.NET, secretos como variables del panel y HTTPS
10. La ruta completa: build de release → APK firmado → instalado en el teléfono, contra la API publicada

---

## 🖥️ Qué se ve en pantalla

| Dónde | Qué hace ahora |
|---|---|
| **Carta y Pedidos** | sin red muestran lo último que se cargó, con el aviso de que son datos guardados |
| **Ficha del pedido** | sin red, «Pasar a…» cambia el estado en pantalla y lo deja con la marca **«Sin enviar»**; al volver la red se envía solo |
| **Lo que pide red** | sin conexión no se dibuja. En Pedidos: el buscador, los filtros, «Nuevo pedido» y «Escanear». En la ficha: el QR, los cobros, el comprobante y cancelar |
| **Menú, tabs y campana** | sin red quedan Inicio, Pedidos y Mi cuenta; Productos, Clientes, Entregas, Notificaciones y Usuarios desaparecen hasta que vuelve la red |
| **Al arrancar sin red** | la app abre con el usuario de la última vez, sin pasar por el login |

---

## 📮 Una bandera para leer y una cola para un cambio

**Leer sin red.** En `ajax_service.js` una lectura se guarda solo si el service que la arma lo pide:

```js
export function obtener_pedidos(opciones = {}){
	return ajax_request({endpoint: `/api/pedidos${query}`, metodo: 'GET', guardable: true});
}
```

| | Con red | Sin red |
|---|---|---|
| `guardable` | guarda la respuesta en el teléfono | devuelve la guardada |

Son `guardable` la carta, la lista de pedidos, la ficha de un pedido y «quién soy». Las pantallas
que viven de esas lecturas llevan `sin_red: true` en `navegacion.js`, y son las únicas que el menú
y los tabs muestran sin conexión.

**Cambiar sin red.** La cola vive en `offline_store.js` y guarda una sola cosa: cambios de estado,
como `{pedido_id, estado}`. El que decide es `pedidos_store.js`, a la vista:

```js
if(offline.sin_conexion){
	offline.encolar(id, estado);
	vm.pedidos = vm.pedidos.map(con_cambios_sin_enviar);
	return 'pendiente';
}
```

Se eligió esa escritura por un motivo: **mandarla dos veces no rompe nada**. Un pedido entregado no
puede pasar otra vez a entregado, así que la API rechaza la repetida.

| No se encola | Por qué |
|---|---|
| Crear un pedido | repetido, serían dos pedidos; y los precios los pone el servidor |
| Cobrar | repetido, serían dos cobros |
| El login | una sesión no se puede prometer: o hay token o no hay |
| La foto del producto | un `FormData` no sobrevive al `localStorage` |

Al volver la red, `sincronizar()` envía la cola en orden y distingue dos fallos. Sin red, con la
sesión vencida o con el servidor caído,
el cambio **sigue en la cola**. Si la API lo rechaza —otro usuario movió el pedido mientras el
teléfono estaba sin red—, reintentar no lo arregla: **sale de la cola y se avisa**.

Cuando el envío termina, si algo salió de la cola, `sincronizar()` **recarga la lista una sola
vez**: la marca «Sin enviar» desaparece y se ve lo que de verdad quedó.

---

## 🔑 Offline no es una base de datos local

La primera idea es replicar todo el modelo en el teléfono. El alcance de la versión es otro:
**seguir leyendo y no perder lo que el usuario hizo.** Un service y un store cortos sobre
`localStorage` lo resuelven; la base local con fusión de conflictos es un proyecto entero y quedó afuera.

| Clave de `localStorage` | Qué guarda |
|---|---|
| `raveat_copia:` + el endpoint | la última respuesta de cada lectura `guardable` |
| `raveat_cola_offline` | los cambios que esperan la red |

**Al cerrar la sesión se borran las dos.** Lo guardado es de quien lo cargó: nada pasa de un
usuario al siguiente, y un cambio nunca sale con la sesión de otro.

Lo que queda afuera, a propósito: las imágenes de la carta dependen de la caché del navegador, un
filtro o una búsqueda que no se hizo con red no tiene copia, y la app cerrada no envía nada. El
cambio se encola cuando el sistema informa que no hay red: con wifi y la API caída, muestra el
error de conexión de siempre.

---

## 🔏 La firma no es un trámite

El keystore es **la identidad de la app**: Google Play no acepta una actualización firmada con otra
identidad, así que **perder el keystore es perder la app**. De ahí las dos reglas: nunca se
versiona —quien lo tenga puede publicar en tu nombre— y siempre tiene respaldo fuera de la máquina.

`build.gradle` lee `keystore.properties` **solo si existe**, igual que `google-services.json`. Sin
el archivo el build termina y el artefacto sale sin firmar: **el build no depende de un secreto.**

---

## ☁️ Producción no es tu PC con otra URL

| | Desarrollo | Producción |
|---|---|---|
| Cadena de conexión y clave JWT | User Secrets | variables del panel — el repositorio nunca las tuvo |
| Contraseña del administrador | la del taller, que es pública | `Admin__Password`, del panel: sin ella la API no arranca |
| CORS | los orígenes de desarrollo | lista explícita: `http://localhost` es el origen del APK, y `http://localhost:5173` la web de tu PC |
| HTTP sin cifrar | no importa: es la red local | redirigido a HTTPS desde el panel |
| URL de la API en el APK | `.env` local | la de `src/config/debug.js`, elegida por `.env.release` |
| Migraciones | al arrancar la API | al arrancar la API |

El build de release **no lee el `.env` de la máquina**: `.env.release` está versionado, apaga el
modo debug y vacía las dos URL, para que la IP de la red local no quede escrita dentro del APK.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/
│  ├─ src/services/
│  │  ├─ copia_local_service.js    la última respuesta de cada lectura guardable
│  │  └─ ajax_service.js           + la bandera guardable
│  ├─ src/config/navegacion.js     + sin_red: las pantallas que quedan en el menú sin conexión
│  ├─ src/stores/offline_store.js  el estado de la red, la cola y su envío
│  ├─ .env.release                 versionado: define el build que se firma
│  └─ android/
│     ├─ keystore.properties.example    la plantilla; el real no se versiona
│     └─ app/build.gradle               lee la firma solo si existe
└─ RavEat.Api/                     sin endpoints nuevos: suma Admin__Password y se publica
```

---

## ✅ Terminaste cuando

- [ ] En modo avión, Pedidos muestra la última lista cargada, con el aviso de datos guardados
- [ ] En modo avión, «Pasar a…» deja el pedido con el estado nuevo y la marca «Sin enviar»
- [ ] Al volver la red el cambio se envía **solo** y la marca desaparece
- [ ] En modo avión, el menú y los tabs muestran solo Inicio, Pedidos y Mi cuenta
- [ ] Al cerrar la sesión no queda ninguna clave `raveat_copia:` ni nada en la cola
- [ ] El keystore de release existe, con respaldo **fuera de la máquina**
- [ ] `.\gradlew assembleRelease` genera un APK **firmado**
- [ ] Ese APK instalado habla con la API publicada, sin `.env` de por medio
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

Es el **hito de la cadena**: la app base terminada y firmada.

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
