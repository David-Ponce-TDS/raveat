# U13 · iOS: la misma app en el iPhone y en TestFlight

> El código de la app no cambia: cambia el sistema que la recibe. iOS pide permisos con
> explicación, firma con una cuenta paga y no deja instalar nada que no haya pasado por Apple.

| | |
|---|---|
| **Rama** | `version-13` |
| **Viene de** | `version-12` — identidad, acceso público y publicación en Google Play |
| **Stack** | Ionic Vue 3 · Capacitor 8.5 (+ `@capacitor/ios`) · CocoaPods · Xcode 27 + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador, Android y **iPhone** (iOS 15.5 o posterior), instalada desde **TestFlight** |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) + una **Mac** con Xcode y CocoaPods |
| **Comandos** | [`README.comandos.md`](README.comandos.md) para la API y Android; los de iOS están acá |

---

## 🎯 Objetivo

**Llevar RavEat al iPhone sin tocar la lógica de la app.** Capacitor genera el proyecto de Xcode
igual que generó el de Android; el trabajo está en lo que iOS exige alrededor: las descripciones de
cada permiso, la firma con un equipo pago, la declaración de cifrado y el camino de la tienda, que
en Apple pasa sí o sí por **App Store Connect** y **TestFlight**.

---

## 🔄 Qué cambia desde la versión 12

| | `version-12` | `version-13` |
|---|---|---|
| **Plataformas** | navegador y Android | + **iOS**: `RavEatApp/ios/`, versionado igual que `android/` |
| **Dependencias nativas** | Gradle | + **CocoaPods** (dos plugins no soportan SPM) |
| **Ciclo de vida** | `AppDelegate` | + **`SceneDelegate`**: iOS 26 lo exige, y sin él la app se cierra al arrancar |
| **Permisos** | `AndroidManifest.xml` | + ocho `NS…UsageDescription` en `Info.plist`, en español y diciendo para qué |
| **Entrar con Google** | plugin propio en Java | + su par en **Swift** (`GoogleSignIn` por CocoaPods) y un cliente OAuth de tipo iOS |
| **Push** | token de FCM | el token de Apple no sirve: se pide el de FCM con `FirebaseMessaging`, una clave APNs y el `GoogleService-Info.plist` |
| **Abrir la app desde el QR** | App Links (`assetlinks.json`) | + **Universal Links** (`apple-app-site-association` y *Associated Domains*) |
| **La prueba antes de la tienda** | prueba interna de Google Play | + **TestFlight**, grupo de prueba interna |
| **Versión** | `versionName 1.1.0` · `versionCode 3` | iOS: `MARKETING_VERSION 1.1.0` · `CURRENT_PROJECT_VERSION 9` |

La API no cambia.

---

## 🧰 Qué hay que instalar

iOS **solo se compila en una Mac**. Antes de instalar, ver qué hay:

```zsh
for r in "Node:node --version" "Xcode:xcodebuild -version" "xcode-select:xcode-select -p" \
         "CocoaPods:pod --version" "git:git --version"; do
  nombre="${r%%:*}"; cmd="${r#*:}"
  if v=$(eval "$cmd" 2>/dev/null | head -1) && [ -n "$v" ]; then printf '[x] %-13s · %s\n' "$nombre" "$v"
  else printf '[ ] %-13s · FALTA\n' "$nombre"; fi
done
```

| Pieza | Si falta |
|---|---|
| Xcode | de la Mac App Store; después `sudo xcode-select -s /Applications/Xcode.app` |
| CocoaPods | `brew install cocoapods` |
| Cuenta de Apple Developer **paga** | <https://developer.apple.com/enroll/> — 99 USD por año. La gratuita corre en el propio iPhone 7 días, pero no llega a TestFlight |

Son comandos de **zsh de macOS**: iOS no existe en Windows.

---

## 📚 Temas

1. Una base de código, tres plataformas: qué es de Capacitor y qué es de cada sistema
2. `Info.plist`: sin la descripción de uso, iOS **cierra la app** en el momento de pedir el permiso
3. Gestores de dependencias nativas: SPM y CocoaPods, y por qué un plugin decide cuál se usa
4. El *deployment target*: la versión mínima de iOS la impone la dependencia más exigente
5. El ciclo de vida de escenas (`UIScene`), obligatorio desde iOS 26, y cómo leer un informe de fallo
6. Firma en Apple: equipo, App ID, certificado de distribución y perfil, con firma automática
7. App Store Connect: la ficha de la app existe **antes** que el primer build
8. TestFlight: prueba interna (miembros del equipo, sin revisión) y externa (con revisión beta)
9. Un plugin propio es por plataforma: lo que se escribió en Java no existe en iOS
10. Lo que el iPhone guarda distinto: el llavero sobrevive a la desinstalación y la foto llega en HEIC

