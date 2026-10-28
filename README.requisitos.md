# Requisitos · versión 12

**Nada nuevo que instalar en la máquina.** Lo nuevo de esta versión son **credenciales**, no
programas: dos clientes OAuth de Google y, opcional, un proyecto de Supabase. El único paquete
nuevo, `@capacitor/app`, entra con `npm install`. La base suma una migración, que la API aplica sola
al arrancar.

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
- [ ] `@capacitor/app` — entra con `npm install` en `RavEatApp`
- [ ] **Dos clientes OAuth de Google**, uno **web** y uno **Android** — antes del martes
- [ ] Proyecto de Supabase con un bucket público `raveat`, opcional: sin él las fotos siguen en `wwwroot/uploads`
- [ ] Cuenta de desarrollador de Google Play, para subir el AAB a la prueba

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

Lo de esta versión: el paquete, desde `RavEatApp`, y los dos lugares donde va el web client id.

```powershell
npm ls @capacitor/app
if (Select-String -Path .\RavEatApp\.env -Pattern '^VITE_GOOGLE_WEB_CLIENT_ID=' -Quiet) { 'client id en .env    ok' } else { 'client id en .env    FALTA' }
if (dotnet user-secrets list --project .\RavEat.Api | Select-String 'GoogleAuth:WebClientId') { 'client id en la API  ok' } else { 'client id en la API  FALTA' }
```

## Las credenciales de Google

En Google Cloud → APIs y servicios → Credenciales hacen falta **dos** clientes OAuth, del mismo
proyecto:

| Cliente | Para qué | Dónde va su id |
|---|---|---|
| **Aplicación web** | es la audiencia del `id_token` que valida la API | `VITE_GOOGLE_WEB_CLIENT_ID` en `.env` y `GoogleAuth:WebClientId` en User Secrets |
| **Android** (`app.raveat` + huella SHA-1) | habilita la pantalla de cuentas en el teléfono | no se copia a ningún lado: vive en la consola |

Debug, release y Play firman distinto: cada huella que vaya a entrar con Google necesita su
cliente Android. Cómo se sacan las huellas: [`README.comandos.md`](README.comandos.md), sección 14.

## Las cuentas de esta versión

| Cuenta | Para qué | Costo |
|---|---|---|
| MonsterASP.NET | publicar la API y MySQL | gratis |
| Google Cloud | los dos clientes OAuth | gratis |
| Supabase | el almacén de fotos, opcional | gratis |
| Google Play Console | publicar el AAB | pago único |

Los pasos de MonsterASP están en la sección 10 de [`README.comandos.md`](README.comandos.md); los
de esta versión, en la 14.

## Si algo dice FALTA

La instalación base está en `README.requisitos.md` de `version-01`; .NET SDK y MySQL, con su
configuración, en el de `version-03`; el proyecto de Firebase, en el de `version-07`. Si acabás de
instalar algo, cerrá la terminal y abrí una nueva: el `PATH` se lee al abrir la sesión.

MySQL además tiene que estar **corriendo**: `Get-Service MySQL*` tiene que decir `Running`.
