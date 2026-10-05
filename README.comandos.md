# Comandos · versión 9

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-09.md`](README.unidad-09.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

Un solo paquete nuevo: `@capacitor/network`, el estado de la red. La cola offline no necesita
paquete: es `localStorage` y un service. Para saber si ya está, desde `RavEatApp`:

```powershell
npm ls @capacitor/network
```

En un proyecto propio se suma así — el `cap sync` registra la parte Android del plugin:

```powershell
Set-Location .\RavEatApp
npm install @capacitor/network
npm run build
npx cap sync android
```

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

Esta versión **no suma migraciones**: si venís de la versión 7, la base ya está al día. La API
aplica las migraciones sola cada vez que arranca. Para inspeccionar o reconstruir desde cero:

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
pedidos, sus ítems, entregas y notificaciones, y deja la numeración en `PED-0001`. La carta, los
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
formato que pide Google Play, se genera en la versión 12.

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

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Capacitor | 8.4.1 |
| Origen de la app en el teléfono | `http://localhost` |

Los permisos del manifiesto son los de la versión 7: esta versión no agrega ninguno a mano. El
plugin de red declara `ACCESS_NETWORK_STATE` por fusión de manifiestos.

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
