# Comandos · versión 6

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-06.md`](README.unidad-06.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

`npm install` trae también los cinco plugins nuevos de esta versión; `dotnet build` restaura
QuestPDF y QRCoder. Están declarados en `package.json` y en el `.csproj`: no se instalan aparte.

| Paquete | Para qué |
|---|---|
| `@capacitor/haptics` | vibración |
| `@capacitor/camera` | cámara y galería |
| `@capacitor/filesystem` · `@capacitor/share` | escribir el PDF y compartirlo |
| `@capacitor-mlkit/barcode-scanning` | escanear el QR |
| `QuestPDF` · `QRCoder` (API) | generar el PDF y el QR |

Para saber si ya están, desde `RavEatApp`: cada plugin tiene que aparecer con su versión.

```powershell
npm ls @capacitor/haptics @capacitor/camera @capacitor/filesystem @capacitor/share @capacitor-mlkit/barcode-scanning
```

En un proyecto propio se suman así — el `cap sync` registra la parte Android de cada plugin:

```powershell
Set-Location .\RavEatApp
npm install @capacitor/haptics @capacitor/camera @capacitor/filesystem @capacitor/share @capacitor-mlkit/barcode-scanning
npm run build
npx cap sync android
Set-Location ..\RavEat.Api
dotnet add package QuestPDF
dotnet add package QRCoder
```

El permiso `CAMERA` y el `<provider>` de la cámara no los agrega ningún comando: van a mano en
`AndroidManifest.xml` (ver [Datos del proyecto Android](#datos-del-proyecto-android)).

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
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=raveat_app;User=raveat_app;Password=TU_CLAVE;"
Set-Location ..
```

El `UserSecretsId` es el mismo en todas las ramas: se configura una vez. Para ver qué quedó:
`dotnet user-secrets list`.

## 3 · Crear el esquema

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- database update
Set-Location ..
```

Las migraciones crean las tablas y cargan los datos de prueba: la carta, los clientes, los cinco
roles y el administrador inicial. Esta versión no suma migraciones.

> **Si venís de otra versión, la base hay que reconstruirla** (`database drop --force` y de nuevo
> `update`): cada rama tiene su propia cadena de migraciones y no es acumulativa entre ramas.

## 4 · La URL de la API en el frontend

```powershell
Set-Location .\RavEatApp
Copy-Item .\.env.example .\.env
Set-Location ..
```

Editá `.env` y poné la IP de tu máquina en la red local (`ipconfig`):

```
VITE_API_URL_DEBUG=http://TU_IP_LAN:5080
```

En el teléfono, `localhost` es el propio teléfono, y la cámara y el escáner se prueban ahí. Para
trabajar solo en el navegador: `VITE_DEBUG_ACTIVADO=false`, y la app usa `VITE_API_URL`
(`http://localhost:5080`).

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

**Empezá siempre por `/health`.** La app abre en el login: se entra con el administrador que sembró
la migración. Con la sesión iniciada, **Mi cuenta → Diagnóstico** muestra contra qué URL pega la
app y si responde.

Cada bloque se prueba por consola antes de tocar la pantalla, en **otra** terminal. Casi todo pide
token: primero se entra, y el token va en el encabezado `Authorization`.

```powershell
Invoke-RestMethod http://localhost:5080/health
$sesion = Invoke-RestMethod -Method Post "http://localhost:5080/api/sesion/login" -ContentType "application/json" -Body '{"email":"admin@raveat.local","password":"RavEat123!"}'
$auth = @{ Authorization = "Bearer $($sesion.sesion.token)" }
Invoke-RestMethod "http://localhost:5080/api/clientes?tamano=2" -Headers $auth | ConvertTo-Json -Depth 4
```

La foto viaja por **multipart**, no como JSON. Desde PowerShell lo más directo es `curl.exe` (el
ejecutable, no el alias), con una foto de la carta:

```powershell
curl.exe -X POST http://localhost:5080/api/productos -H "Authorization: Bearer $($sesion.sesion.token)" -F "categoria_id=1" -F "nombre=Prueba multipart" -F "precio=1000" -F "disponible=true" -F "imagen=@RavEat.Api/wwwroot/seed/productos/flan.jpg"
```

El comprobante y el QR son binarios: se guardan a un archivo. El `1` es el id de un pedido creado
desde la app.

```powershell
Invoke-WebRequest http://localhost:5080/api/pedidos/1/comprobante -Headers $auth -OutFile pedido.pdf -UseBasicParsing
Invoke-WebRequest http://localhost:5080/api/pedidos/1/qr -Headers $auth -OutFile pedido-qr.png -UseBasicParsing
```

Si el PDF abre y el QR se deja leer por cualquier lector, el backend está entero: lo que falle
después es del teléfono (permiso, plugin o manifiesto).

Los rechazos de la API también son una prueba. `Invoke-RestMethod` tira la excepción en rojo con
un 400 y esconde el cuerpo; se lee así:

```powershell
try { Invoke-RestMethod -Method Post "http://localhost:5080/api/clientes" -Headers $auth -ContentType "application/json" -Body '{"nombre":"","telefono":""}' } catch { (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
```

Responde `cliente_nombre_requerido`.

## 6 · Compilar

```powershell
Set-Location .\RavEatApp
npm run lint
npm run build
Set-Location ..

dotnet build .\RavEat.Api\RavEat.Api.csproj
```

