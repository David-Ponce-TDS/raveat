# Comandos · versión 12

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-12.md`](README.unidad-12.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

Esta versión **no suma paquetes**: la firma es un `<canvas>` del navegador y lo demás son pantallas
y endpoints sobre lo que ya estaba.

## 2 · Crear la base y el usuario

Solo si no venís de una versión anterior con la base ya creada. Desde `mysql -u root -p`:

```sql
CREATE DATABASE raveat_app CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
CREATE USER 'raveat_app'@'localhost' IDENTIFIED BY 'TU_CLAVE';
GRANT ALL PRIVILEGES ON raveat_app.* TO 'raveat_app'@'localhost';
FLUSH PRIVILEGES;
```

La contraseña va en User Secrets, nunca en `appsettings.json`:

```powershell
Set-Location .\RavEat.Api
dotnet user-secrets list
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=raveat_app;User=raveat_app;Password=TU_CLAVE;"
Set-Location ..
```

## 3 · El esquema

Esta versión **suma tres migraciones**: `Comandas` (el estado de cada ítem), `Historial` (la tabla
`historiales`) y `FirmaEntrega` (la columna `firma_url`). La API las aplica sola cada vez que
arranca. Para inspeccionar o reconstruir desde cero:

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- migrations list
dotnet tool run dotnet-ef -- database drop --force
dotnet tool run dotnet-ef -- database update
Set-Location ..
```

## 4 · La URL de la API en el frontend

```powershell
Set-Location .\RavEatApp
if (Test-Path .\.env) { '.env ya existe' } else { Copy-Item .\.env.example .\.env }
Set-Location ..
```

Editá `.env` con la IP de tu máquina (`ipconfig`): `VITE_API_URL_DEBUG=http://TU_IP_LAN:5080`.
Para trabajar solo en el navegador: `VITE_DEBUG_ACTIVADO=false`.

Ese archivo vale para desarrollo. El build de release no lo lee: usa `.env.release` (sección 9).

## 5 · Correr

Dos terminales:

```powershell
Set-Location .\RavEat.Api
dotnet run
```

```powershell
Set-Location .\RavEatApp
npm run dev
```

| Servicio | URL |
|---|---|
| API | `http://localhost:5080` |
| Health | `http://localhost:5080/health` |
| App (Vite) | `http://localhost:5173` |

### Ver el esqueleto y el scroll infinito

Con la API en la misma máquina los datos llegan antes de que el esqueleto se alcance a ver. Para
verlo, en el navegador: DevTools → Network → **Slow 3G**, y recargar cualquier lista. El scroll
infinito se ve con más de veinte pedidos: al llegar al pie aparece «Cargando más...» y la lista
sigue; en la última página el sensor se apaga y queda «Mostrando N de N».

### Volver a empezar

El administrador tiene en **Mi cuenta → Herramientas de prueba** el botón que borra todos los
pedidos, sus ítems, entregas, el historial, las notificaciones y las firmas guardadas, y deja la
numeración en `PED-0001`. La carta, los
clientes y los usuarios no se tocan. Pide escribir `BORRAR`.

## 6 · Probar el offline sin salir a la calle

| Dónde | Cómo se corta la red |
|---|---|
| Teléfono | modo avión |
| Navegador | DevTools → Network → **Offline** |

1. Con red, entrar a **Pedidos** y abrir la ficha de un pedido: eso deja las copias guardadas.
2. Cortar la red.
3. Recargar **Pedidos**: muestra la misma lista, con el aviso de datos guardados.
4. Abrir la ficha y tocar **Pasar a…**: el pedido queda con el estado nuevo y la marca «Sin enviar».
5. Mirar la pantalla: no están el buscador, los filtros, **Nuevo pedido** ni **Escanear**, y en
   la ficha queda una sola acción, **Pasar a…**.
6. Volver la red: el cambio se envía solo y la marca desaparece.

Las copias y la cola se miran desde la consola del navegador:

```js
Object.keys(localStorage).filter(clave => clave.startsWith('raveat_copia:'))
JSON.parse(localStorage.getItem('raveat_cola_offline'))
```

## 7 · Compilar y llevar el debug al teléfono

