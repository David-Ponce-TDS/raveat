# U12 · Identidad, acceso público y publicación

> La app frente a alguien de afuera: uno entra con una cuenta que no es nuestra, otro mira sin
> ninguna cuenta, y la tienda es la puerta por la que llegan los dos.

| | |
|---|---|
| **Rama** | `version-12` — el último corte de la cadena |
| **Viene de** | `version-11` — preparación, historial y firma |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android, y la página pública del pedido en cualquier navegador **sin sesión** |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**Que la app pueda mostrarse a alguien que no tiene cuenta en ella.** Hasta la versión 11 todo
pasaba detrás del login: la única pregunta era *qué rol hace falta*. En una pantalla anónima la
pregunta es otra: **qué se puede mostrar sin saber quién mira**.

Entran el ingreso con Google, el token público del pedido con su QR, la página pública con App
Links verificados, la carta en la raíz del sitio, el almacén de fotos fuera del servidor y lo que
pide Google Play para publicar. Es el cierre de la cadena: **la app publicada**.

---

## 🔄 Qué cambia desde la versión 11

| | `version-11` | `version-12` |
|---|---|---|
| **Entrar** | email y contraseña (+ huella) | + **con Google**: el `id_token` validado en el servidor, contra la audiencia |
| **Mirar sin cuenta** | la vitrina de la app | + la **página pública** del pedido, `/p/{token}`, y la carta en la raíz del sitio |
| **El QR** | llevaba el código del pedido | lleva una **URL `https`**: la abre cualquier cámara |
| **Identificadores del pedido** | `PED-0042`, correlativo | + `TokenPublico`: 16 bytes al azar, índice único |
| **Abrir la app desde afuera** | no se podía | **App Links verificados** por dominio |
| **Las fotos de producto** | `wwwroot/uploads` | + Supabase Storage, si está configurado |
| **El APK de release** | permitía HTTP a cualquier destino | solo HTTPS; el HTTP de la PC queda en el build de debug |
| **La tienda** | APK firmado | + el AAB, la política de privacidad y la ficha de Google Play |
| **La base** | — | una migración: `TokenPublicoPedido` |

---

## 🧰 Qué hay que instalar

