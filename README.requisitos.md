# Requisitos · versión 7

**Nada nuevo que instalar en la máquina.** Los tres plugins de esta versión entran con
`npm install`. Lo nuevo es una **cuenta de Firebase** —gratis, y solo para el push— y un
**segundo dispositivo**.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code
- [x] .NET SDK 10
- [x] MySQL Server 8
- [x] Teléfono Android con cámara, con la depuración USB activada
- [ ] **Proyecto de Firebase** con `google-services.json` descargado — nuevo en esta versión
- [ ] **Un segundo dispositivo, o una segunda sesión** — nuevo en esta versión

**Los dos que faltan no son intercambiables.** Sin Firebase funciona todo menos el push. Sin un
segundo usuario no se ve nada: la bandeja se escribe para el interesado y nunca para el que ejecuta
la acción, así que trabajando solo con el administrador la campanita queda en cero para siempre. El
segundo puede ser otro teléfono, o el mismo navegador con otro usuario en una ventana privada.

## Comprobarlo

Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; dotnet='--version'; mysql='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-7} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

Lo propio de esta versión: el archivo de Firebase en su lugar y la bandera del `.env`.

```powershell
if (Test-Path .\RavEatApp\android\app\google-services.json) { 'google-services  ok' } else { 'google-services  FALTA' }
if (Select-String -Path .\RavEatApp\.env -Pattern '^VITE_PUSH_NOTIFICATIONS_ENABLED=false' -Quiet) { 'push en el .env  APAGADO' } else { 'push en el .env  ok' }
```

El teléfono, conectado por USB:

```powershell
adb devices
```

| Dice | Significa |
|---|---|
| `<serie>  device` | listo |
| `<serie>  unauthorized` | falta aceptar la depuración USB en la pantalla del teléfono |
| la lista vacía | el cable no transmite datos, o la depuración USB está apagada |

## El proyecto de Firebase

Se hace una vez, desde el navegador, y no lleva tarjeta. El archivo que se descarga al final es
**tuyo** y no se versiona: cada uno necesita el suyo.

1. Entrar a la consola de Firebase con una cuenta de Google y **crear un proyecto** — sin Analytics
   alcanza.
2. Agregar una **app Android**. El nombre del paquete tiene que ser exactamente el `applicationId`
   de la app: `app.raveat`.
3. Descargar **`google-services.json`** y dejarlo en `RavEatApp/android/app/`.
4. Reconstruir el APK: el archivo se lee **al compilar**, no al arrancar.

```powershell
Set-Location .\RavEatApp
npm run build
npx cap sync android
Set-Location .\android
.\gradlew installDebug
```

Si algo dice `FALTA`: la instalación base está en `README.requisitos.md` de `version-01`; .NET SDK
y MySQL, con su configuración, en el de `version-03`. Si acabás de instalarlo, cerrá la terminal y
abrí una nueva: el `PATH` se lee al abrir la sesión.

MySQL además tiene que estar **corriendo**: `Get-Service MySQL*` tiene que decir `Running`.