```powershell
Set-Location .\RavEatApp
npm run lint
npm run build
npx cap sync android

Set-Location .\android
.\gradlew installDebug
Set-Location ..\..

dotnet build .\RavEat.Api\RavEat.Api.csproj
```

## 8 · La firma, una sola vez

Primero, si ya hay keystore: no se genera dos veces.

```powershell
Set-Location .\RavEatApp\android
if (Test-Path .\raveat-release.jks) { 'keystore          ok' } else { 'keystore          FALTA' }
if (Test-Path .\keystore.properties) { 'keystore.properties ok' } else { 'keystore.properties FALTA' }
```

Si falta, se genera con `keytool`, que viene con el JDK de Android Studio. Pide las contraseñas y
los datos del titular por teclado:

```powershell
keytool -genkeypair -v -keystore raveat-release.jks -alias raveat -keyalg RSA -keysize 2048 -validity 10000
Copy-Item .\keystore.properties.example .\keystore.properties
notepad .\keystore.properties
```

Si `keytool` no figura en el `PATH`, se llama con la ruta completa:

```powershell
& "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe" -genkeypair -v -keystore raveat-release.jks -alias raveat -keyalg RSA -keysize 2048 -validity 10000
```

En un proyecto propio cambian dos valores: el nombre del archivo y el alias.

```powershell
keytool -genkeypair -v -keystore MI_APP-release.jks -alias MI_ALIAS -keyalg RSA -keysize 2048 -validity 10000
```

Dos reglas:

- **El keystore y sus contraseñas nunca se versionan.** `.gitignore` corta `*.jks`, `*.keystore` y
  `keystore.properties`.
- **El keystore se respalda fuera de la máquina**, el mismo día. Perderlo es perder la posibilidad
  de actualizar la app publicada.

Para ver la huella del keystore, que después piden Firebase y Google Play:

```powershell
keytool -list -v -keystore .\raveat-release.jks -alias raveat
```

## 9 · El APK de release

```powershell
Set-Location .\RavEatApp
npm run build:release
npx cap sync android

Set-Location .\android
.\gradlew assembleRelease
```

El APK firmado queda en `app\build\outputs\apk\release\app-release.apk`. El AAB, que es el
formato que pide Google Play, está en la sección 14.

`build:release` compila con `--mode release`: `.env.release`, que está versionado, apaga el modo
debug y vacía las dos URL. La app queda apuntando a la URL de producción de
`src/config/debug.js`. En un proyecto propio esa constante se cambia por la dirección de tu API.

Sin `keystore.properties` el comando termina igual y el APK sale como
`app-release-unsigned.apk`, que el teléfono no instala.

Para comprobar que el APK quedó firmado — `apksigner` viene con las build-tools del SDK:

```powershell
$apksigner = Get-ChildItem "$env:ANDROID_HOME\build-tools" -Recurse -Filter apksigner.bat | Select-Object -Last 1
& $apksigner.FullName verify --print-certs .\app\build\outputs\apk\release\app-release.apk
```

Debug y release firman distinto: para instalar uno sobre el otro hay que desinstalar antes.

```powershell
adb uninstall app.raveat
adb install .\app\build\outputs\apk\release\app-release.apk
```

## 10 · Publicar la API en MonsterASP.NET

La cuenta es gratuita. Todo se hace desde el panel web, salvo el paquete, que se arma en la PC.

**En el panel, una vez:**

1. **Databases → MySQL**: crear la base. Anotar servidor, base, usuario y contraseña.
2. **Create new service → Website**: plan **Free**, **Free subdomain** con el nombre del sitio
   —bajo `.runasp.net` o `.tryasp.net`— y **My own application**. El asistente no pregunta la
   versión de .NET: el sitio ya corre .NET 10.
3. **Scripting → Environment variables** del sitio, y **Restart application** después de cargarlas:

