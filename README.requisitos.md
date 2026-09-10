# Requisitos · versión 3

**Dos programas nuevos: .NET SDK 10 y MySQL Server 8.** El resto es el mismo entorno de las
versiones 1 y 2. Los comandos son para **PowerShell en Windows**.

- [x] Git
- [x] Node.js 22 o superior — trae `npm`
- [x] JDK 21 — viene con Android Studio
- [x] Android SDK · `compileSdk 36` · `minSdk 24`
- [x] Visual Studio Code
- [ ] **.NET SDK 10** — nuevo en esta versión
- [ ] **MySQL Server 8** — nuevo en esta versión

---

## Antes de instalar: qué tenés ya

**Corré esto primero.** Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; dotnet='--version'; mysql='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-7} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

> Si acabás de instalar algo y sigue diciendo `FALTA`, **cerrá la terminal y abrí una nueva**: el
> `PATH` se lee al abrir la sesión.

La instalación completa de los ya marcados está en `README.requisitos.md` de `version-01`.

---

## Qué instalar

| # | Programa | Versión | Descarga | Para qué |
|---|---|---|---|---|
| 1 | **.NET SDK** | `10.x` | [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) | compilar y correr la API |
| 2 | **MySQL Server** | `8.x` | [dev.mysql.com/downloads](https://dev.mysql.com/downloads/) | la base de datos |
| 3 | **MySQL Workbench** · opcional | cualquiera reciente | [dev.mysql.com/downloads/workbench](https://dev.mysql.com/downloads/workbench/) | **ver** la base: tablas, datos, qué creó cada migración |

**El SDK, no el Runtime.** El Runtime solo ejecuta; para compilar hace falta el SDK. La versión
exacta la fija `global.json` en la raíz del repositorio: si la tuya no sirve, `dotnet build` lo
avisa con el número que espera.

**La página de MySQL empuja a crear una cuenta de Oracle. No hace falta**: abajo del botón de
login está el enlace **«No thanks, just start my download»**, que descarga directo.

**De MySQL alcanza el Server; Workbench es la ventana.** No es la base de datos ni la reemplaza:
es la interfaz gráfica para inspeccionarla. La terminal ejecuta; Workbench muestra. Durante la
instalación del Server:

- Anotá la contraseña de `root` — la vas a usar una sola vez, para crear la base y el usuario de
  la app.
- Dejá el puerto por defecto, `3306`.
- Dejá que quede como **servicio de Windows**: arranca solo y no hay que levantarlo a mano.

---

## Después de instalar MySQL: el cliente en el PATH

El instalador **no publica `mysql` en la terminal**. Sin esto, `mysql --version` dice `FALTA`
aunque el servidor esté corriendo — Workbench y la API no lo notan porque le hablan al servidor por
el puerto, sin pasar por el PATH.

```powershell
# Encuentra la carpeta bin de la versión instalada (8.0, 8.4, la que sea)
$mysql = (Get-ChildItem "$env:ProgramFiles\MySQL" -Directory -Filter 'MySQL Server*' | Select-Object -First 1).FullName + '\bin'

# Guardarlo para las terminales futuras, y aplicarlo a ésta
[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path','User') + ";$mysql", 'User')
$env:Path += ";$mysql"

mysql --version
```

**Servidor y cliente son dos cosas.** El servidor es el servicio que guarda los datos; `mysql` es
la terminal para hablarle. Comprobar que el servicio está corriendo:

```powershell
Get-Service MySQL*
```

Tiene que decir `Running`. Si dice `Stopped`: `Start-Service` con el nombre que haya listado.

---

## Está listo cuando

- [ ] El chequeo de arriba responde los ocho, ninguno con `FALTA`
- [ ] `dotnet --version` empieza con `10.`
- [ ] `Get-Service MySQL*` dice `Running`
- [ ] `mysql -u root -p` conecta con la contraseña que anotaste (salir con `exit`)