---

## 🍎 Agregar iOS

```zsh
cd ~/Dev/raveat/RavEatApp
npm ci
npm run build:release                                # la URL de producción queda grabada
export LANG=en_US.UTF-8                              # CocoaPods se queja sin UTF-8
npx cap sync ios                                     # copia la web y corre pod install
npx cap open ios                                     # abre App.xcworkspace, no el .xcodeproj
```

En Xcode: target **App** → **Signing & Capabilities** → *Automatically manage signing* → **tu
equipo**. El repositorio no trae ninguno: Xcode lo graba en `project.pbxproj` como
`DEVELOPMENT_TEAM`, y ese cambio **no se commitea**. También se cambia el *Bundle Identifier*
(`app.raveat`) por uno propio, como el `applicationId` de Android.

Así se generó la carpeta la primera vez, por si hay que rehacerla:

```zsh
npm install @capacitor/ios@8.5.3 --save-exact        # la misma versión que @capacitor/core
npx cap add ios --packagemanager CocoaPods
sed -i '' "s/platform :ios, '15.0'/platform :ios, '15.5'/" ios/App/Podfile
sed -i '' 's/IPHONEOS_DEPLOYMENT_TARGET = 15.0;/IPHONEOS_DEPLOYMENT_TARGET = 15.5;/' ios/App/App.xcodeproj/project.pbxproj
(cd ios/App && pod install --repo-update)
npx --yes --package @capacitor/assets@3.0.5 capacitor-assets generate --ios
```

---

## 🧩 Por qué CocoaPods y no SPM

Capacitor 8 arma iOS con **Swift Package Manager** por defecto. `cap add ios` lo avisa así:

```
[warn] @capacitor-community/speech-recognition does not have a Package.swift
[warn] @capacitor-mlkit/barcode-scanning does not have a Package.swift
```

No es un aviso para ignorar: con SPM esos dos plugins **no entran en la app**, y el dictado y el QR
fallan en el iPhone sin ningún error de compilación. Con `--packagemanager CocoaPods` entran todos.

La segunda sorpresa viene de ML Kit: pide **iOS 15.5** y el proyecto nace con 15.0, así que
`pod install` falla con *required a higher minimum deployment target* hasta que se sube en los dos
lugares, el `Podfile` y el proyecto.

---

## 💥 Sin escenas, la app se cierra al arrancar

El primer build subido a TestFlight **se cerraba al abrirlo**, sin mostrar nada. El informe de
fallo del iPhone lo dice en una línea:

```
Exception Type:  EXC_BREAKPOINT (SIGTRAP)
Thread 0:  _UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption
```

Desde iOS 26, una app compilada con el SDK nuevo **tiene que adoptar el ciclo de vida de escenas**
(`UIScene`): la ventana ya no la maneja el `AppDelegate` sino un `SceneDelegate`. Capacitor 8.4
todavía genera el `AppDelegate` clásico, así que UIKit mata la app apenas crea la escena. En
Android no existe nada equivalente: es un requisito de la plataforma, no un error del código.

Se arregla con **Capacitor 8.5.3**, que trae `CAPSceneDelegateProxy` —el que reparte a los plugins
los enlaces y las URL que antes llegaban al `AppDelegate`—, más tres piezas:

```zsh
npm install --save-exact @capacitor/core@8.5.3 @capacitor/ios@8.5.3 \
  @capacitor/android@8.5.3 @capacitor/cli@8.5.3
npx cap sync ios
```

**1 · `ios/App/App/SceneDelegate.swift`** (nuevo): crea la ventana con el controlador de Capacitor.

```swift
import UIKit
import Capacitor

class SceneDelegate: UIResponder, UIWindowSceneDelegate {
    var window: UIWindow?

    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions) {
        guard let windowScene = scene as? UIWindowScene else { return }
        window = UIWindow(windowScene: windowScene)
        window?.rootViewController = CAPBridgeViewController()
        window?.makeKeyAndVisible()
        SceneDelegateProxy.shared.scene(scene, willConnectTo: session, options: connectionOptions)
    }

    func scene(_ scene: UIScene, openURLContexts URLContexts: Set<UIOpenURLContext>) {
        SceneDelegateProxy.shared.scene(scene, openURLContexts: URLContexts)
    }

    func scene(_ scene: UIScene, continue userActivity: NSUserActivity) {
        SceneDelegateProxy.shared.scene(scene, continue: userActivity)
    }
}
```