| Variable | Valor |
|---|---|
| `ConnectionStrings__Default` | `Server=dbXXXXX.databaseasp.net;Port=3306;Database=BASE;User=USUARIO;Password=CLAVE;` |
| `Jwt__Key` | una clave larga y al azar, generada con el bloque de abajo |
| `Admin__Password` | la contraseña del administrador en producción, de 8 caracteres o más |
| `UsuariosDePrueba__Password` | la de los cuatro usuarios de prueba (vendedor, proceso, caja y delivery) en producción, de 8 caracteres o más |
| `Cors__AllowedOrigins__0` | `http://localhost` — el origen del APK |
| `Cors__AllowedOrigins__1` | `https://localhost` — el origen del APK si Capacitor usa su esquema por defecto |
| `Cors__AllowedOrigins__2` | `http://localhost:5173` — el navegador de tu PC, para probar la web contra la API publicada |
| `Firebase__CuentaDeServicio` | `firebase-cuenta.json` — la push automática, sección 11. Opcional: sin ella la API anda y no manda push |

La clave de `Jwt__Key` sale de este bloque, que la deja en el portapapeles sin mostrarla: se pega
directo en el panel.

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes) | Set-Clipboard
```

La contraseña de la base se copia en **Databases → la base → Password**, con el ícono de copiar y
sin apretar **Show**. El panel oculta en la lista la cadena y la contraseña, pero **muestra
`Jwt__Key` entera**: esa lista no se captura ni se comparte en pantalla.

La contraseña del taller es pública: está en este repositorio. Publicada, la API no arranca sin
`Admin__Password` ni `UsuariosDePrueba__Password`, y al arrancar se las pone a `admin@raveat.local`
y a los cuatro usuarios de prueba en lugar de la del taller. En la PC no cambia nada.

4. **HTTPS → Enable HTTPS**: emite el certificado de Let's Encrypt. Es un paso aparte de crear el
   sitio: sin emitirlo, HTTP anda y HTTPS no. Con el certificado emitido, en la misma pantalla,
   **Force redirect to HTTPS → Enabled**: nadie manda su contraseña sin cifrar.

**En la PC, cada vez que se publica: armar el paquete.** Son tres líneas, desde la carpeta del
repositorio. Antes, frenar la API local: si está corriendo, el `publish` falla con MSB3026 por
archivos bloqueados.

```powershell
$salida = "$env:USERPROFILE\Documents\raveat"
dotnet publish .\RavEat.Api\RavEat.Api.csproj -c Release -o "$salida\publish-api"
Compress-Archive -Path "$salida\publish-api\*" -DestinationPath "$salida\raveat-api.zip" -Force
```

| Línea | Qué hace |
|---|---|
| `$salida` | la carpeta donde queda todo: en Documentos, **fuera del repositorio** |
| `dotnet publish` | compila la API y deja en `publish-api` lo que el servidor necesita: los `.dll`, `web.config` y las imágenes de la carta |
| `Compress-Archive` | arma `raveat-api.zip` con eso, que es el archivo que se sube |

El `\*` comprime el contenido de la carpeta y no la carpeta: sin él, `web.config` queda un nivel
más adentro y el sitio responde 403. Las fotos que subiste probando en tu PC (`wwwroot\uploads`)
no entran al paquete: el proyecto las deja afuera.

5. **Files → `wwwroot` → Upload file**: subir el zip, **descomprimirlo** ahí con sobrescritura, y
   **Scripting → Restart application**.
6. Probar, cambiando la dirección por la de tu sitio:

```powershell
Invoke-RestMethod https://TU_SITIO.runasp.net/health
Invoke-RestMethod https://TU_SITIO.runasp.net/api/categorias | ConvertTo-Json -Depth 4
```

La API aplica las migraciones al arrancar, con la cadena del panel: las tablas y los datos de
ejemplo quedan creados la primera vez que el sitio responde. `appsettings.json` viaja con
`Password=CAMBIAR`; la cadena real, la clave JWT y la contraseña del administrador viven en las
variables del panel, que la sobrescritura no toca.

## 11 · La push automática: la cuenta de servicio de Firebase

Cada aviso que entra a la bandeja sale también como push, y lo manda la API. Para pedírselo a FCM,
la API se identifica con la **cuenta de servicio** del proyecto: un JSON con clave privada, que no
es el `google-services.json` de la app. Ese es público y va adentro del APK; este es un secreto y
no sale de tu PC ni del servidor.

**Una vez: bajar la clave.** En la consola de Firebase, **Configuración del proyecto → Cuentas de
servicio → Generar nueva clave privada**. Guardarla fuera del repositorio, en la misma carpeta del
paquete de la sección 10:

```powershell
$salida = "$env:USERPROFILE\Documents\raveat"
New-Item -ItemType Directory -Force $salida | Out-Null
Move-Item "$env:USERPROFILE\Downloads\*-firebase-adminsdk-*.json" "$salida\firebase-cuenta.json"
```

**En la PC: decirle a la API dónde está.** Va en User Secrets, como la cadena de conexión:

```powershell
dotnet user-secrets set "Firebase:CuentaDeServicio" "$env:USERPROFILE\Documents\raveat\firebase-cuenta.json" --project .\RavEat.Api
```

Al levantar, la consola de la API no tiene que decir `Push apagado`. Si lo dice, el secreto no
está o tiene otro nombre.

**En MonsterASP.NET: el mismo archivo, al lado de la API.** **Files → `wwwroot` → Upload file**
con `firebase-cuenta.json`, la variable `Firebase__CuentaDeServicio` con ese nombre (sección 10) y
**Restart application**. Es el mismo lugar del zip, pero no va adentro: el paquete se arma desde el
repositorio y el JSON nunca entra al repositorio.

**Probarlo con dos sesiones.** La migración `UsuariosDePrueba` deja un usuario por rol, todos con
la clave del taller `RavEat123!`, la misma del administrador. Publicada la API, la reemplaza
`UsuariosDePrueba__Password` (sección 10):

| Usuario | Rol |
|---|---|
| `vendedor@raveat.local` | Vendedor |
| `proceso@raveat.local` | Proceso |
| `caja@raveat.local` | Caja |
| `delivery@raveat.local` | Delivery |

El teléfono entra como `vendedor@raveat.local` y crea un pedido; el navegador entra como
`proceso@raveat.local` y le cambia el estado. Con la app cerrada, al teléfono le llega la push como
banner flotante; con la app abierta, aparece el mismo aviso y la campanita sube. Tocarla abre el
pedido.
Con varios avisos juntos Android los agrupa: el resumen del grupo abre la app, cada aviso abre su
pedido.

```powershell
# Que el telefono quedo anotado: una fila por telefono con sesion abierta.
mysql -u raveat_app -p raveat_app -e "SELECT usuario_id, LEFT(token, 20) AS token, actualizado_en FROM dispositivos_push;"
```

## 12 · El mapa y el dictado

**El mapa.** En **Clientes**, editar uno y tocar **Marcar en el mapa**. El buscador arranca con la
dirección escrita: enviarla y tocar un resultado acerca el mapa. Arrastrar hasta que la punta del
pin quede en la puerta y **Usar este punto**: la dirección pasa a ser la del punto. En un pedido de **Entrega** con cliente, el mismo
botón aparece en el destino y guarda el punto en la ficha.

**Sin mapa.** DevTools → Network → **Offline** y abrir el mapa: dice que no cargó, y el formulario
se guarda igual con la dirección escrita.

**El dictado.** El micrófono está en la referencia del cliente, en las observaciones del pedido y
en la descripción del producto. En el teléfono, el primer toque pide el permiso; el icono late
mientras escucha y se corta solo con el silencio.

Para ver qué servicio de voz tiene el teléfono, y para sacarle el permiso y probar el camino del
«no»:

```powershell
adb shell cmd package query-services -a android.speech.RecognitionService
adb shell pm revoke app.raveat android.permission.RECORD_AUDIO
```

## 13 · La preparación, el historial y la firma

**La preparación.** Con dos sesiones —el administrador en la PC y `proceso@raveat.local` en el
teléfono, por ejemplo—: crear un pedido y confirmarlo desde **Pedidos**. En **Preparación** aparece
primero el más viejo. Abrirlo, tocar ▶ en un ítem —el pedido pasa a **En preparación**— y ✓ en el
último: queda **Listo**, y al que lo creó le llega el aviso.

**El historial.** **Historial**, en el menú. El administrador ve todo el local y puede acotarlo con
**Ver solo lo mío**; con `proceso@raveat.local` se ven solo sus movimientos de preparación. Para ver que
el cambio y su fila se guardaron juntos, la misma hora en las dos tablas:

```powershell
mysql -u raveat_app -p raveat_app -e "SELECT p.codigo, p.estado, p.actualizado_en, h.estado_anterior, h.estado_nuevo, h.creado_en FROM pedidos p JOIN historiales h ON h.pedido_id = p.id ORDER BY h.id DESC LIMIT 5;"
```

**La firma.** Con `delivery@raveat.local`, tomar una entrega y pasarla a **En camino**. En la ficha
aparece **Firmar recepción**: firmar con el dedo y **Guardar firma**. La ficha muestra la imagen,
que queda en `RavEat.Api\wwwroot\uploads\firmas`:

```powershell
Get-ChildItem .\RavEat.Api\wwwroot\uploads\firmas
```

## 14 · Google, el pedido público y la tienda

**Entrar con Google.** El web client id va en los dos lados: en `RavEatApp\.env` y en la API.
Después se reinicia `npm run dev` (Vite lee el `.env` al arrancar) y la API.

```powershell
Add-Content .\RavEatApp\.env 'VITE_GOOGLE_WEB_CLIENT_ID=TU_CLIENTE_WEB.apps.googleusercontent.com'
dotnet user-secrets set "GoogleAuth:WebClientId" "TU_CLIENTE_WEB.apps.googleusercontent.com" --project .\RavEat.Api
```

En MonsterASP, la misma clave como variable del panel, con doble guion bajo, y Restart:
`GoogleAuth__WebClientId`. El email con el que se prueba tiene que estar dado de alta en
**Usuarios**: Google autentica, pero el alta la sigue haciendo el administrador.

Las huellas SHA-1 y SHA-256 de debug y de release, para el cliente Android de Google Cloud y para
los App Links:

```powershell
Set-Location .\RavEatApp\android
.\gradlew signingReport
```

La de Play está en Play Console → la app → Prueba y lanzamiento → Configuración → **Integridad de
la app** → Firma de apps.

**El pedido público.** Abrir la ficha de un pedido: el QR lleva su URL pública. Para mirar la
página sin crear nada, el token de muestra:

```powershell
Start-Process http://localhost:5080/p/ejemplo
```

Con la API publicada, el QR apunta a su dominio. Para que lo haga aunque la request llegue por
otra dirección, y para declarar las huellas de los App Links, en el panel de MonsterASP:
`Publico__BaseUrl` = `https://raveat.runasp.net` y una variable por huella,
`Publico__HuellasSha256__0`, `Publico__HuellasSha256__1`, con el formato `AA:BB:...` que da
`signingReport`. Lo que publica la API:

