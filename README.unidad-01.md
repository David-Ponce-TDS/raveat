# U1 · Entorno, proyecto y primer APK

> Inicio del proyecto móvil: configuración, tema propio y primer APK.

| | |
|---|---|
| **Rama** | `version-01` |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite |
| **Corre en** | navegador y teléfono Android |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo de la versión 1

Dejar el entorno funcionando y llevar un proyecto recién creado hasta un archivo `.apk` instalado en
el teléfono. Al terminar hay una app propia, con su tema claro y oscuro, abriéndose desde la
pantalla de inicio del celular.

El ícono y el splash siguen siendo los de fábrica de Capacitor, y está bien que así sea: **esta
versión resuelve que la app exista y corra**, no cómo se ve desde afuera.

**Una app móvil es una web empaquetada en un contenedor nativo.** El proyecto es una aplicación web
común, y Capacitor es lo que la mete adentro del `.apk` que el teléfono sabe instalar.

---

## 🧰 Qué hay que instalar

Cuatro programas: **Git**, **Node.js 22 o superior**, **Visual Studio Code** y **Android Studio**.
El JDK y el SDK de Android vienen con Android Studio, así que no se instalan aparte.

Los enlaces de descarga, el orden recomendado, cómo consultar qué tenés ya y cómo dejar
`ANDROID_HOME` y `adb` listos están en **[`README.requisitos.md`](README.requisitos.md)**.

---

## 📚 Temas de la unidad

1. Las herramientas y el rol de cada una: Node, JDK, Android SDK, editor
2. Crear el proyecto y recorrer la estructura de carpetas
3. Qué es Capacitor y cómo se relaciona con el proyecto web
4. La primera página y los componentes de Ionic
5. Tema propio: variables CSS y modo oscuro que se recuerda
6. Correr en el navegador
7. Agregar Android, primer build de Gradle y APK de depuración
8. El repositorio: git, `.gitignore` y qué nunca se sube

---

## 🖥️ Qué se ve en pantalla

Una sola página. De arriba abajo: el logo, una tarjeta de presentación, la carta con los productos
destacados y la lista de las piezas del stack. El botón de tema está en la barra superior, y la
elección entre claro y oscuro se mantiene al recargar.

El tema se aplica en **dos lugares**, y los dos hacen falta: un script en el `<head>` de
`index.html` lee `localStorage` y pone la clase `ion-palette-dark` antes de que cargue el bundle
—sin eso se ve un parpadeo blanco al abrir—, y `stores/app_store.js` mantiene el mismo valor para
que el botón sepa en qué estado está.

### Lo primero que se ve no es la web

En el teléfono, antes de la pantalla de inicio aparece un instante de **splash**. No lo dibuja
Ionic ni la app: lo dibuja **Android**, con una imagen del proyecto nativo, mientras la webview
todavía no existe. Es el contenedor tapando el hueco.

Lo maneja el plugin `@capacitor/splash-screen`, que no se importa en ningún archivo de `src/`: se
configura y ya, en `capacitor.config.json`.

```json
"SplashScreen": { "launchAutoHide": true, "launchFadeOutDuration": 300, "showSpinner": false }
```

Con `launchAutoHide: true` el splash se cierra solo a los 500 ms. Es lo correcto mientras la app
arranque instantáneamente, como acá. Cuando el arranque empiece a hacer trabajo real, esta bandera
pasa a `false` y el splash lo cierra la app a mano, ya con la primera pantalla dibujada.

**Ese instante es la idea de la unidad, en una imagen:** hay una parte nativa y una parte web, y se
turnan. El splash es lo único que ves mientras la segunda todavía no arrancó.

---

## 📁 Cómo está organizado

