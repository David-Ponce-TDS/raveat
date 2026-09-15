# Requisitos · versión 6

**Nada nuevo que instalar en la máquina.** Los cinco plugins de esta versión entran con
`npm install`, y QuestPDF y QRCoder con `dotnet build`. Lo nuevo es el teléfono.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code
- [x] .NET SDK 10
- [x] MySQL Server 8
- [ ] **Teléfono Android con cámara**, con la depuración USB activada — nuevo en esta versión

Cámara, escáner y compartir **no existen en el navegador**: se prueban en el APK. El mismo teléfono
sirve para la huella; el QR se puede escanear desde la pantalla de la PC o de otro teléfono.

## Comprobarlo

Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; dotnet='--version'; mysql='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-7} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
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

Si algo dice `FALTA`: la instalación base está en `README.requisitos.md` de `version-01`; .NET SDK
y MySQL, con su configuración, en el de `version-03`. Si acabás de instalarlo, cerrá la terminal y
abrí una nueva: el `PATH` se lee al abrir la sesión.

MySQL además tiene que estar **corriendo**: `Get-Service MySQL*` tiene que decir `Running`.
