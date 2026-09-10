# Comandos · versión 4

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-04.md`](README.unidad-04.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

`dotnet tool restore` instala `dotnet-ef` en la versión que fija `dotnet-tools.json`, en el propio
repositorio y no en la máquina: todos usan la misma sin pisar otras instalaciones.

## 2 · Crear la base y el usuario

Solo si no venís de la versión 3 con la base ya creada. La API no crea la base: crea las
**tablas** dentro de una base que ya existe. Desde `mysql -u root -p`:

```sql
CREATE DATABASE raveat_app CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
CREATE USER 'raveat_app'@'localhost' IDENTIFIED BY 'TU_CLAVE';
GRANT ALL PRIVILEGES ON raveat_app.* TO 'raveat_app'@'localhost';
FLUSH PRIVILEGES;
```

La contraseña **no va en `appsettings.json`** —ese archivo se versiona, y una clave ahí termina en
git—. Va en User Secrets:

```powershell
Set-Location .\RavEat.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=raveat_app;User=raveat_app;Password=TU_CLAVE;"
Set-Location ..
```

El secreto pisa la plantilla `Password=CAMBIAR`. El `UserSecretsId` del `.csproj` es **el mismo en
todas las ramas**: se configura una vez y sigue andando al cambiar de versión. Para ver qué quedó
guardado: `dotnet user-secrets list`.

## 3 · Crear el esquema

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- database update
Set-Location ..
```

Las migraciones crean las tablas **y cargan los datos de prueba**: la carta con sus fotos y los
clientes. Si la base queda vacía después de esto, no corrió la migración de datos.

> **Si venís de otra versión, la base hay que reconstruirla** (`database drop --force` y de nuevo
> `update`): cada rama tiene su propia cadena de migraciones y no es acumulativa entre ramas.
> Cambiar de rama no cambia la base real, y el esquema queda desincronizado del código.

## 4 · La URL de la API en el frontend

```powershell
Set-Location .\RavEatApp
Copy-Item .\.env.example .\.env
Set-Location ..
```

Editá `.env` y poné la IP de tu máquina en la red local:

```
VITE_API_URL_DEBUG=http://TU_IP_LAN:5080
```

**Por qué una IP y no `localhost`**: en el teléfono, `localhost` es el propio teléfono. La IP la
averiguás con `ipconfig` y **cambia según la red**. Para trabajar solo en el navegador, poné
`VITE_DEBUG_ACTIVADO=false` y la app usa `VITE_API_URL` (`http://localhost:5080`).

`.env` está en `.gitignore` y nunca se versiona; `.env.example` sí.

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

**Empezá siempre por `/health`.** Si no responde, no hay nada que revisar del lado del frontend.
Desde la app la misma prueba está en **Mi cuenta → Diagnóstico**, que además muestra contra qué
URL está pegando: es la única forma de verla en el teléfono, donde no hay consola a mano.

Cada bloque se prueba por consola antes de tocar la pantalla, en **otra** terminal:

```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod "http://localhost:5080/api/clientes?tamano=2" | ConvertTo-Json -Depth 4
Invoke-RestMethod "http://localhost:5080/api/productos/listado?busqueda=empanadas&disponible=true"
```

Cada listado responde con `pagina: {pagina, tamano, total, hay_mas}`: con `tamano=2` y los cinco
clientes sembrados, `hay_mas` dice `true`. La búsqueda filtra **en la base**, no en la página
descargada.

Los rechazos de la API se provocan a propósito: también son una prueba. `Invoke-RestMethod` tira
la excepción en rojo con un 400 y esconde el cuerpo; se lee así:

```powershell
try { Invoke-RestMethod -Method Post "http://localhost:5080/api/clientes" -ContentType "application/json" -Body '{"nombre":"","telefono":""}' } catch { (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
```

Responde `cliente_nombre_requerido`. El mismo gesto con el teléfono de María (`11-5555-1001`)
responde `cliente_duplicado`; y en un pedido creado desde la app, un salto de estado inválido por
`PUT /api/pedidos/{id}/estado` responde `transicion_invalida`.

## 6 · Compilar

```powershell
Set-Location .\RavEatApp
npm run lint
npm run build
Set-Location ..

dotnet build .\RavEat.Api\RavEat.Api.csproj
```