```powershell
Invoke-RestMethod https://raveat.runasp.net/.well-known/assetlinks.json | ConvertTo-Json -Depth 5
```

Con la app instalada, si Android verificó el dominio, dice `verified`:

```powershell
adb shell pm get-app-links app.raveat
```

En un proyecto propio, el host del `intent-filter` de `AndroidManifest.xml` es el de tu API.

**Las fotos en Supabase.** En supabase.com → tu proyecto: Storage → New bucket `raveat`, marcado
**Public**. La URL y la clave `service_role` salen de Project Settings → API. En local:

```powershell
dotnet user-secrets set "Supabase:Url" "https://TU_PROYECTO.supabase.co" --project .\RavEat.Api
dotnet user-secrets set "Supabase:ServiceKey" "TU_SERVICE_ROLE" --project .\RavEat.Api
```

En el panel de MonsterASP: `Supabase__Url` y `Supabase__ServiceKey`. La `service_role` firma como
administrador: nunca en el APK ni en una variable `VITE_`. Sin las dos, las fotos van a
`wwwroot/uploads` como antes.

**El AAB para Play.** Con el keystore de la sección 8:

```powershell
Set-Location .\RavEatApp
npm run build:release
npx cap sync android

Set-Location .\android
.\gradlew bundleRelease
```

