# Comandos · versión 5

Todo lo que se tipea para poner esta versión a andar, en orden. Los comandos son para
**PowerShell en Windows** y se ejecutan **desde la raíz del repositorio**, salvo que se indique
otra cosa.

Qué es esta versión y por qué está hecha así: [`README.unidad-05.md`](README.unidad-05.md).

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

`npm install` trae también los dos plugins nuevos de esta versión —
`@aparajita/capacitor-biometric-auth` y `@aparajita/capacitor-secure-storage`—: están declarados
en `package.json`, no hay que instalarlos aparte. `dotnet tool restore` instala `dotnet-ef` en la
versión que fija `dotnet-tools.json`.

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

Las migraciones crean las tablas y cargan los datos de prueba — la carta, los clientes, los cinco
roles **y el administrador inicial**, que es la puerta de entrada de esta versión.

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

En el teléfono, `localhost` es el propio teléfono. Para trabajar solo en el navegador:
`VITE_DEBUG_ACTIVADO=false`, y la app usa `VITE_API_URL` (`http://localhost:5080`).

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

**Empezá siempre por `/health`.** Lo primero que muestra la app es **el login**: se entra con el
administrador que sembró la migración.

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

Salidas en `android/app/build/outputs/`. Si cambió la firma: `adb uninstall app.raveat`. La
**huella se prueba acá**: en el navegador no existe.

## 8 · Migraciones

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- migrations list
dotnet tool run dotnet-ef -- database update
dotnet tool run dotnet-ef -- database drop --force
```

El `--` no es decorativo: sin él, `dotnet tool run` se come los argumentos. También podés aplicar
las migraciones al arrancar, con `dotnet run -- --migrate`.

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| «No se pudo conectar con la API» | La API no está levantada, o la URL apunta a otro lado | Probar `/health`; revisar `VITE_API_URL_DEBUG` |
| Todo devuelve **401** | Sin sesión: desde esta versión los endpoints exigen token | Entrar con el administrador sembrado |
| Renueva la sesión y la cierra igual | Dos renovaciones en paralelo: el refresh **rota** y se revocan entre sí | La renovación tiene que ser única en curso — ya lo resuelve `ajax_service` |
| El botón de huella no aparece | No hay biometría en el dispositivo, o no hay sesión guardada | Entrar primero con email y contraseña |
| En el navegador anda y en el teléfono no | `VITE_API_URL_DEBUG` apunta a `localhost` | Poner la IP LAN de la PC (`ipconfig`) |
| Cambié la IP del `.env` y el APK sigue con la vieja | La URL queda **grabada dentro del bundle** | `npm run build` + `npx cap sync android` de nuevo |
| `dotnet build` falla con MSB3026/3027 | La API corriendo tiene tomados sus `.dll` | Pararla (`Ctrl+C`) y recompilar |
| `Access denied for user ...` | Falta el secreto: `appsettings.json` dice `Password=CAMBIAR` | Sección 2 |
| La lista sale vacía pero la API responde | No corrió la migración de datos de prueba | `migrations list` y `database update` |
| Toqué `src/` y el teléfono muestra lo viejo | Falta `npm run build` antes de `cap sync` | Sección 6 |

El catálogo completo está en
[`comandos-por-modulo.md` en `main`](https://github.com/David9-dev/raveat/blob/main/diagramas/comandos-por-modulo.md).

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Capacitor | 8.4.1 |

`gradlew` (el *wrapper*) fija qué Gradle se usa: siempre se invoca `.\gradlew`, nunca `gradle`.
El permiso de biometría lo declara el plugin por fusión de manifiestos: no se toca a mano.

### Firma

Sigue alcanzando el `debug.keystore` automático de Android Studio. El keystore de **release**
llega en la versión 8, con su regla: **el keystore y sus contraseñas nunca se versionan.**

### Íconos y splash · bajo demanda

Solo si se modifican las imágenes fuente de `RavEatApp/assets/`:

```powershell
Set-Location .\RavEatApp
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --android
```