El archivo se agrega **al target App** en Xcode (arrastrarlo al grupo `App`, con «App» tildado en
*Target Membership*); si no, compila igual pero la clase no existe en tiempo de ejecución.

**2 · `AppDelegate.swift`**: decirle a UIKit quién maneja la escena.

```swift
func application(_ application: UIApplication,
                 configurationForConnecting connectingSceneSession: UISceneSession,
                 options: UIScene.ConnectionOptions) -> UISceneConfiguration {

    let config = UISceneConfiguration(name: "Default Configuration", sessionRole: connectingSceneSession.role)
    config.delegateClass = SceneDelegate.self
    return config
}
```

**3 · `Info.plist`**: el manifiesto de escenas, al lado de las otras claves.

```xml
<key>UIApplicationSceneManifest</key>
<dict>
	<key>UIApplicationSupportsMultipleScenes</key>
	<false/>
	<key>UISceneConfigurations</key>
	<dict>
		<key>UIWindowSceneSessionRoleApplication</key>
		<array>
			<dict>
				<key>UISceneConfigurationName</key>
				<string>Default Configuration</string>
				<key>UISceneDelegateClassName</key>
				<string>$(PRODUCT_MODULE_NAME).SceneDelegate</string>
				<key>UISceneStoryboardFile</key>
				<string>Main</string>
			</dict>
		</array>
	</dict>
</dict>
```

> **Cómo se leyó el informe de fallo.** Con el iPhone conectado por cable: `xcrun devicectl device
> info apps --device <UDID>` confirma que está instalada, `xcrun devicectl device process launch
> --device <UDID> --console app.raveat` la abre y muestra qué pasa, y los `.ips` del teléfono se
> copian con `idevicecrashreport`. En Xcode es *Window → Devices and Simulators → View Device Logs*.

---

## 🔑 Entrar con Google en iOS

El plugin de Android está escrito en Java: en iOS no existe y hay que escribir su par en **Swift**.
Del lado del JavaScript no cambia nada —`registerPlugin('GoogleAuth')`, `signIn` y `signOut`—, y
devuelve lo mismo: el `id_token` que valida la API.

**1 · Un cliente OAuth de tipo iOS**, en Google Cloud → *Google Auth Platform* → *Clientes* → *Crear
cliente* → **iOS**, con el *Bundle ID* (`app.raveat`) y el *Team ID* de la cuenta de Apple. Es un
cliente más del mismo proyecto que el web y el de Android; el web sigue siendo el que pide el token.

**2 · El SDK**, en el `Podfile`, dentro del bloque del target y no en `capacitor_pods` (que
`cap sync` reescribe y se llevaría el pod):

```ruby
target 'App' do
  capacitor_pods
  pod 'GoogleSignIn', '~> 9.1'
end
```

**3 · `Info.plist`**: el cliente y su esquema invertido, por donde Google devuelve el control.

```xml
<key>GIDClientID</key>
<string>$(GOOGLE_IOS_CLIENT).apps.googleusercontent.com</string>
<key>CFBundleURLTypes</key>
<array>
	<dict>
		<key>CFBundleURLSchemes</key>
		<array>
			<string>com.googleusercontent.apps.$(GOOGLE_IOS_CLIENT)</string>
		</array>
	</dict>
</array>
```

El esquema es el ID del cliente **al revés**: `com.googleusercontent.apps.` seguido de la parte que
va antes de `.apps.googleusercontent.com`.

Esa parte no se escribe en el `Info.plist` sino en el ajuste `GOOGLE_IOS_CLIENT`: en Xcode, target
**App** → *Build Settings* → *User-Defined*. Igual que el equipo, es el de cada uno y **no se
commitea**.

**4 · `GoogleAuthPlugin.swift`**: configura `GIDConfiguration(clientID:, serverClientID:)` con el
cliente de iOS y el **web** como `serverClientID` —de ahí sale la audiencia del `id_token`— y llama
a `GIDSignIn.sharedInstance.signIn(withPresenting:)`. Cancelar devuelve el código `12501`, el mismo
que Android, para que el JavaScript lo trate igual.

**5 · Registrarlo.** Un plugin propio no lo descubre nadie: se da de alta en un controlador que
extiende el de Capacitor, y el `SceneDelegate` usa ese controlador.

