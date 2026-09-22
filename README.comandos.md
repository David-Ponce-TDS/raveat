# Comandos · versión 7

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-07.md`](README.unidad-07.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

`npm install` trae también los tres plugins nuevos. Están declarados en `package.json`: no se
instalan aparte. La API no suma paquetes en esta versión.

| Paquete | Para qué |
|---|---|
| `@capacitor/geolocation` | capturar las coordenadas del cliente |
| `@capacitor/local-notifications` | la notificación local, la que dispara la propia app |
| `@capacitor/push-notifications` | registrarse en FCM y recibir el push |

Para saber si ya están, desde `RavEatApp`: cada plugin tiene que aparecer con su versión.

```powershell
npm ls @capacitor/geolocation @capacitor/local-notifications @capacitor/push-notifications
```

En un proyecto propio se suman así — el `cap sync` registra la parte Android de cada plugin:

```powershell
Set-Location .\RavEatApp
npm install @capacitor/geolocation @capacitor/local-notifications @capacitor/push-notifications
npm run build
npx cap sync android
```

Los dos permisos de ubicación no los agrega ningún comando: van a mano en `AndroidManifest.xml`
(ver [Datos del proyecto Android](#datos-del-proyecto-android)).

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

Esta versión **suma una migración**, `Reparto`: crea las tablas `entregas` y `notificaciones`. Las
anteriores siguen cargando los datos de prueba: la carta, los clientes, los cinco roles y el
administrador inicial.

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

Para el push, además, la bandera — sin ella la app ni siquiera intenta registrarse en FCM:

```
VITE_PUSH_NOTIFICATIONS_ENABLED=true
```

En el teléfono, `localhost` es el propio teléfono. Para trabajar solo en el navegador:
`VITE_DEBUG_ACTIVADO=false`, y la app usa `VITE_API_URL` (`http://localhost:5080`).

## 5 · El archivo de Firebase

Solo para el push. Se baja de la consola de Firebase con el paquete `app.raveat` y va acá:

```
RavEatApp/android/app/google-services.json
```

**No se versiona**: `.gitignore` lo corta, porque es la credencial de tu proyecto. Comprobar que
está en su lugar:

```powershell
if (Test-Path .\RavEatApp\android\app\google-services.json) { 'ok' } else { 'FALTA' }
```

El `build.gradle` aplica el plugin de Google **solo si el archivo existe**: sin él la app compila
igual y funciona todo menos el push. El build no puede depender de un secreto.

