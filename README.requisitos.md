# Requisitos · versión 1

Todo lo que tiene que estar instalado para levantar esta rama, de dónde se baja y cómo se verifica.
Los comandos son para **PowerShell en Windows**.

Una vez que esto esté, todo lo demás está en [`README.comandos.md`](README.comandos.md).

---

## Antes de instalar: qué tenés ya

**Corré esto primero.** Lo que responda con una versión ya está; lo que diga `FALTA`, no.

```powershell
([ordered]@{ git='--version'; node='--version'; npm='--version'; java='-version'; adb='version'; sdk='' }).GetEnumerator() | ForEach-Object {
	$v = $null
	if ($_.Key -eq 'sdk') { $v = $env:ANDROID_HOME } elseif (Get-Command $_.Key -ErrorAction SilentlyContinue) { $v = & $_.Key $_.Value 2>&1 | Select-Object -First 1 }
	'{0,-6} {1}' -f $_.Key, $(if ($v) { $v } else { 'FALTA' })
}
```

> Si acabás de instalar algo y sigue diciendo `FALTA`, **cerrá la terminal y abrí una nueva**. El
> `PATH` se lee al abrir la sesión, no en cada comando.

---

## Qué instalar

Cuatro programas, en este orden. **Lo más pesado va último a propósito**: podés ir avanzando con los
primeros mientras Android Studio descarga.

| # | Programa | Mínimo | Descarga | Para qué |
|---|---|---|---|---|
| 1 | **Git** | cualquiera reciente | [git-scm.com/download/win](https://git-scm.com/download/win) | clonar el repositorio y versionar tu proyecto |
| 2 | **Node.js** | `22` | [nodejs.org/en/download](https://nodejs.org/en/download) | correr y compilar la app. Trae `npm` incluido |
| 3 | **Visual Studio Code** | cualquiera reciente | [code.visualstudio.com](https://code.visualstudio.com/) | el editor donde se trabaja |
| 4 | **Android Studio** | cualquiera reciente | [developer.android.com/studio](https://developer.android.com/studio) | el SDK, el emulador y el build de Gradle |

**Los números son mínimos.** Si ya tenés una versión más nueva, dejala como está.

Tres cosas que **no** hay que instalar aparte:

- **El JDK** viene con Android Studio, en `...\Android Studio\jbr`, pero **hay que apuntarle
  `JAVA_HOME`** — ver más abajo, sin eso `java` no existe para la terminal. Acá la versión sí es
  exacta —la 21— y conviene no tocarla: Gradle y el plugin de Android son estrictos con el JDK, y
  uno más nuevo rompe el build en lugar de mejorarlo.
- **El SDK de Android** lo descarga el asistente de Android Studio la primera vez que lo abrís.
- **`adb`** viene adentro del SDK.

---

## Después de instalar Node: permitir que npm corra en PowerShell

Windows viene con la ejecución de scripts bloqueada, y `npm` en Windows es un script `.ps1`. Sin
esto, cualquier `npm install` falla con *«no se puede cargar el archivo npm.ps1 porque la ejecución
de scripts está deshabilitada»*.

```powershell
# Ver cómo está. En una máquina recién instalada dice Undefined o Restricted.
Get-ExecutionPolicy -Scope CurrentUser

# Habilitarlo. No pide administrador.
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

`RemoteSigned` deja correr los scripts que están en tu máquina y **exige firma digital a los
descargados de internet**. Es la configuración que recomienda Microsoft para estaciones de trabajo.
Con `-Scope CurrentUser` el cambio vale solo para tu usuario: no toca al resto del equipo.

---

## Después de instalar: `JAVA_HOME`, `ANDROID_HOME` y el `PATH`

Se hace una sola vez. Android Studio trae el JDK y el SDK adentro de sus carpetas, pero **no los
publica en el sistema**: hasta que no corras esto, `java -version` y `adb` no existen para la
terminal, aunque estén instalados.

```powershell
# 1 · Ver qué hay ahora. Si imprimen vacío, no están definidas.
$env:JAVA_HOME
$env:ANDROID_HOME

# 2 · Las dos rutas por defecto. Si instalaste en otro lado, cambialas acá y el resto sigue igual.
$jdk = "$env:ProgramFiles\Android\Android Studio\jbr"
$sdk = "$env:LOCALAPPDATA\Android\Sdk"

# 3 · Guardarlas de forma permanente, para tu usuario.
[Environment]::SetEnvironmentVariable('JAVA_HOME', $jdk, 'User')
[Environment]::SetEnvironmentVariable('ANDROID_HOME', $sdk, 'User')

# 4 · Aplicarlas también a ESTA terminal, para no tener que abrir una nueva.
$env:JAVA_HOME    = $jdk
$env:ANDROID_HOME = $sdk

# 5 · Agregar java y adb al PATH del usuario, sin pisar lo que ya estaba.
$path = [Environment]::GetEnvironmentVariable('Path', 'User')
[Environment]::SetEnvironmentVariable('Path', "$path;$jdk\bin;$sdk\platform-tools", 'User')

# 6 · Y también a esta terminal.
$env:Path += ";$jdk\bin;$sdk\platform-tools"

# 7 · Comprobar. Los tres tienen que responder.
java -version
adb version
$env:ANDROID_HOME
```

> **El JDK no se instala aparte, pero sí hay que apuntarlo.** El que trae Android Studio vive en
> `...\Android Studio\jbr` y es el que usa Gradle. Sin `JAVA_HOME`, `.\gradlew` desde la terminal no
> encuentra con qué compilar — funciona solo si lanzás el build desde adentro de Android Studio, que
> conoce su propio JDK.

> **Los pasos 3 y 5 no son opcionales.** `SetEnvironmentVariable` escribe la variable para las
> terminales **futuras**: la que tenés abierta ya leyó su entorno al arrancar y no se entera. Sin
> esas dos líneas, `$env:ANDROID_HOME` sigue vacío y `adb` sigue sin encontrarse, aunque la variable
> ya esté bien guardada.
>
> Si tenías **otras** terminales abiertas de antes, ésas sí hay que cerrarlas y volver a abrirlas.

### El molde, para cualquier otra variable

```powershell
[Environment]::SetEnvironmentVariable('NOMBRE_DE_LA_VARIABLE', 'el valor', 'User')  # permanente
$env:NOMBRE_DE_LA_VARIABLE = 'el valor'                                            # esta terminal
```

Las dos líneas, siempre. El tercer argumento de la primera es el alcance: `'User'` es tu usuario y
es el que querés casi siempre; `'Machine'` afecta a toda la computadora y pide permisos de
administrador.

Después volvé a correr el chequeo de arriba: tienen que responder los seis.

---

## Conectar el teléfono

Activá la **depuración por USB** en el teléfono, conectalo, y aceptá el diálogo de autorización que
aparece en su pantalla. Está bien conectado cuando lo lista:

```powershell
adb devices
```

Si sale vacío o dice `unauthorized`, el diálogo de autorización quedó sin aceptar.

---

## Está listo cuando

- [ ] El chequeo del entorno responde los seis con una versión, ninguno con `FALTA`
- [ ] `adb devices` lista tu teléfono
- [ ] `npm install` en `RavEatApp/` termina sin errores