```swift
class RavEatViewController: CAPBridgeViewController {
    override func capacitorDidLoad() {
        bridge?.registerPluginInstance(GoogleAuthPlugin())
    }
}
```

Y el `SceneDelegate` le pasa a Google los enlaces que le tocan:

```swift
func scene(_ scene: UIScene, openURLContexts URLContexts: Set<UIOpenURLContext>) {
    for contexto in URLContexts { _ = GIDSignIn.sharedInstance.handle(contexto.url) }
    SceneDelegateProxy.shared.scene(scene, openURLContexts: URLContexts)
}
```

Los dos archivos nuevos se agregan **al target App**. Mientras la pantalla de consentimiento esté en
modo *Testing*, solo entran las cuentas que figuren en *Público → Usuarios de prueba*.

---

## 🔗 Universal Links: el QR abre la app

Es el par de los App Links de Android. El QR del pedido es `https://<dominio>/p/<token>`; con esto
iOS abre la app en vez del navegador. Hacen falta dos cosas, una de cada lado:

**La API**, `/.well-known/apple-app-site-association` (sin extensión, `application/json`):

```json
{"applinks":{"details":[{"appIDs":["TEAMID.app.raveat"],"components":[{"/":"/p/*"}]}]}}
```

**La app**, el permiso *Associated Domains* en `App.entitlements`, enganchado al target con
`CODE_SIGN_ENTITLEMENTS`:

```xml
<key>com.apple.developer.associated-domains</key>
<array>
	<string>applinks:raveat.runasp.net</string>
</array>
```

El Team ID no va en el código: en el panel de MonsterASP, `Publico__TeamIdApple` = **tu** Team ID
(developer.apple.com → *Membership details*). Sin esa variable, el archivo da 404.

La firma automática registra la capacidad en la cuenta de Apple sola. Para comprobar que el archivo
quedó bien servido:

```zsh
curl -s -i https://raveat.runasp.net/.well-known/apple-app-site-association | grep -iE "^HTTP|content-type|appIDs"
```

Las claves tienen que ser exactamente `appIDs` y `/`: una política de nombres global que las pase a
`snake_case` hace que iOS ignore el archivo sin ningún aviso.

---

## 🔔 Push en iOS: el token tiene que ser el de FCM

La API manda el aviso por **FCM** (Firebase Cloud Messaging) y FCM lo entrega por **APNs**, el
servicio de Apple. En Android el token que da el teléfono ya es el de FCM. En iPhone el plugin
entrega el **token crudo de Apple**, y la API no puede usarlo: FCM lo rechaza. Hay que pedirle a
Firebase el suyo, con tres cosas:

**1 · Una clave APNs**, en developer.apple.com → *Certificates, Identifiers & Profiles* → **Keys** →
nueva clave con *Apple Push Notifications service (APNs)* y entorno **Sandbox y Producción**
(TestFlight usa el de producción; Xcode, el de pruebas, y esto no se puede cambiar después de
crearla). Se baja un `.p8` **una sola vez**: se guarda fuera del repositorio.

**2 · Firebase**, en el mismo proyecto que Android: registrar la app iOS con el bundle id, bajar
`GoogleService-Info.plist` y, en *Configuración → Cloud Messaging → Configuración de la app para
Apple*, subir la clave `.p8` con su *Key ID* y el *Team ID*, para desarrollo y para producción.

**3 · La app.** El `.plist` va en `ios/App/App/` y **no se versiona**, igual que
`google-services.json`. Una fase de compilación lo copia al paquete solo si existe: sin él la app
compila y arranca, sin push.

```ruby
# Podfile, en el bloque del target
pod 'FirebaseMessaging'
```

```swift
// AppDelegate.swift
func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
    if Bundle.main.path(forResource: "GoogleService-Info", ofType: "plist") != nil {
        FirebaseApp.configure()
    }
    return true
}

func application(_ application: UIApplication, didRegisterForRemoteNotificationsWithDeviceToken deviceToken: Data) {
    guard FirebaseApp.app() != nil else {
        NotificationCenter.default.post(name: .capacitorDidRegisterForRemoteNotifications, object: deviceToken)
        return
    }
    Messaging.messaging().apnsToken = deviceToken
    Messaging.messaging().token { token, error in
        if let error = error {
            NotificationCenter.default.post(name: .capacitorDidFailToRegisterForRemoteNotifications, object: error)
        } else if let token = token {
            NotificationCenter.default.post(name: .capacitorDidRegisterForRemoteNotifications, object: token)
        }
    }
}
```