Queda en `app\build\outputs\bundle\release\app-release.aab`. Cada subida necesita un
`versionCode` mayor que la anterior, en `android\app\build.gradle`. La política de privacidad que
pide la ficha es `https://raveat.runasp.net/privacidad.html`, y los caminos de descarga que
muestran la carta y la página del pedido se editan en `RavEat.Api\wwwroot\distribucion.json`, sin
recompilar. Ahí está también el switch `appEnPedido`: en `false` la página del pedido no ofrece
abrir ni bajar la app; el día que esté publicada se pone en `true` y se sube solo ese archivo.

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| Sin red, Pedidos muestra el error y no la lista | Esa lectura nunca se hizo con red: no hay copia | Entrar una vez con red; los filtros y las búsquedas tienen copia aparte |
| El cambio de estado no se encola y sale el error de conexión | El teléfono tiene red, pero la API no responde | Reintentar cuando responda: se encola solo cuando el sistema informa que no hay red |
| El cambio no se envía al volver la red | No hay sesión: sin token no se envía | Iniciar sesión y abrir Pedidos |
| «La API no aceptó 1 de los cambios guardados» | Otro usuario movió el pedido mientras no había red | Nada: la lista se recarga con el estado real |
| El APK de release sale `unsigned` | Falta `keystore.properties` | Sección 8 |
| `Keystore was tampered with, or password was incorrect` | La contraseña de `keystore.properties` no es la del keystore | Corregirla; si se perdió, no se recupera |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | Debug y release firman distinto | `adb uninstall app.raveat` y reinstalar |
| El APK de release no conecta con tu PC | Apunta a producción: es lo esperado | Para desarrollo, el APK de debug |
| El sitio publicado responde 500.30 | Falta `ConnectionStrings__Default`, `Jwt__Key`, `Admin__Password` o `UsuariosDePrueba__Password`, o no se hizo Restart | Revisar las variables del panel |
| El sitio responde 403 o 404 en todo | El zip llevó la carpeta y no su contenido | Rearmarlo con el `\*` |
| HTTP anda y HTTPS no | El certificado no se emitió | **HTTPS → Enable HTTPS** |
| El APK dice error de CORS contra producción | Falta `http://localhost` en `Cors__AllowedOrigins__N` | Cargarlo y Restart |
| La web de tu PC no conecta con la API publicada | Falta `http://localhost:5173` en `Cors__AllowedOrigins__N`, o el `.env` sigue apuntando a la API local | Cargarlo y Restart; en `.env`, `VITE_API_URL` con la dirección del sitio |
| La consola de la API dice `Push apagado` | Falta `Firebase:CuentaDeServicio` en User Secrets, o `Firebase__CuentaDeServicio` en el panel | Sección 11 |
| La API no arranca: `FileNotFoundException` del JSON | La ruta del secreto no apunta al archivo | `Test-Path` con esa ruta; en MonsterASP, el JSON tiene que estar en `wwwroot` |
| La bandeja recibe el aviso y el teléfono no | `dispositivos_push` está vacía: el teléfono nunca mandó su token | Cerrar sesión y volver a entrar en el teléfono, con el permiso de notificaciones dado |
| La push le llega al usuario anterior del teléfono | La sesión anterior venció sin **Salir**, y el que entró después todavía no tiene rol | Salir desde **Mi cuenta**: el logout da de baja el teléfono |
| `dotnet publish` falla con MSB3026/3027 | La API local tiene tomados sus `.dll` | Pararla y publicar de nuevo |
| «No se pudo conectar con la API» | La API no está levantada, o la URL apunta a otro lado | Probar `/health`; **Mi cuenta → Diagnóstico** |
| Toqué `src/` y el teléfono muestra lo viejo | Falta el build antes de `cap sync` | Sección 7 o 9 |
| El micrófono no aparece en el teléfono | El teléfono no tiene servicio de reconocimiento de voz | Sección 12: `query-services`; instalar o actualizar la app de Google |
| El micrófono no aparece en la PC | El navegador no tiene Web Speech API (Firefox) | Probar en Chrome o Edge |
| El micrófono late y no escribe nada | Sin red: el reconocimiento de Google la necesita | Volver a tener red; el campo se escribe a mano |
| El mapa queda gris | Sin red, o el servidor de tiles no responde | El aviso lo dice: guardar con la dirección escrita |
| El buscador del mapa no encuentra la dirección | Nominatim busca calle, número y ciudad | Escribirla completa, o arrastrar el mapa hasta la puerta |
| **Preparación** no aparece en el menú | El rol no es administrador ni proceso | Entrar con `proceso@raveat.local` |
| Un pedido confirmado no aparece en Preparación | Sigue en borrador, o ya se entregó | Confirmarlo desde Pedidos; Preparación muestra confirmados, en preparación y listos |
| El historial de un usuario está vacío | Cada rol ve solo lo suyo, y todavía no movió nada | Es lo esperado; el administrador ve el del local |
| **Firmar recepción** no aparece | La entrega no está en camino, o la lleva otro repartidor | Pasarla a En camino con el repartidor que la tomó |
| Guardar la firma dice que no hay conexión | Sin red la firma no se encola | Volver a tener red y firmar de nuevo |
| No aparece **Entrar con Google** | Falta `VITE_GOOGLE_WEB_CLIENT_ID`, o Vite no se reinició | Sección 14 y reiniciar `npm run dev` |
| `DEVELOPER_ERROR (10)` | El client id es el de Android, o falta el cliente Android con la SHA-1 de este APK | Sección 14: el web en el `.env`, y un cliente Android por huella |
| `SIGN_IN_FAILED (12500)` | Falta la pantalla de consentimiento, o en modo prueba tu cuenta no está en Usuarios de prueba | Completarla en Google Cloud |
| «El ingreso con Google no está configurado en el servidor» | Falta `GoogleAuth:WebClientId` en la API | User Secrets, o la variable del panel |
| «Esa cuenta no está habilitada» | El email no está dado de alta, o está inactivo | Darlo de alta en **Usuarios** |
| Anda con el APK propio y falla en el de Play | Play refirma: la huella es otra | Cliente Android y `assetlinks` con la huella de Play |
| El QR abre el navegador y no la app | La huella no está en `assetlinks.json`, o el APK es de debug | `pm get-app-links`; cargar la huella y reinstalar |
| El QR apunta a la IP de la PC | Se generó contra la API local | Es lo esperado; publicada, `Publico__BaseUrl` |
| La foto nueva da 502 «imagen_no_guardada» | Supabase rechazó el archivo: bucket inexistente, o la clave no es `service_role` | Revisar el bucket y la clave, o borrar las dos variables |
| El APK de release no conecta con tu PC por HTTP | El release solo habla HTTPS | Para desarrollo, el de debug |
| Play rechaza el AAB por el `versionCode` | Ya se subió uno con ese número | Subirlo en `build.gradle` |

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Capacitor | 8.4.1 |
| Origen de la app en el teléfono | `http://localhost` |

