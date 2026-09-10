# Comandos · versión 2

PowerShell en Windows, desde la raíz del repositorio. Entorno:
[`README.requisitos.md`](README.requisitos.md).

Mismo ciclo que la versión 1. Lo único nuevo es el paso 7.

---

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
```

Sin `.env` ni secretos: la app todavía no habla con ninguna API.

## 2 · Correr en el navegador

```powershell
npm run dev
```

**http://localhost:5173**

## 3 · Compilar

```powershell
npm run lint
npm run build
```

Salida en `RavEatApp/dist/`.

## 4 · Llevarlo al teléfono

Con el teléfono conectado y la depuración USB activada:

```powershell
Set-Location .\RavEatApp
npm run build
npx cap run android
```

`cap run` sincroniza, compila, instala y abre la app. Pregunta el dispositivo con una lista.

```powershell
npx cap run android --list           # ver los ids
npx cap run android --target <id>    # ir directo a ese
```

> ⚠️ **`cap run` no compila la web.** Sincroniza lo que hay en `dist/`, así que sin `npm run build`
> antes el APK lleva el código de la vez anterior, sin avisar. Es el error más frecuente.

### Ver qué dice la app mientras corre

```powershell
adb logcat -s Capacitor:V chromium:V *:E
```

### Si falla en el medio

`cap run` hace tres cosas seguidas. Por separado se ve en cuál:

```powershell
npx cap sync android      # 1 · copia dist/ al proyecto Android
Set-Location .\android
.\gradlew installDebug    # 2 · compila el APK y lo instala
adb devices               # 3 · el teléfono tiene que estar en la lista
```

`INSTALL_FAILED_UPDATE_INCOMPATIBLE` significa que la app instalada se compiló en otra máquina y su
firma no coincide:

```powershell
adb uninstall app.raveat
```

## 5 · Abrir en Android Studio

```powershell
npx cap open android
```

Opcional: el paso 4 hace el ciclo entero por terminal. Sirve cuando falla el lado nativo — **Logcat**
para los errores del contenedor, **Build → Output** para los de Gradle.

Para el lado web el que sirve es Chrome: `chrome://inspect` con la app corriendo, y se elige la
webview de `app.raveat`. **Logcat ve el contenedor, `chrome://inspect` ve la web que está adentro.**

## 6 · Generar el APK como archivo

```powershell
Set-Location .\RavEatApp\android
.\gradlew assembleDebug
```

Queda en `android/app/build/outputs/apk/debug/`.

## 7 · Íconos y splash · nuevo en esta versión

Las imágenes fuente van en `RavEatApp/assets/`, en PNG y cuadradas:

| Archivo | Genera |
|---|---|
| `icon-only.png` | el ícono de la app y los favicones |
| `icon-foreground.png` · `icon-background.png` | las dos capas del adaptive icon de Android |
| `splash.png` · `splash-dark.png` | la pantalla de arranque, claro y oscuro |

```powershell
Set-Location .\RavEatApp
npx @capacitor/assets generate --android
npm run build
npx cap run android
```

`npx` pregunta si instala la herramienta: se acepta con `y`, es de un solo uso. `generate` escribe
los tamaños de `android/app/src/main/res/` y los favicones de `public/` — no se editan a mano, la
próxima corrida los pisa. Funcionó si `git status` muestra los `mipmap-*` y `splash` modificados.

### Si falla con `Something went wrong installing the "sharp" module`

Pasa con npm 11+ (el de Node 24): npm bloquea por seguridad los scripts de instalación, y `sharp`
descarga su binario en uno. Se aprueba una vez y no vuelve a pasar:

```powershell
npm install --no-save @capacitor/assets
npm install-scripts approve sharp
npm rebuild sharp --foreground-scripts
npx capacitor-assets generate --android
```

> ⚠️ **El ícono viaja adentro del APK**: hasta que `cap run` compile e instale, el teléfono muestra
> el viejo. Si sobrevive igual, es el caché del launcher: `adb uninstall app.raveat` y de nuevo
> `npx cap run android`.

El comportamiento del splash está en `capacitor.config.json`:

```json
"SplashScreen": { "launchAutoHide": true, "launchFadeOutDuration": 300, "androidScaleType": "CENTER_CROP", "showSpinner": false }
```

`launchAutoHide: true` lo cierra solo a los 500 ms. Cuando el arranque haga trabajo real —leer el
token, pedir biometría— pasa a `false` y lo cierra la app, ya con la primera pantalla dibujada.

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

**Gradle** es el motor de build; el **Android Gradle Plugin** le enseña qué es un APK. Son dos
versiones distintas y se confunden seguido. Siempre se invoca `.\gradlew`, nunca `gradle`: el
*wrapper* fija la versión y por eso el build es reproducible.

Cada capacidad nativa suma su permiso cuando llega: cámara y galería en `version-06`, ubicación en
`version-07`.

`android/local.properties` **no se versiona**: lo genera Android Studio en cada máquina.

### Firma

El APK de depuración usa el `debug.keystore` que Android Studio genera solo en cada máquina, en
`$env:USERPROFILE\.android\debug.keystore`. No hay nada que configurar.

El keystore de **release** —el que firma para publicar— tiene una sola regla: **nunca se
versiona.** El `.gitignore` ya lo corta.