Y el permiso en `App.entitlements`: `aps-environment` en `development`; Xcode lo pasa a `production`
al exportar para TestFlight. Del lado de la API, el mensaje lleva la configuración de Apple
—prioridad 10 y sonido— para que el aviso salga al instante:

```csharp
Apns = new ApnsConfig {
    Headers = new Dictionary<string, string> {["apns-priority"] = "10"},
    Aps = new Aps {Sound = "default"}
}
```

---

## 🧪 El simulador no sirve para esta app

ML Kit no publica binario para el simulador en Macs con Apple Silicon: el pod excluye `arm64`, la
app sale `x86_64` y el simulador actual se niega a instalarla con *«Se necesita actualizar
RavEat»*. No es un error de configuración: con el QR adentro, **RavEat se prueba en el iPhone**,
conectado por cable desde Xcode o instalado desde TestFlight.

---

## 🔐 `Info.plist`: cada permiso dice para qué

| Clave | Lo usa |
|---|---|
| `NSCameraUsageDescription` | `camara_service` y `qr_service` |
| `NSPhotoLibraryUsageDescription` | elegir la foto de un producto desde la galería |
| `NSPhotoLibraryAddUsageDescription` | guardar en la galería la foto que saca la cámara |
| `NSFaceIDUsageDescription` | `biometria_service` |
| `NSMicrophoneUsageDescription` · `NSSpeechRecognitionUsageDescription` | `dictado_service` |
| `NSLocationWhenInUseUsageDescription` | `geolocalizacion_service` |
| `NSLocationAlwaysAndWhenInUseUsageDescription` | la pide App Store Connect porque el plugin de ubicación la menciona, aunque la app solo usa la ubicación abierta |
| `ITSAppUsesNonExemptEncryption = false` | evita la pregunta de exportación en cada build: solo HTTPS y el cifrado del sistema |

Molde para un permiso nuevo:

```xml
<key>NS…UsageDescription</key>
<string>RavEat usa … para …</string>
```

Apple rechaza los textos genéricos («la app necesita la cámara»): se dice qué hace la app con eso.

---

## 🔒 Face ID al abrir, y lo que el iPhone guarda distinto

**Face ID al abrir.** En el perfil, *Pedir Face ID al abrir* guarda una preferencia en el
almacenamiento seguro. Con eso encendido, `bloqueo_service` pide la biometría **antes** de restaurar
la sesión; si se cancela, la app cae al login. Para encenderlo se confirma la identidad en ese
momento: mejor enterarse ahí de que la biometría no anda que con la app cerrada.

```js
// src/services/bloqueo_service.js · se llama una vez al abrir la app
export async function pasar_bloqueo_de_arranque(){
	if(!(await bloqueo_activado())) return true;
	if(!(await restaurar_sesion())) return true;
	if(!(await biometria_disponible())) return true;
	return verificar_identidad('Confirmá tu identidad para abrir RavEat');
}
```

Los textos dicen el nombre que usa el teléfono —*Face ID*, *Touch ID* o *huella*— con
`etiqueta_biometria()`, y no «huella» fijo.

**El llavero sobrevive a la desinstalación.** En Android, desinstalar borra todo. En el iPhone el
llavero (*Keychain*), donde el almacenamiento seguro guarda el token, **queda**: al reinstalar, la
app arrancaba con la sesión anterior. `limpiar_restos_de_instalacion_anterior()` deja una marca en
`localStorage`, que sí se borra con la app; si al abrir la marca no está, es una instalación nueva y
borra el token, el refresh y la preferencia de bloqueo.

**La foto llega en otro formato.** La cámara del iPhone entrega **HEIC** y originales de varios
megas, y la API acepta JPG, PNG o WEBP de hasta 5 MB. `camara_service` reencoda siempre a **JPEG
de hasta 1600 px** antes de subir, en las dos plataformas.

---

## 🚀 Del Mac a TestFlight

**1 · La ficha.** <https://appstoreconnect.apple.com> → *Apps* → **+** → *Nueva app*: iOS, nombre,
idioma principal, el ID de pack (`app.raveat`, que la firma automática ya registró), un SKU y
acceso ilimitado. Sin la ficha, la subida falla con *App record … not found*.

**2 · El build**, sin abrir Xcode:

