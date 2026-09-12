# Comandos · versión 3

PowerShell en Windows, desde la raíz del repositorio. Entorno:
[`README.requisitos.md`](README.requisitos.md).

Primera versión con dos procesos: **la API y el frontend corren a la vez**, cada uno en su
terminal. Los pasos 1 a 3 se hacen una sola vez; del 4 en adelante es el día a día.

---

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
Set-Location ..

dotnet tool restore
```

`dotnet tool restore` instala `dotnet-ef` en la versión que fija `dotnet-tools.json` — en el
repositorio, no en la máquina: todos usan la misma sin pisar otras instalaciones.

## 2 · Crear la base y el usuario

La API no crea la base: crea las **tablas** dentro de una base que ya existe. Con la contraseña de
`root` de la instalación:

```powershell
mysql -u root -p
```

```sql
CREATE DATABASE raveat_app CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
CREATE USER 'raveat_app'@'localhost' IDENTIFIED BY 'TU_CLAVE';
GRANT ALL PRIVILEGES ON raveat_app.* TO 'raveat_app'@'localhost';
FLUSH PRIVILEGES;
exit
```

La contraseña **no va en `appsettings.json`** — ese archivo se versiona, y una clave ahí termina en
git. Va en User Secrets, que la guarda fuera del repositorio:

```powershell
Set-Location .\RavEat.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=raveat_app;User=raveat_app;Password=TU_CLAVE;"
Set-Location ..
```

El secreto pisa la plantilla de `appsettings.json` (`Password=CAMBIAR`). El `UserSecretsId` es el
mismo en todas las ramas: se configura una vez y sirve para todo el recorrido. Ver qué quedó:
`dotnet user-secrets list`.

Después, el esquema — crea las tablas **y carga la carta de prueba**:

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- database update
Set-Location ..
```

## 3 · La URL de la API en el frontend

```powershell
Set-Location .\RavEatApp
Copy-Item .\.env.example .\.env
Set-Location ..
```

Editá `.env`: la línea de `VITE_API_URL_DEBUG` viene **comentada** — descomentala y poné la IP de
tu máquina en la red (`ipconfig` la muestra, y cambia con la red):

```
VITE_API_URL_DEBUG=http://TU_IP_LAN:5080
```

Es **esa** variable y no `VITE_API_URL`: con el modo debug activo —el valor por defecto— la app lee
la `_DEBUG` e ignora la otra. Si editás el `.env` con `npm run dev` corriendo, reinicialo: Vite lo
lee al arrancar.

**Por qué una IP y no `localhost`**: en el teléfono, `localhost` es el propio teléfono. Para
trabajar solo en el navegador, `VITE_DEBUG_ACTIVADO=false` y la app usa `http://localhost:5080`.

`.env` está en `.gitignore` y nunca se versiona; `.env.example` sí.

## 4 · Correr

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
| Health | `http://localhost:5080/health` |
| La carta | `http://localhost:5080/api/productos/resumen` |
| App | `http://localhost:5173` |

**Empezá siempre por `/health`.** Si no responde, no hay nada que revisar del lado del frontend.

Alternativa en VS Code: `Ctrl+Shift+P` → `Tasks: Run Task` → **`RavEat: elegir entorno`** levanta
los dos procesos en terminales integradas. **Debug web** es el entorno del día a día.

> ⚠️ **La task borra la base y la reconstruye en cada arranque del backend.** Es intencional: cada
> rama tiene su propia cadena de migraciones, y cambiar de rama no cambia la base real — reconstruir
> elimina esa clase entera de bug. **Los datos que cargues no sobreviven al próximo arranque.**

## 5 · Compilar

```powershell
Set-Location .\RavEatApp
npm run lint
npm run build
Set-Location ..

dotnet build .\RavEat.Api\RavEat.Api.csproj
```

Si `dotnet build` falla con MSB3026/3027, la API está corriendo y tiene tomados sus `.dll`:
pararla con `Ctrl+C` y compilar de nuevo.

## 6 · Llevarlo al teléfono

Con el teléfono conectado, la depuración USB activada y **la IP de la red en `.env`**:

```powershell
Set-Location .\RavEatApp
npm run build
npx cap run android
```

> ⚠️ **`cap run` no compila la web.** Sincroniza lo que hay en `dist/`, así que sin `npm run build`
> antes el APK lleva el código de la vez anterior, sin avisar. Vale también para la URL: si
> cambiaste el `.env`, hace falta `build` de nuevo — la URL queda grabada dentro del bundle.

`INSTALL_FAILED_UPDATE_INCOMPATIBLE`: la app instalada se compiló en otra máquina y su firma no
coincide — `adb uninstall app.raveat`.

## 7 · Migraciones

```powershell
Set-Location .\RavEat.Api
dotnet tool run dotnet-ef -- migrations list      # cuáles hay y cuáles faltan aplicar
dotnet tool run dotnet-ef -- database update      # aplicar las pendientes
dotnet tool run dotnet-ef -- database drop --force
```

El `--` no es decorativo: sin él, `dotnet tool run` se come los argumentos en vez de pasárselos a
`dotnet-ef`.

### Aplicarlas al arrancar: `dotnet run -- --migrate`

Las mismas migraciones, sin el paso aparte: la API aplica lo que falte y después queda escuchando.

```powershell
Set-Location .\RavEat.Api
dotnet run -- --migrate
```

