# Requisitos · versión 11

**Nada nuevo que instalar**: ni programa ni plugin. La preparación y el historial son pantallas y
endpoints nuevos sobre lo que ya está, y la firma es un `<canvas>` del navegador. La base suma tres
migraciones, que la API aplica sola al arrancar.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio, y trae `keytool`
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code
- [x] .NET SDK 10
- [x] MySQL Server 8
- [x] Teléfono Android con cámara, con la depuración USB activada
- [x] Proyecto de Firebase con `google-services.json` descargado
- [x] Un segundo dispositivo, o una segunda sesión
- [x] Keystore de release, generado en la versión 8
- [x] Cuenta de MonsterASP.NET, opcional: sin ella se llega hasta el APK firmado
- [x] `cropperjs` — entra con `npm install` en `RavEatApp`
- [x] Cuenta de servicio de Firebase — el JSON con clave privada, cargado en User Secrets
- [x] `@capacitor-community/speech-recognition` — entra con `npm install` en `RavEatApp`
- [x] Reconocimiento de voz en el teléfono — la app de Google, con el idioma español

## Comprobarlo

Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; dotnet='--version'; mysql='--version'; java='-version'; adb='version'; keytool='-help'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-8} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

Si `keytool` dice `FALTA`, está igual: vive en la carpeta del JDK de Android Studio aunque no
figure en el `PATH`.

```powershell
Test-Path "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe"
```

Lo propio de esta versión: el keystore y su archivo de propiedades.

```powershell
if (Test-Path .\RavEatApp\android\raveat-release.jks) { 'keystore            ok' } else { 'keystore            FALTA' }
if (Test-Path .\RavEatApp\android\keystore.properties) { 'keystore.properties ok' } else { 'keystore.properties FALTA' }
```

Cómo se generan: [`README.comandos.md`](README.comandos.md), sección 8.

La cuenta de servicio, de la versión 9:

```powershell
$ruta = dotnet user-secrets list --project .\RavEat.Api | Select-String 'Firebase:CuentaDeServicio' | ForEach-Object { ($_ -split ' = ', 2)[1] }
if ($ruta -and (Test-Path $ruta)) { 'cuenta de servicio  ok' } else { 'cuenta de servicio  FALTA' }
```

Cómo se baja y se carga: [`README.comandos.md`](README.comandos.md), sección 11.

El dictado, de la versión 10: el plugin, desde `RavEatApp`, y el servicio de voz del teléfono, con
el teléfono conectado. Si la segunda línea no lista ningún servicio, el micrófono no va a aparecer.

```powershell
npm ls @capacitor-community/speech-recognition
adb shell cmd package query-services -a android.speech.RecognitionService
```

Si falta el servicio, se instala o se actualiza **Google** desde Play Store. En el navegador de la
PC, el dictado anda en Chrome y en Edge; en Firefox el micrófono no aparece, y es lo esperado.

## La cuenta de esta versión

| Cuenta | Para qué | Costo |
|---|---|---|
| MonsterASP.NET | publicar la API y MySQL | gratis |

Los pasos están en [`README.comandos.md`](README.comandos.md), sección 10.

## Si algo dice FALTA

La instalación base está en `README.requisitos.md` de `version-01`; .NET SDK y MySQL, con su
configuración, en el de `version-03`; el proyecto de Firebase, en el de `version-07`. Si acabás de
instalar algo, cerrá la terminal y abrí una nueva: el `PATH` se lee al abrir la sesión.

MySQL además tiene que estar **corriendo**: `Get-Service MySQL*` tiene que decir `Running`.