```zsh
cd ~/Dev/raveat/RavEatApp/ios/App
xcodebuild -workspace App.xcworkspace -scheme App -configuration Release \
  -destination 'generic/platform=iOS' -archivePath ~/Desktop/RavEat.xcarchive \
  -allowProvisioningUpdates DEVELOPMENT_TEAM=TU_TEAM_ID GOOGLE_IOS_CLIENT=TU_CLIENTE_IOS archive
```

**3 · Subirlo.** `export.plist`, con tu Team ID:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>method</key><string>app-store-connect</string>
	<key>destination</key><string>upload</string>
	<key>teamID</key><string>TU_TEAM_ID</string>
	<key>signingStyle</key><string>automatic</string>
</dict>
</plist>
```

```zsh
xcodebuild -exportArchive -archivePath ~/Desktop/RavEat.xcarchive \
  -exportOptionsPlist export.plist -exportPath ~/Desktop/ipa -allowProvisioningUpdates
```

Con `destination` en `export` deja el `.ipa` en vez de subirlo (para entregarlo con Transporter).
`-allowProvisioningUpdates` usa la cuenta iniciada en Xcode → *Settings* → *Accounts*: crea solo el
certificado de distribución y el perfil.

**4 · TestFlight.** El build aparece en *TestFlight* como **Procesando** y queda listo a los pocos
minutos. *Pruebas internas* → **+** → un grupo con *distribución automática* → los testers, que
tienen que ser usuarios del equipo en *Usuarios y acceso*. Les llega el mail y lo instalan con la
app **TestFlight** del iPhone.

Cada build nuevo necesita un `CURRENT_PROJECT_VERSION` mayor que el anterior: igual que el
`versionCode` de Play, Apple rechaza un número repetido.

---

## 📁 Cómo está organizado

```
RavEatApp/
├─ ios/App/
│  ├─ Podfile                          los plugins como pods; platform :ios, '15.5'
│  ├─ Podfile.lock                     versiones exactas de los pods (se versiona)
│  ├─ App.xcworkspace                  lo que se abre en Xcode
│  └─ App/
│     ├─ AppDelegate.swift             + configurationForConnecting: la escena la maneja SceneDelegate
│     ├─ SceneDelegate.swift           crea la ventana con RavEatViewController (iOS 26)
│     ├─ RavEatViewController.swift    registra los plugins propios
│     ├─ GoogleAuthPlugin.swift        entrar con Google, el par en Swift del plugin de Android
│     ├─ App.entitlements              Associated Domains (Universal Links) y aps-environment (push)
│     ├─ GoogleService-Info.plist      lo baja Firebase; no se versiona
│     ├─ Info.plist                    permisos, cifrado, idioma, orientación y el manifiesto de escenas
│     └─ Assets.xcassets/              ícono y splash, generados desde assets/
└─ src/services/
   ├─ google_auth_service.js           el mismo servicio en las dos plataformas
   ├─ bloqueo_service.js               Face ID (o huella) al abrir, si está encendido en el perfil
   ├─ token_service.js                 + borra la sesión que el llavero guardó de otra instalación
   └─ camara_service.js                + la foto sale siempre en JPEG de hasta 1600 px
```

Lo que **no** se versiona lo regenera `npx cap sync ios`: `ios/App/Pods/`, `ios/App/App/public/` y
`capacitor.config.json` dentro de `ios/` (ver `ios/.gitignore`).

---

## ✅ Terminaste cuando

- [ ] `npx cap sync ios` termina sin avisos de SPM y con `pod install` exitoso
- [ ] El archive se firma con **tu** equipo y **tu** bundle id
- [ ] La app **abre** en el iPhone y no se cierra sola: el `SceneDelegate` y el plugin están en el target
- [ ] La ficha de la app existe en App Store Connect
- [ ] El build aparece en TestFlight y pasa de *Procesando* a listo
- [ ] Un grupo de prueba interna con vos como tester, y la app instalada desde TestFlight
- [ ] En el iPhone, cada permiso aparece en español diciendo para qué: cámara, ubicación, micrófono y Face ID
- [ ] Con *Pedir Face ID al abrir* encendido en el perfil, la app pide Face ID antes de mostrar la sesión
- [ ] Desinstalar y volver a instalar la app arranca en el login, sin la sesión anterior
- [ ] Una foto sacada con el iPhone se sube sin error
- [ ] *Entrar con Google* abre la pantalla de cuentas en el iPhone y entra con un email dado de alta
- [ ] Escanear el QR de un pedido con la cámara abre la app, no el navegador
- [ ] Una acción que genera un aviso hace sonar el iPhone aunque la app esté cerrada
- [ ] `npm run lint` y `npm run build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
