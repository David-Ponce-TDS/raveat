# U6 · Cámara, código QR y archivos

> Capacidades del dispositivo: permisos, fotografías, archivos y códigos QR.

| | |
|---|---|
| **Rama** | `version-06` |
| **Viene de** | `version-05` — autenticación, roles y biometría |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android — cámara, escáner y compartir, solo en el teléfono |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

La app **usa el teléfono**: vibración, foto de producto subida por multipart, comprobante del
pedido en PDF con su QR impreso, compartir con el sistema y escaneo del QR. El modelo sigue en
ocho entidades.

**El permiso es un flujo con final abierto, no una casilla.** El sistema puede decir que sí, que
no, o que no y no vuelvas a preguntar: la app tiene que seguir siendo usable en los tres casos.

---

## 🔄 Qué cambia desde la versión 5

| | `version-05` | `version-06` |
|---|---|---|
| **El teléfono** | biometría y almacenamiento seguro | + vibración, cámara, galería, compartir y escáner |
| **Foto del producto** | una ruta escrita a mano | el archivo, por **multipart**; la ruta la decide el servidor |
| **Comprobante** | no había | **PDF** generado en el servidor (QuestPDF) |
| **Código QR** | no había | lo genera el servidor (QRCoder), va impreso en el PDF y lo lee el teléfono (ML Kit) |
| **Permisos de Android** | `INTERNET` y los que suman los plugins | + `CAMERA`, pedido en tiempo de ejecución |
| **Modelo** | 8 entidades | 8, sin migraciones nuevas |

---

## 🧰 Qué hay que instalar

**Nada nuevo en la máquina**: los cinco plugins entran con `npm install`, y QuestPDF y QRCoder con
`dotnet build`. Lo que sí hace falta es **un teléfono con cámara** — ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Capacidades nativas y plugins: qué agrega Capacitor sobre la web
2. La vibración: la capacidad sin permiso ni diálogo
3. Permisos de Android: normales y peligrosos; consultar, pedir y aceptar el no
4. Cámara y galería: de la foto a un archivo que se puede subir
5. `multipart/form-data`: por qué un JSON no puede llevar una foto
6. El servidor decide la ruta: validación, nombre GUID y `wwwroot/uploads`
7. Binarios: el comprobante en PDF y el Blob del lado del cliente
8. Código QR de punta a punta: generar, imprimir, escanear
9. Compartir con el sistema: Filesystem y el diálogo de Share
10. Probar en el dispositivo, con un camino web para cada capacidad

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Qué hace ahora |
|---|---|
| **Productos** | el alta y la edición llevan **foto**: cámara o galería, con el permiso pedido en el momento |
| **Pedidos** | cada pedido tiene **Comprobante**: baja el PDF y lo comparte; **Escanear** lee el QR y abre ese pedido |
| **Mi cuenta** | el interruptor de **vibración**, junto al tema y al Diagnóstico |

---

## 🔑 El permiso no es una casilla

| | `VIBRATE` | `CAMERA` |
|---|---|---|
| Tipo | **normal** | **peligroso** |
| Cuándo se concede | al instalar, sin diálogo | en tiempo de ejecución: se declara **y** se pide |
| Puede decir que no | no | sí, y también «no volver a preguntar» |

El flujo está en `camara_service.js`: **consultar, pedir si hace falta, aceptar el no.** Denegado
es una respuesta posible y la pantalla la muestra como mensaje; denegado para siempre ya no muestra
diálogo, y la única salida honesta es mandar a los ajustes del teléfono. Diseñar para el «sí» y
tratar el «no» como error es el bug más común de estas capacidades.

---

## 📤 El header que no hay que poner

Con `FormData`, **el boundary del multipart lo arma el navegador**: ponerle `Content-Type` a mano
rompe la subida. En jQuery son dos banderas, `contentType: false` y `processData: false`.

Del otro lado, **la ruta de la imagen la decide el servidor**:

- el request manda el archivo, y no existe campo `imagen_url`;
- el nombre en disco es un **GUID**, porque el nombre original viene del cliente;
- editar sin adjuntar foto conserva la anterior;
- el multipart no pasa por la política `snake_case` del JSON: cada campo declara su nombre con
  `[FromForm(Name = "...")]`.

---

## 📱 Un camino web para cada capacidad

| Capacidad | En el teléfono | En el navegador |
|---|---|---|
| Vibración | `Haptics` | silencio: no hay motor de vibración |
| Foto | cámara o galería, con permiso | `<input type="file">` |
| Comprobante | se comparte con el diálogo del sistema | `navigator.share` o descarga directa |
| Escáner | lector de Google (ML Kit) | tipear el código en el buscador de Pedidos |

Probar solo con `npm run dev` deja la mitad de la versión sin probar; una pantalla que solo
funciona con plugin deja afuera el desarrollo diario.

---

## 🌐 La API

Lo que suma al contrato de la versión 5:

| Método | Ruta | Devuelve |
|---|---|---|
| `POST` `PUT` | `/api/productos[/{id}]` | ahora **multipart**, con `imagen` opcional (JPG, PNG o WEBP, hasta 5 MB) |
| `GET` | `/api/pedidos/{id}/comprobante` | el comprobante en **PDF** — binario, no JSON |
| `GET` | `/api/pedidos/{id}/qr` | el QR del pedido en **PNG** |

El QR lleva **texto plano**: el código del pedido (`PED-0001`), el mismo que va impreso debajo.
Escanearlo es buscar ese código. Las fotos subidas se guardan como ruta relativa
(`/uploads/productos/<guid>.jpg`) y el frontend la completa en `utils/imagenes.js`.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/                  el frontend, como en la versión 5, más:
│  ├─ android/                 + permiso CAMERA y el FileProvider de la cámara
│  └─ src/services/            + vibracion_service, camara_service, compartir_service, qr_service
└─ RavEat.Api/
   ├─ Controllers/             productos acepta multipart; pedidos suma /comprobante y /qr
   ├─ wwwroot/uploads/         las fotos subidas desde la app (ignoradas por git)
   └─ Program.cs               + la licencia de QuestPDF
```

---

## ✅ Terminaste cuando

- [ ] La vibración se activa y desactiva desde Mi cuenta, y al encenderla el teléfono vibra
- [ ] La foto de un producto se saca desde la app y **pide el permiso la primera vez**
- [ ] **Denegado a propósito**, la app muestra el mensaje y sigue andando; con «no volver a preguntar», manda a los ajustes
- [ ] La foto subida queda en `wwwroot/uploads/productos/` con nombre GUID y se ve en la carta
- [ ] El comprobante de un pedido se descarga en PDF y se comparte a otra app
- [ ] El QR del comprobante, escaneado desde otro teléfono (o impreso), abre el pedido correcto
- [ ] En el navegador, cada capacidad tiene su camino: archivo, descarga, buscador
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