El resultado del frontend queda en `RavEatApp/dist/`. De ahí lo toma Capacitor: **`cap sync` copia
lo que hay en `dist/`, no lo que hay en `src/`** — sin un `build` previo, el APK lleva el código
viejo. Es el olvido más común.

## 7 · Llevarlo al teléfono

```powershell
Set-Location .\RavEatApp
npm run build
npx cap sync android

Set-Location .\android
.\gradlew installDebug      # instalar en el dispositivo conectado
.\gradlew assembleDebug     # solo generar el APK
```

Salidas en `android/app/build/outputs/`. Para desinstalar una versión previa (por ejemplo si
cambió la firma): `adb uninstall app.raveat`. Para abrirlo en Android Studio:
`npx cap open android`.

## 8 · Migraciones

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- migrations list      # ver cuáles hay y cuáles faltan aplicar
dotnet tool run dotnet-ef -- database update      # aplicar las pendientes
dotnet tool run dotnet-ef -- database drop --force
```

El `--` no es decorativo: sin él, `dotnet tool run` se come los argumentos en vez de pasárselos a
`dotnet-ef`. También podés aplicarlas al arrancar, con `dotnet run -- --migrate`.

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

Una migración de datos se genera **vacía** y se completa a mano con `InsertData` — así se
escribieron los clientes de prueba. Conviene leer el `Up()` antes de aplicar.

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| «No se pudo conectar con la API» | La API no está levantada, o la URL apunta a otro lado | **Mi cuenta → Diagnóstico**: ahí se ven la URL y la respuesta de `/health` |
| En el navegador anda y en el teléfono no | `VITE_API_URL_DEBUG` apunta a `localhost` | Poner la IP LAN de la PC (`ipconfig`) |
| Cambié la IP del `.env` y el APK sigue con la vieja | La URL queda **grabada dentro del bundle** | `npm run build` + `npx cap sync android` de nuevo |
| Error de CORS en la consola | El origen no está permitido | En Development se aceptan localhost e IPs privadas |
| `dotnet build` falla con MSB3026/3027 | La API corriendo tiene tomados sus `.dll` | Pararla (`Ctrl+C`) y recompilar |
| `Access denied for user ...` | Falta el secreto: `appsettings.json` dice `Password=CAMBIAR` | Sección 2 |
| `dotnet-ef` no encontrado | Falta `dotnet tool restore` | Sección 1 |
| La migración se genera vacía | No compilaste antes del `migrations add` | `dotnet build` y regenerar |
| El 400 sale en rojo y sin cuerpo | `Invoke-RestMethod` esconde el body | Leerlo con el `StreamReader` de la Sección 5 |
| Error de CORS **con la API levantada** | Dos API pelearon el puerto 5080 | `Get-NetTCPConnection -LocalPort 5080 -State Listen`, cerrar todas y levantar una |
| La lista sale vacía pero la API responde | No corrió la migración de datos de prueba | `migrations list` y `database update` |
| Toqué `src/` y el teléfono muestra lo viejo | Falta `npm run build` antes de `cap sync` | Sección 6 |
| Parpadeo blanco al abrir la app | Se quitó el script inline de `index.html` | Restaurarlo: aplica el tema antes del bundle |

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Capacitor | 8.4.1 |

**Gradle** es el motor de build y el **Android Gradle Plugin** es lo que le enseña a Gradle qué es
un APK: son dos versiones distintas. `gradlew` (el *wrapper*) fija cuál Gradle se usa, y por eso el
build es reproducible; siempre se invoca `.\gradlew`, nunca `gradle`. El único permiso declarado
es `INTERNET`.

### Firma

En esta versión alcanza con el `debug.keystore` automático: Android Studio lo genera solo en
`$env:USERPROFILE\.android\debug.keystore` y `assembleDebug` lo usa sin configurar nada. Si
generás un keystore propio, la regla es una sola: **el keystore y sus contraseñas nunca se
versionan.**

### Íconos y splash · bajo demanda

Los recursos gráficos de Android ya están generados y versionados. Solo si se modifican las
imágenes fuente de `RavEatApp/assets/` se regeneran con:

```powershell
Set-Location .\RavEatApp
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --android
```

`npx` descarga la versión indicada, la ejecuta y no toca `package.json`. No forma parte de la
instalación normal.
