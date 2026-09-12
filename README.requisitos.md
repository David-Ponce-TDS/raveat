# Requisitos · versión 2

**Nada nuevo.** El entorno es el mismo de la versión 1 y `package.json` no cambió.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code

## Comprobarlo

Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-6} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

Si algo dice `FALTA`, la instalación completa está en la versión 1 (`README.requisitos.md` de
`version-01`). Si acabás de instalarlo, cerrá la terminal y abrí una nueva: el `PATH` se lee al
abrir la sesión.