Los permisos del manifiesto son los de la versión 7: ni esta versión ni la anterior agregan
ninguno a mano. El
plugin de red declara `ACCESS_NETWORK_STATE` y el del dictado `RECORD_AUDIO`, más la consulta del
servicio `android.speech.RecognitionService`, por fusión de manifiestos.

### El icono de la notificación

Las push y los avisos locales llevan el gorro de RavEat en la barra de estado. Es un PNG aparte del
ícono de la app —`ic_stat_notify`, silueta blanca sobre fondo transparente, en las cinco
densidades de `res/drawable-*`—, porque Android pinta el icono chico de un solo color. Lo usan las
push por el `meta-data` del manifiesto y los avisos locales por `capacitor.config.json`. Sin él,
los dos muestran un círculo genérico.

Se regenera solo si cambia el dibujo: la fuente es `recursos/iconos/ic_stat_notify-fuente.png` y
se sube al [Asset Studio](https://romannurik.github.io/AndroidAssetStudio/icons-notification.html)
con el nombre `ic_stat_notify`; el zip que devuelve se descomprime sobre `res/`.

### Íconos y splash · bajo demanda

Solo si se modifican las imágenes fuente de `RavEatApp/assets/`:

```powershell
Set-Location .\RavEatApp
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --android
```
