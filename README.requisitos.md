# Requisitos · versión 8

**Nada nuevo que instalar en la máquina.** El plugin de red entra con `npm install` y `keytool`
—el que genera el keystore— vino con el JDK de Android Studio. Lo nuevo se **genera** o se **crea
en la web**: el keystore y una cuenta gratuita.

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
- [ ] **Keystore de release** — se genera una vez y se guarda para siempre. Nuevo en esta versión
- [ ] **Cuenta de MonsterASP.NET** — para publicar la API y MySQL. Nuevo en esta versión

**La cuenta es opcional para cursar.** Sin ella se llega hasta el APK firmado; con ella, ese APK
habla con tu propia API.

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
