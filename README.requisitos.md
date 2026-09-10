# Requisitos · versión 4

**Nada nuevo.** El entorno es el mismo de la versión 3: la versión agrega entidades, endpoints y
pantallas, no herramientas.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code
- [x] .NET SDK 10
- [x] MySQL Server 8

## Comprobarlo

Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; dotnet='--version'; mysql='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-7} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

Si algo dice `FALTA`: la instalación base (Git, Node, JDK, Android SDK, VS Code) está en
`README.requisitos.md` de `version-01`; .NET SDK y MySQL, con su configuración, en el de
`version-03`. Si acabás de instalarlo, cerrá la terminal y abrí una nueva: el `PATH` se lee al
abrir la sesión.

MySQL además tiene que estar **corriendo**: `Get-Service MySQL*` tiene que decir `Running`.