## 6 · Correr

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
Invoke-RestMethod "http://localhost:5080/api/notificaciones" -Headers $auth | ConvertTo-Json -Depth 4
Invoke-RestMethod "http://localhost:5080/api/entregas" -Headers $auth | ConvertTo-Json -Depth 4
```

**La bandeja va a venir vacía, y no es un error.** Es la regla del módulo funcionando: ninguna
notificación se escribe para el que ejecutó la acción. Para verla con algo adentro hacen falta dos
usuarios — uno crea el pedido, el otro le cambia el estado:

```powershell
$otro = Invoke-RestMethod -Method Post "http://localhost:5080/api/sesion/login" -ContentType "application/json" -Body '{"email":"OTRO_USUARIO","password":"SU_CLAVE"}'
$auth2 = @{ Authorization = "Bearer $($otro.sesion.token)" }
Invoke-RestMethod -Method Put "http://localhost:5080/api/pedidos/1/estado" -Headers $auth2 -ContentType "application/json" -Body '{"estado":"confirmado"}'
Invoke-RestMethod "http://localhost:5080/api/notificaciones" -Headers $auth | ConvertTo-Json -Depth 4
```

Ahora el `resumen.no_leidas` del **primero** dejó de ser cero. Ese número es el de la campanita: sale
del resumen del servidor sobre la bandeja completa, nunca de contar las filas que bajaron.

La entrega no se crea por su propio endpoint: **nace con el pedido** de tipo `entregas`. Un pedido
de ese tipo sin cliente con dirección se rechaza antes de existir:

```powershell
try { Invoke-RestMethod -Method Post "http://localhost:5080/api/pedidos" -Headers $auth -ContentType "application/json" -Body '{"tipo":"entregas","items":[{"producto_id":1,"cantidad":1}]}' } catch { (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
```

Responde `entrega_sin_cliente`. Con un cliente que sí tiene dirección, el mismo `POST` devuelve el
pedido y `GET /api/entregas` ya la muestra en `pendiente`.

## 7 · Compilar

```powershell
Set-Location .\RavEatApp
npm run lint
npm run build
Set-Location ..

dotnet build .\RavEat.Api\RavEat.Api.csproj
```

**`cap sync` copia lo que hay en `dist/`, no lo que hay en `src/`** — sin un `build` previo, el
APK lleva el código viejo. Es el olvido más común.

## 8 · Llevarlo al teléfono

```powershell
Set-Location .\RavEatApp
npm run build
npx cap sync android

Set-Location .\android
.\gradlew installDebug      # instalar en el dispositivo conectado
.\gradlew assembleDebug     # solo generar el APK
```

Salidas en `android/app/build/outputs/`. La notificación local, el push y el diálogo del permiso de
ubicación **se prueban acá**: en el navegador, el push no existe y la local no sale de la pestaña.

Para volver a ver el diálogo del permiso desde cero —por ejemplo, después de un «no volver a
preguntar»—, se desinstala y se instala de nuevo:

```powershell
adb uninstall app.raveat
Set-Location .\RavEatApp\android
.\gradlew installDebug
```

## 9 · Mandar un push

El envío **no sale de nuestra API**: se hace desde la consola de Firebase, y esa es la lección. Se
necesita el APK instalado, con `google-services.json` adentro y el permiso de notificaciones
concedido.

1. Abrir la app una vez: al arrancar se registra en FCM.
2. En la consola de Firebase: **Messaging → Nueva campaña → Notificaciones**.
3. Escribir título y texto, elegir la app `app.raveat` y enviar.
4. **Cerrar la app** antes de que llegue: el push es el único de los tres que llega con la app
   cerrada.

El token de registro queda en el log del dispositivo, por si hace falta apuntarle a uno solo:

```powershell
adb logcat -s Capacitor/Console | Select-String "Push token registrado"
```

## 10 · Migraciones

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
| La campanita siempre en cero | Estás solo: no se notifica al que ejecuta la acción | Dos usuarios, uno crea y el otro cambia el estado (Sección 6) |
| La bandeja trae notificaciones de otro | No puede pasar: la consulta filtra por el token | Si pasa, hay un `usuario_id` viajando por query donde no debe |
| El contador no coincide con las filas en pantalla | Está bien: el contador es de la bandeja completa | Es el `resumen` del servidor, no la página |
| El push no llega | Falta `google-services.json`, la bandera del `.env` o el permiso | Secciones 4 y 5, y reinstalar el APK |
| El push no llega **con la app abierta** | Es lo esperado en Android: en primer plano no se muestra solo | Cerrar la app y volver a enviar |
| La app compila pero nunca se registra en FCM | El `.json` se lee al **compilar**: se agregó después del build | `npm run build` + `npx cap sync android` + `installDebug` |
| «Sin permiso de ubicación» y no vuelve a preguntar | «No volver a preguntar»: el sistema ya no muestra el diálogo | Es lo esperado: la app manda a los ajustes |
| Las coordenadas quedan vacías y el cliente se guarda igual | Es lo esperado: sin coordenadas el mapa busca por la dirección | Nada |
| `getCurrentPosition` tarda y termina en error | GPS apagado, o bajo techo sin señal | Prender la ubicación del teléfono; la aproximada alcanza |
| «Un pedido de entrega necesita un cliente» | El pedido de tipo `entregas` nació sin cliente | Elegir cliente, y que tenga dirección cargada |
| «El cliente no tiene dirección cargada» | El cliente existe pero la dirección está vacía | Editarlo y cargar la dirección |
| La entrega no aparece después de crear el pedido | El pedido no era de tipo `entregas` | La entrega nace **solo** con ese tipo |
| «Solo se puede tomar la entrega para uno mismo» | Un delivery quiso asignársela a otro | Los que venden asignan; delivery solo toma |
| «La entrega la mueve el repartidor asignado» | Otro delivery quiso moverla | La mueve el suyo, o admin y los que venden |
| La notificación local no aparece en el navegador | El plugin solo existe en el APK | Probarla en el teléfono |
| «No se pudo conectar con la API» | La API no está levantada, o la URL apunta a otro lado | Probar `/health`; con sesión, **Mi cuenta → Diagnóstico** |
| Todo devuelve **401** | Sin sesión: los endpoints exigen token | Entrar con el administrador sembrado |
| En el navegador anda y en el teléfono no | `VITE_API_URL_DEBUG` apunta a `localhost` | Poner la IP LAN de la PC (`ipconfig`) |
| Cambié la IP del `.env` y el APK sigue con la vieja | La URL queda **grabada dentro del bundle** | `npm run build` + `npx cap sync android` de nuevo |
| `dotnet build` falla con MSB3026/3027 | La API corriendo tiene tomados sus `.dll` | Pararla (`Ctrl+C`) y recompilar |
| `Access denied for user ...` | Falta el secreto: `appsettings.json` dice `Password=CAMBIAR` | Sección 2 |
| El 400 sale en rojo y sin cuerpo | `Invoke-RestMethod` esconde el body | Leerlo con el `StreamReader` de la Sección 6 |
| Error de CORS **con la API levantada** | Dos API pelearon el puerto 5080 | `Get-NetTCPConnection -LocalPort 5080 -State Listen`, cerrar todas y levantar una |
| Toqué `src/` y el teléfono muestra lo viejo | Falta `npm run build` antes de `cap sync` | Sección 7 |

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
| `ACCESS_COARSE_LOCATION` · `ACCESS_FINE_LOCATION` | las dos precisiones: desde Android 12 el usuario puede dar solo la aproximada |
| `<provider>` de `FileProvider` + `res/xml/file_paths.xml` | la cámara entrega la foto a la webview por ahí |

`VIBRATE`, `POST_NOTIFICATIONS` y el permiso de biometría los declaran sus plugins por fusión de
manifiestos: no se tocan a mano.

### Firma

Sigue alcanzando el `debug.keystore` automático de Android Studio. Si generás un keystore propio,
la regla es una sola: **el keystore y sus contraseñas nunca se versionan.** Lo mismo vale para
`google-services.json`.

### Íconos y splash · bajo demanda

Solo si se modifican las imágenes fuente de `RavEatApp/assets/`:

```powershell
Set-Location .\RavEatApp
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --android
```