| Comando | Aplica lo pendiente | Levanta la API |
|---|---|---|
| `dotnet run` | no | sí |
| `dotnet run -- --migrate` | sí, **antes** de escuchar | sí |
| `dotnet tool run dotnet-ef -- database update` | sí | no |

Acá también el `--` separa: lo que va antes es para `dotnet run`, lo que va después es para la API.
Sin él, `dotnet` toma `--migrate` como opción suya y no la reconoce.

Lo hace `Program.cs`: si `--migrate` está entre los argumentos, corre `MigrateAsync()` y recién
entonces `app.Run()`. **No borra nada** — aplica las migraciones que falten, igual que
`database update`. Si la base quedó con el esquema de otra rama, primero `database drop --force`.

Si la base no está accesible, el arranque corta con la excepción de conexión y la API no llega a
escuchar. Es lo que se busca: peor sería levantar contra un esquema que no es el de esta rama.

---

## 8 · Depurar de punta a punta

**Siempre en este orden**, de adentro hacia afuera: la API sola, después la app, y recién después
el teléfono. Saltear pasos es lo que hace buscar el problema donde no está.

### 1. ¿Responde la API?

```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod http://localhost:5080/api/productos/resumen | Select-Object -ExpandProperty resumen
```

`/health` tiene que devolver `status: ok`. Si no responde, no hay nada que revisar del lado del
frontend.

### 2. ¿Contra qué dirección pega la app?

En la app, **Mi cuenta → Diagnóstico**: muestra la URL que resolvió `config/debug.js` y prueba
`/health` con un botón. Es la misma respuesta del paso anterior, pero vista desde donde importa —
adentro de la app, y en el teléfono, donde no hay consola a mano.

| Lo que muestra | Qué significa |
|---|---|
| `http://localhost:5080` **en el teléfono** | está mal: ahí `localhost` es el propio teléfono |
| la IP de tu máquina y «La API respondió: ok» | el camino completo funciona |
| la IP correcta y un error | la API no está levantada, o el teléfono no está en la misma red |

### 3. La consola de red del teléfono

Con el teléfono conectado por USB y la depuración USB activada:

1. en Chrome de la PC, ir a `chrome://inspect/#devices`
2. buscar la WebView de tu app — figura con el id del paquete, `app.raveat`
3. **inspect**, y se abre un DevTools apuntando a lo que corre en el teléfono
4. pestaña **Red**, tocar **Reintentar** en la pantalla con error
5. tiene que aparecer la fila del pedido, con la dirección a la que salió

Esa fila es la prueba: si dice `localhost`, el `.env` no llegó al bundle; si dice tu IP y falla
igual, el problema está del lado de la API o de la red.

> La consola de red también sirve en el navegador (`F12` → Red), pero ahí `localhost` funciona
> siempre. El error de `localhost` **solo se ve en el teléfono**.

---

## Si algo falla

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| «No se pudo conectar con la API» | la API no está levantada, o la URL apunta a otro lado | probar `/health` en el navegador; revisar `.env` |
| En el navegador anda, en el teléfono no | `VITE_API_URL_DEBUG` dice `localhost` | poner la IP de la red (`ipconfig`) |
| Cambié el `.env` y el APK sigue igual | la URL queda grabada en el bundle | `npm run build` + `npx cap run android` |
| `Access denied for user ...` | falta el secreto: la conexión sigue con `Password=CAMBIAR` | paso 2 |
| `dotnet-ef` no encontrado | falta `dotnet tool restore` | paso 1 |
| La lista sale vacía pero la API responde | no corrió la migración de datos | `migrations list` — paso 7 |
| Puse la IP en el `.env` y sigue llamando a `localhost` | la IP fue a `VITE_API_URL`, pero con el modo debug activo la app usa **`VITE_API_URL_DEBUG`**, que viene comentada en la plantilla | descomentar `VITE_API_URL_DEBUG` con la IP, y **reiniciar `npm run dev`**: Vite lee el `.env` al arrancar |
| Error de CORS en la consola | el origen no está permitido | en desarrollo se aceptan `localhost` e IPs privadas; revisar `Cors:AllowedOrigins` |

---

## Datos del proyecto Android

| Dato | Valor |
|---|---|
| `appId` / `applicationId` / `namespace` | `app.raveat` |
| `webDir` | `dist` |
| `minSdkVersion` | 24 |
| `compileSdkVersion` / `targetSdkVersion` | 36 |
| Android Gradle Plugin | 9.3.0 |
| Gradle (wrapper) | 9.5.0 |
| Capacitor | 8.4.1 |
| Permiso declarado | `INTERNET` |

Siempre `.\gradlew`, nunca `gradle`: el *wrapper* fija la versión y el build es reproducible.
`android/local.properties` **no se versiona**: lo genera Android Studio en cada máquina.

Los íconos y el splash ya vienen generados y versionados. Solo si cambiás las fuentes de
`RavEatApp/assets/` se regeneran: `npx @capacitor/assets generate --android` — con npm 11+ hace
falta aprobar antes el script de `sharp` (`npm install-scripts approve sharp`).

### Firma

El APK de depuración usa el `debug.keystore` que Android Studio genera solo en cada máquina. No hay
nada que configurar. El keystore de **release** —el que firma para publicar— tiene una sola regla:
**nunca se versiona.** El `.gitignore` ya lo corta.