**Nada nuevo en la máquina.** Lo nuevo son credenciales: **dos clientes OAuth de Google** —uno web,
uno Android— y, opcional, un proyecto de Supabase. El único paquete nuevo de la app es
`@capacitor/app`, que entra con `npm install`. Ver [`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Autenticación con proveedor externo: confiar en la identidad que emite un tercero
2. El `id_token` se valida en el servidor **y contra la audiencia**, no solo la firma
3. Google autentica pero no habilita: el alta la sigue haciendo un administrador
4. Plugins nativos propios: cuando la capacidad no viene en un paquete y hay que escribirla
5. Un identificador adivinable en una URL sin login es un agujero (IDOR)
6. `TokenPublico`: 16 bytes al azar, índice único y comparación completa, nunca por prefijo
7. El recorte anónimo vive en un `record` propio, no se reusa el del detalle interno
8. El QR lleva una **URL `https`**, no un texto suelto: lo abre cualquier cámara
9. App Links verificados: la misma dirección abre la app o el navegador
10. Los archivos fuera del servidor: Supabase Storage, con caída al disco
11. Publicar: HTTPS obligatorio en el release, política de privacidad, AAB firmado y prueba en Play

---

## 🖥️ Qué se ve en pantalla

| Dónde | Qué hace ahora |
|---|---|
| **Login** | + **Entrar con Google**: en el teléfono la pantalla de cuentas del sistema; en el navegador, el botón de Google |
| **La página pública** | `/p/{token}`: el pedido para el comensal, sin login — estado, ítems y total |
| **El QR del pedido** | escaneado con la cámara del teléfono, abre la página pública, o la app si está instalada |
| **Pedidos → escanear** | el QR nuevo se traduce al código del pedido y lo busca, igual que antes |
| **Compartir el pedido** | + el enlace público, al final del texto |
| **La raíz del sitio** | la carta pública, con el panel para bajar la app |

Para el comensal, el pedido en *borrador* se presenta como **«Tomado»**: «borrador» es lenguaje
del mostrador, y la vista pública lo traduce.

---

## 🔑 Verificar la firma no alcanza: hay que verificar para quién

Un `id_token` de Google es válido aunque haya sido emitido para otra aplicación. Sin comparar la
**audiencia** contra el web client id propio, cualquier token legítimo del mundo entra. Es el
control que casi siempre falta.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `GoogleAuthPlugin.java` | `android/.../app/raveat/` | el plugin propio: la pantalla de cuentas del sistema y `requestIdToken` |
| `registerPlugin` | `MainActivity.java` | se registra **antes** de `super.onCreate`: ahí nace el bridge |
| `google_auth_service.js` | `src/services/` | el plugin en el teléfono, Google Identity Services en el navegador |
| `POST /api/sesion/login-google` | `SesionController` | `ValidateAsync` con `Audience`, email verificado, y la sesión propia de siempre |

Y **Google autentica pero no habilita**: si el email no está dado de alta, la API responde 403 y no
crea nada. Quién opera el local lo sigue decidiendo un administrador, la misma regla desde la
versión 5. Después de un rechazo la app suelta la cuenta elegida: si no, Google la recuerda y el
siguiente intento repite el mismo error sin dejar elegir otra.

---

## 🎲 `PED-0042` se recorre sumando uno

La lección de seguridad más concreta del taller: el **código** lo lee una persona en el mostrador y
por eso es correlativo; el **token** lo lee una cámara y por eso es aleatorio. Son dos
identificadores del mismo pedido, con públicos distintos. Publicar el correlativo en una URL sin
login es un agujero con nombre: **IDOR**.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `TokenPublico` | `Pedido` | `RandomNumberGenerator`, 16 bytes en hexadecimal, índice único |
| `TokenPublicoPedido` | `Migrations/` | `RANDOM_BYTES` de MySQL para los pedidos que ya existían |
| `PedidoPublicoResponse` | `PublicoController` | el recorte anónimo: ni cliente, ni teléfono, ni quién lo cargó |
| `/p/{token}` | `PublicoController` | la página HTML para el navegador; 410 si el pedido se canceló |
| `/p/:token` | `router/index.js` | la misma dirección en la app: ruta **abierta**, con sesión o sin ella |

El recorte anónimo vive en un **`record` propio**: si la vista pública devolviera el mismo DTO que
el detalle interno, agregarle mañana el teléfono del cliente lo pondría en internet sin que nadie
lo note. El tipo aparte es la garantía de que publicar es una decisión, no un accidente.

---

## 🔗 Una dirección, dos destinos

El QR lleva `https://raveat.runasp.net/p/<token>`. Con la app instalada y el dominio verificado,
Android la abre en la app; sin la app, la abre el navegador con la página que sirve la API. Los dos
caminos terminan mostrando el mismo pedido.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `intent-filter android:autoVerify="true"` | `AndroidManifest.xml` | declara el host y el camino `/p/` |
| `/.well-known/assetlinks.json` | `PublicoController` | la huella del certificado que Android compara al instalar |
| `deep_links_service.js` | `src/services/` | `appUrlOpen` y `getLaunchUrl`: el enlace se vuelve una ruta |

---

## 📦 La firma con la que se sube no es la firma con la que se instala

Con *Play App Signing*, Google **refirma** el paquete: la huella que verifica los App Links y la que
habilita el ingreso con Google son **las de Play**, no las del keystore local. Anda perfecto con el
APK propio y falla justo en el que instala el tester: el error más desorientador de la semana, y
por eso está acá y no en un pie de página.

El release además deja de hablar HTTP: `network_security_config.xml` lo niega salvo `localhost`, y
el permiso amplio vive en `src/debug/`, que solo compila en el build de debug. Es lo que hace
verdadera la declaración de «Seguridad de los datos» de la ficha.

---

## 🌐 La API

Lo que suma esta versión al contrato de la 11:

| Método | Ruta | Devuelve |
|---|---|---|
| `POST` | `/api/sesion/login-google` | `{sesion}` — con `id_token`; 403 si el email no tiene alta |
| `GET` | `/api/publico/pedidos/{token}` | el pedido anónimo; 404 si no existe, 410 si se canceló |
| `GET` | `/p/{token}` | la página HTML del pedido |
| `GET` | `/.well-known/assetlinks.json` | las huellas de `Publico:HuellasSha256` |
| `GET` | `/api/publico/qr-descarga` | el QR que lleva a la ficha de Play |
| `GET` | `/` | la carta pública (`wwwroot/index.html`) |

El detalle del pedido suma `enlace_publico`, y su QR lleva esa URL. El token `ejemplo` muestra la
página con un pedido de muestra, sin crear uno.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEat.Api/
│  ├─ Controllers/SesionController.cs           + login-google
│  ├─ Controllers/PublicoController.cs          el pedido sin login, la página, assetlinks y el QR de descarga
│  ├─ Data/AlmacenArchivos.cs                   Supabase o el disco, según la configuración
│  ├─ wwwroot/index.html                        la carta pública
│  ├─ wwwroot/distribucion.json                 los caminos de descarga: se editan sin recompilar
│  ├─ wwwroot/privacidad.html                   la política que pide Play
│  └─ Migrations/                               + TokenPublicoPedido
└─ RavEatApp/
   ├─ android/app/src/main/java/app/raveat/GoogleAuthPlugin.java   el plugin propio
   ├─ android/app/src/debug/res/xml/            el HTTP de la PC, solo en debug
   └─ src/
      ├─ services/google_auth_service.js        Google en el teléfono y en el navegador
      ├─ services/deep_links_service.js         del enlace a la ruta
      └─ views/pedido_publico_page.vue          /p/:token dentro de la app
```

---

## ✅ Terminaste cuando

- [ ] **Entrar con Google** abre la pantalla de cuentas y entra con un email dado de alta
- [ ] Con un email sin alta, la API responde 403 y el botón ofrece **Probar con otra cuenta**
- [ ] Un `id_token` de otra aplicación es rechazado: la audiencia no coincide
- [ ] El QR de un pedido, escaneado con la cámara del teléfono, abre la página pública
- [ ] `/p/{token}` muestra el pedido sin sesión, y un token inventado no muestra otro pedido
- [ ] La vista pública no expone teléfono ni datos internos: es el `record` anónimo
- [ ] Con la app instalada y la huella publicada, el enlace abre la app; sin ella, el navegador
- [ ] Con Supabase configurado, la foto nueva de un producto queda en el bucket
- [ ] El AAB firmado sube a la prueba de Play con su ficha y su política de privacidad
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

Es el cierre de la cadena: **la app publicada.**

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
