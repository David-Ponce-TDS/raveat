# Comandos · versión 1

Todo lo que se tipea para poner la app a andar. Los comandos son para **PowerShell en Windows** y se
ejecutan desde la raíz del repositorio salvo que se indique otra cosa.

Da por sentado que el entorno ya está instalado. Si no, empezá por
[`README.requisitos.md`](README.requisitos.md).

---

## 1 · Instalar

```powershell
Set-Location .\RavEatApp
npm install
```

## 2 · Correr en el navegador

```powershell
npm run dev
```

Queda en **http://localhost:5173**.

## 3 · Compilar

```powershell
npm run lint
npm run build
```

El resultado queda en `RavEatApp/dist/`.

## 4 · Llevarlo al teléfono

Con el teléfono conectado y la depuración USB activada:

```powershell
Set-Location .\RavEatApp
npm run build
npx cap run android
```

Eso es todo. **`cap run` sincroniza, compila el APK, lo instala y abre la app en el teléfono.** Te
pregunta en qué dispositivo con una lista; elegís con las flechas y Enter.

Para saltear la pregunta:

```powershell
npx cap run android --list           # ver los ids disponibles
npx cap run android --target <id>    # ir directo a ese
```

> ⚠️ **`cap run` no compila la web.** Sincroniza lo que encuentre en `dist/`, así que sin
> `npm run build` antes, el APK lleva el código de la vez anterior. Es el error más frecuente,
> y no da ningún aviso: la app abre, funciona, y muestra lo viejo.

### Ver qué dice la app mientras corre

```powershell
adb logcat -s Capacitor:V chromium:V *:E
```

Los mensajes de Capacitor, los `console.log` de la pantalla y cualquier error del sistema. `Ctrl+C`
para salir.

### Si algo falla en el medio

`cap run` hace tres cosas seguidas, y cuando falla no siempre dice en cuál. Correrlas por separado
te lo dice:

```powershell
npx cap sync android      # 1 · copia dist/ al proyecto Android
Set-Location .\android
.\gradlew installDebug    # 2 · compila el APK y lo instala
adb devices               # 3 · comprobar que el teléfono esté visible
```

A `installDebug` no le falta `assembleDebug`: Gradle resuelve las dependencias entre tareas y ya
arrastra el empaquetado del APK.

Si la instalación falla con `INSTALL_FAILED_UPDATE_INCOMPATIBLE`, la app que está en el teléfono se
compiló en otra máquina y su firma no coincide con la tuya:

```powershell
adb uninstall app.raveat
```

## 5 · Abrir en Android Studio

Para depurar del lado nativo. `npx cap open android` abre el proyecto Android en Android
Studio. Desde ahí se corre la app con **▶ Run** sobre el dispositivo conectado o un emulador.

No es obligatorio: el paso 4 hace el ciclo entero por terminal. Pero cuando algo falla del lado
nativo, Android Studio es lo que lo muestra.

| Herramienta | Dónde está | Para qué sirve |
|---|---|---|
| **Logcat** | panel inferior de Android Studio | los errores del lado nativo: permisos denegados, plugins que fallan, la app que se cierra sin decir nada |
| **Device Manager** | barra lateral derecha | crear y arrancar emuladores cuando no hay teléfono a mano |
| **Build → Output** | panel inferior | por qué falló Gradle, con el mensaje completo |

Para depurar el **lado web** —el HTML, el CSS y el JavaScript de la app— el que sirve es Chrome, no
Android Studio: con la app corriendo en el teléfono, abrir `chrome://inspect` en la computadora y
elegir la webview de `app.raveat`. Se obtiene el inspector completo, con consola, red y puntos de
interrupción, apuntando al código que está corriendo en el celular.

Los dos hacen falta y miran cosas distintas: **Logcat ve el contenedor, `chrome://inspect` ve la
web que está adentro.** Un error que no aparece en la consola de Chrome casi siempre está en Logcat.

## 6 · Generar el APK como archivo

El paso 4 instala en el teléfono conectado. Si lo que querés es el `.apk` suelto —para pasarlo por
otro medio o para guardarlo—:

```powershell
Set-Location .\RavEatApp\android
.\gradlew assembleDebug
```

Queda en `android/app/build/outputs/apk/debug/`.

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

**Gradle** es el motor de build y el **Android Gradle Plugin (AGP)** es lo que le enseña a Gradle qué
es un APK: son dos versiones distintas y se confunden seguido. `gradlew` —el *wrapper*— fija qué
versión de Gradle se usa, y por eso el build es reproducible en cualquier máquina. Siempre se invoca
`.\gradlew`, nunca `gradle`.

`android/local.properties`, que contiene `sdk.dir`, **no se versiona**: lo genera Android Studio en
cada máquina.

### Firma

Todo APK va firmado con un **keystore**, que Android usa como identidad de la app. Acá alcanza con el
`debug.keystore`: Android Studio lo genera solo en cada máquina, en
`$env:USERPROFILE\.android\debug.keystore`, y `assembleDebug` lo usa sin que haya que configurar
nada.