```
RavEatApp/
├─ src/
│  ├─ datos/      carta.js — los productos de la carta
│  ├─ router/     las rutas de la app
│  ├─ stores/     stores Pinia
│  ├─ theme/      global.css
│  ├─ views/      una página por ruta
│  ├─ App.vue     el contenedor raíz
│  └─ main.js     arranque de la app
├─ android/       proyecto Android generado por Capacitor
└─ public/seed/   las fotos de la carta
```

---

## 🔑 La firma de la app

**Android no instala un APK sin firma.** No es una política de una tienda: es del sistema
operativo, que usa la firma como identidad de la aplicación. Si el APK se instaló y abrió, estaba
firmado.

### En esta versión no hay nada que configurar

Buscá `signingConfigs` en `android/app/build.gradle` y no está. **La firma de depuración es
automática**: el Android Gradle Plugin se la aplica solo a la variante `debug`, con una clave que
el propio Android Studio genera la primera vez que compilás cualquier proyecto.

Esa clave no vive en el repositorio, sino en tu carpeta de usuario:

```
C:\Users\<tu-usuario>\.android\debug.keystore
```

No es del proyecto: es de tu máquina. Si mañana creás otra app sin relación con ésta, se firma con
el mismo archivo. Y si lo borrás, Android Studio te crea otro sin preguntar.

Su contraseña es pública y siempre la misma —alias `androiddebugkey`, contraseña `android`—, así
que se puede escribir en un comando sin ningún problema. Es lo que hace falta para leer su huella
digital, el dato que después piden los servicios de Google al registrar una app:

```powershell
keytool -list -v -keystore "$env:USERPROFILE\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
```

### Android exige que la identidad no cambie

Una app instalada **solo acepta actualizaciones firmadas con la misma clave** que la instaló. Es la
garantía central del modelo: sin ella, cualquiera podría publicar un paquete que se llame
`app.raveat` y reemplazar tu aplicación en el teléfono de otro. El nombre del paquete dice cómo se
llama la app; la firma dice quién la hizo, y es lo único que no se puede falsificar.

De ahí sale el mensaje que vas a ver el día que instales en un teléfono donde ya está la app
compilada por otra persona:

```
INSTALL_FAILED_UPDATE_INCOMPATIBLE
```

Android no está fallando: está haciendo su trabajo. Vio dos identidades distintas para el mismo
`app.raveat` y se negó a pisar una con la otra. Como las claves de depuración son por máquina,
alcanza con que dos personas compartan un teléfono para probar y aparece. Se resuelve sacando la
anterior, que es exactamente lo que Android pide:

```powershell
adb uninstall app.raveat
```

### El corolario: la clave es la app

Si la firma es la identidad, **perder la clave es perder la aplicación**. No hay trámite ni
recuperación: una app publicada que se firmó con una clave que ya no existe no se puede volver a
actualizar nunca. Se publica de nuevo, con otro nombre de paquete, desde cero, sin sus usuarios.

Eso convierte al archivo de la clave en el activo más frágil del proyecto, y explica las dos reglas
que valen desde hoy aunque todavía no exista ninguna clave propia:

**Se respalda fuera de la máquina que la generó.** Un disco que se rompe se lleva la app con él.

**No entra al repositorio.** El `.gitignore` ya corta `*.jks`, `*.keystore` y `keystore.properties`
desde esta primera versión. Quien tiene la clave puede publicar actualizaciones en nombre de la
app: subirla a un repositorio es entregar la aplicación, no solo un archivo.

---

## ✅ Terminaste cuando

- [ ] `npm install` termina sin errores
- [ ] La app abre en `http://localhost:5173` y se ve la pantalla de inicio
- [ ] El botón de tema alterna claro y oscuro, y la elección sobrevive a un recargado
- [ ] `npm run lint` y `npm run build` terminan sin errores
- [ ] `.\gradlew assembleDebug` genera el APK
- [ ] El APK instalado abre en el teléfono y el botón de tema funciona ahí también
- [ ] El proyecto abre en Android Studio y la app corre con **▶ Run**, sin errores en Logcat

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