**`cap sync` copia lo que hay en `dist/`, no lo que hay en `src/`** — sin un `build` previo, el
APK lleva el código viejo. Es el olvido más común.

## 7 · Llevarlo al teléfono

```powershell
Set-Location .\RavEatApp
npm run build
npx cap sync android

Set-Location .\android
.\gradlew installDebug      # instalar en el dispositivo conectado
.\gradlew assembleDebug     # solo generar el APK
```

Salidas en `android/app/build/outputs/`. Cámara, escáner, compartir y huella **se prueban acá**:
en el navegador no existen.

Para volver a ver el diálogo del permiso desde cero —por ejemplo, después de un «no volver a
preguntar»—, se desinstala y se instala de nuevo:

```powershell
adb uninstall app.raveat
Set-Location .\RavEatApp\android
.\gradlew installDebug
```

## 8 · Migraciones

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- migrations list
dotnet tool run dotnet-ef -- database update
dotnet tool run dotnet-ef -- database drop --force
```

El `--` no es decorativo: sin él, `dotnet tool run` se come los argumentos. También podés aplicar
las migraciones al arrancar, con `dotnet run -- --migrate`.

Para crear una migración propia, el ciclo es **compilar, generar, revisar, aplicar** — `dotnet-ef`
lee el ensamblado **compilado**, así que sin `dotnet build` antes del `add` la migración sale vacía
o vieja:

```powershell
Set-Location .\RavEat.Api
dotnet build
dotnet tool run dotnet-ef -- migrations add MiCambio
dotnet tool run dotnet-ef -- migrations list
dotnet tool run dotnet-ef -- database update
```

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| La cámara no abre y aparece un mensaje de permiso | El permiso quedó denegado | Ajustes del teléfono, o reinstalar (Sección 7) |
| El diálogo del permiso no vuelve a aparecer | «No volver a preguntar»: el sistema ya no pregunta | Es lo esperado: la app manda a los ajustes |
| La app se cae al sacar la foto | Falta el `<provider>` de `FileProvider` | Está en `AndroidManifest.xml`: no recortarlo |
| La subida de la foto da **400** | Se le puso `Content-Type` a mano al `FormData` | `contentType: false` y `processData: false` |
| «La categoría indicada no existe» con una elegida | El multipart no aplica `snake_case`: `categoria_id` llega en 0 | `[FromForm(Name = "categoria_id")]` en la propiedad |
| El primer escaneo dice que se descarga el lector | ML Kit baja el lector aparte la primera vez | Esperar unos segundos y reintentar |
| El escáner no anda en el navegador | El lector solo existe en el APK | Tipear el código en el buscador de Pedidos |
| El primer comprobante falla con un texto de licencia | QuestPDF exige declarar la licencia | La línea de `Program.cs`: no borrarla |
| Las fotos subidas no están en otra máquina | `wwwroot/uploads/` está en `.gitignore` | Es a propósito: son datos, no código |
| «No se pudo conectar con la API» | La API no está levantada, o la URL apunta a otro lado | Probar `/health`; con sesión, **Mi cuenta → Diagnóstico** |
| Todo devuelve **401** | Sin sesión: los endpoints exigen token | Entrar con el administrador sembrado |
| El botón de huella no aparece | No hay biometría en el dispositivo, o no hay sesión guardada | Entrar primero con email y contraseña |
| En el navegador anda y en el teléfono no | `VITE_API_URL_DEBUG` apunta a `localhost` | Poner la IP LAN de la PC (`ipconfig`) |
| Cambié la IP del `.env` y el APK sigue con la vieja | La URL queda **grabada dentro del bundle** | `npm run build` + `npx cap sync android` de nuevo |
| `dotnet build` falla con MSB3026/3027 | La API corriendo tiene tomados sus `.dll` | Pararla (`Ctrl+C`) y recompilar |
| `Access denied for user ...` | Falta el secreto: `appsettings.json` dice `Password=CAMBIAR` | Sección 2 |
| El 400 sale en rojo y sin cuerpo | `Invoke-RestMethod` esconde el body | Leerlo con el `StreamReader` de la Sección 5 |
| Error de CORS **con la API levantada** | Dos API pelearon el puerto 5080 | `Get-NetTCPConnection -LocalPort 5080 -State Listen`, cerrar todas y levantar una |
| Toqué `src/` y el teléfono muestra lo viejo | Falta `npm run build` antes de `cap sync` | Sección 6 |

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Capacitor | 8.4.1 |

`gradlew` (el *wrapper*) fija qué Gradle se usa: siempre se invoca `.\gradlew`, nunca `gradle`.

| En `AndroidManifest.xml` | Por qué |
|---|---|
| `CAMERA` | permiso **peligroso**: se declara y además se pide en tiempo de ejecución |
| `READ_EXTERNAL_STORAGE` (hasta Android 12) | la galería; desde Android 13 el selector no pide permiso |
| `<provider>` de `FileProvider` + `res/xml/file_paths.xml` | la cámara entrega la foto a la webview por ahí |

`VIBRATE` y el permiso de biometría los declaran sus plugins por fusión de manifiestos: no se tocan
a mano.

### Firma

Sigue alcanzando el `debug.keystore` automático de Android Studio. Si generás un keystore propio,
la regla es una sola: **el keystore y sus contraseñas nunca se versionan.**

### Íconos y splash · bajo demanda

Solo si se modifican las imágenes fuente de `RavEatApp/assets/`:

```powershell
Set-Location .\RavEatApp
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --android
```
