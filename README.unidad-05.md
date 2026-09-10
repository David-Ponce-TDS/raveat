# U5 · Autenticación, roles y biometría

> Login, roles y biometría: quién entra al sistema y qué puede hacer cada uno.

| | |
|---|---|
| **Rama** | `version-05` |
| **Viene de** | `version-04` — ABM, listas y estado compartido |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android, con la API local — la huella, solo en el teléfono |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

Entra la **sesión**: login con JWT, refresh token con rotación, cinco roles y el guard que **por
fin bloquea**. La huella desbloquea una sesión ya guardada. El modelo suma `Rol`, `Usuario` y
`RefreshToken`: ocho entidades.

**Autenticar es quién sos; autorizar es qué podés hacer.** Son dos preguntas distintas, con dos
códigos de error distintos, y confundirlas es el origen de la mayoría de los agujeros de seguridad
de una app. Todavía no hay cámara, archivos, ubicación ni notificaciones: eso arranca en la
versión 6.

---

## 🔄 Qué cambia desde la versión 4

| | `version-04` | `version-05` |
|---|---|---|
| **Modelo** | 5 entidades | 8: + `Rol`, `Usuario`, `RefreshToken` |
| **Endpoints** | todos públicos | **cerrados por defecto**, lo público se abre uno por uno |
| **Primera pantalla** | la vitrina | **el login** |
| **Guard del router** | leía `meta.roles`, no bloqueaba | bloquea según el rol de la sesión |
| **Menú y tabs** | iguales para todos | según el rol: cada uno ve lo suyo |
| **El token** | no existía | viaja en cada request, se renueva solo ante un 401 |
| **Almacenamiento** | `localStorage` para el tema | + **almacenamiento seguro** del sistema para la sesión |
| **Plugins** | los de fábrica | + biometría y secure storage (`@aparajita`) |

---

## 🧰 Qué hay que instalar

**Nada nuevo**: los dos plugins de esta versión entran con `npm install`. La verificación del entorno
está en [`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Contraseñas: por qué se guarda el hash y nunca el texto
2. Qué es un JWT y qué significa que esté firmado
3. Claims: la identidad viaja adentro del token
4. Roles como datos, no como enum de código
5. Cerrar por defecto y abrir lo público endpoint por endpoint
6. El problema de revocar un token que el servidor no guarda
7. Refresh tokens: opacos, hasheados y con rotación
8. Almacenamiento seguro en el dispositivo
9. El guard del router empieza a bloquear
10. Biometría: desbloquear una sesión, no reemplazar el login

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Qué hace ahora |
|---|---|
| **Login** | lo primero que se ve — email y contraseña, y el botón de huella si hay sesión guardada |
| **Usuarios** | nueva, solo `ADMIN`: alta de usuarios y asignación de roles |
| **Menú y tabs** | cada rol ve solo sus pantallas |
| **Mi cuenta** | la sesión: quién sos, tu rol, cerrar sesión — y le explica al usuario sin rol que falta habilitarlo |

El estado que enseña el módulo: **un usuario recién creado entra y no puede hacer nada**. Tiene
sesión válida y token firmado, pero hasta que un administrador le asigne un rol no accede a ninguna
pantalla operativa. No es un error: es un estado del modelo, y por eso `Usuario.RolId` es opcional.

---

## 🔑 Autenticar no es autorizar

| | Autenticación | Autorización |
|---|---|---|
| Pregunta | ¿quién sos? | ¿qué podés hacer? |
| Dónde vive | el token firmado | el rol dentro de ese token |
| Si falla | **401** — renovar o volver a entrar | **403** — tu rol no alcanza |

Confundir los dos códigos es el error típico: ante un 403 no sirve renovar el token ni mandar al
login, porque la sesión está perfecta. Lo que falta es permiso. Por eso `[Authorize]` a secas **no
alcanza** —solo exige estar autenticado y dejaría pasar al usuario sin rol—: en los endpoints se
listan los roles.

---

## 🔁 Un token firmado no se puede revocar

Un JWT vale hasta que vence: el servidor no lo guarda, solo verifica la firma. Dar de baja a un
usuario **no lo saca al instante** — seguiría entrando horas con el token que ya tiene.

La solución es `OnTokenValidated` en `Program.cs`: en cada request confirma contra la base que el
usuario siga activo y que su rol no haya cambiado. Cuesta una consulta por request y a cambio una
baja o un cambio de rol tienen efecto inmediato.

El **refresh token** resuelve el otro lado: el access token dura poco a propósito, y para no pedir
la contraseña a cada rato se cambia por uno nuevo con un secreto de larga vida. Tres decisiones que
van juntas:

- es **opaco**, no un JWT: no lleva datos, solo sirve para pedir un access token;
- se guarda **hasheado**, igual que una contraseña: quien lea la tabla no puede usarlos;
- **rota en cada uso**: el presentado queda revocado. Un refresh robado deja de servir apenas el
  dueño legítimo renueva.

En el frontend, la renovación ante un 401 es **única en curso**: si diez requests fallan juntas,
todas esperan la misma llamada a `/refresh`. Sin eso, con rotación, diez renovaciones en paralelo
se revocarían entre sí y cerrarían la sesión de alguien que no hizo nada mal.

---

## 👆 La biometría no reemplaza al login

No hay forma de que una huella se convierta en un token: **el servidor nunca la ve.** Lo que hace
es desbloquear la sesión que ya está guardada en el dispositivo. El usuario entra una vez con email
y contraseña; a partir de ahí la huella le evita volver a escribirlos.

Por eso el botón de huella solo aparece con **dos** condiciones: que el dispositivo tenga biometría
y que haya una sesión guardada. Sin login previo no hay nada que desbloquear.

Y el token se guarda en el **almacenamiento seguro del sistema** (Keystore en Android), nunca en
`localStorage`: ahí lo lee cualquier script que se cuele en la webview. En el navegador el plugin
cae a `localStorage` porque no hay nada mejor, y para desarrollo alcanza.

---

## 🌐 La API

Cada controller está **cerrado a nivel de clase** y lo público se abre endpoint por endpoint. Es a
propósito: olvidarse del atributo tiene que dejar el endpoint cerrado de más, no abierto.

| Ruta | Quién |
|---|---|
| `/health`, vitrina, `GET /api/categorias`, `GET /api/productos/{id}` | público |
| `/api/sesion/login`, `/refresh`, `/logout` | público |
| `/api/sesion/yo` | cualquier autenticado, **incluso sin rol** |
| Catálogo, ABM de productos y categorías, `/api/usuarios` | `ADMIN` |
| `/api/clientes` | `ADMIN`, `VENDEDOR`, `CAJA` |
| Pedidos: ver y mover estados | los cinco roles |
| Pedidos: crear y cancelar | `ADMIN`, `VENDEDOR`, `CAJA` |
| Pedidos: cobrar | `ADMIN`, `CAJA` |

`/api/sesion/yo` es el **único** endpoint con `[Authorize]` a secas, y tiene motivo: es de donde
la app aprende que el usuario todavía no fue habilitado.

Los endpoints nuevos:

| Método | Ruta | Devuelve |
|---|---|---|
| `POST` | `/api/sesion/login` | `{sesion}` con token, refresh y usuario |
| `POST` | `/api/sesion/refresh` | `{sesion}` nueva. **Revoca el refresh presentado** |
| `POST` | `/api/sesion/logout` | revoca el refresh (o todos, con `todos: true`) |
| `GET` | `/api/sesion/yo` | `{usuario}` con su rol actual |
| `GET` | `/api/usuarios` | `{usuarios, pagina, roles}` — paginado |
| `POST` | `/api/usuarios` | alta. Sin `rol_id` queda sin habilitar |
| `PUT` | `/api/usuarios/{id}/rol` | asigna o quita el rol |
| `DELETE` | `/api/usuarios/{id}` | baja lógica **y revoca sus sesiones** |

El resto del contrato sigue el de la versión 4: ABM, paginación, estados de pedido, `snake_case`,
proyección a `record` y bajas lógicas.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/                  el frontend, como en la versión 4, más:
│  └─ src/
│     ├─ services/             + sesion_service, token_service, biometria_service, usuarios_service
│     ├─ stores/               + sesion_store, usuarios_store
│     └─ views/                + login_page, usuarios_page
└─ RavEat.Api/
   ├─ Auth/                    nuevo: TokenService (emite y valida) y ClaimsExtensions
   ├─ Controllers/             + SesionController, UsuariosController
   ├─ Domain/Entities/         + Rol, Usuario, RefreshToken — 8 entidades
   └─ Migrations/              + identidad: roles, usuarios y el administrador inicial
```

---

## ✅ Terminaste cuando

- [ ] `database update` crea las tablas, la carta, los clientes, los cinco roles **y el administrador inicial**
- [ ] La app abre en el **login**; con el administrador sembrado se ven los cinco tabs
- [ ] `GET /api/productos/listado` **sin token devuelve 401**; con el token del admin, 200
- [ ] El admin crea un usuario **sin rol**: ese usuario entra, ve solo Inicio y Mi cuenta, y Mi cuenta le explica que falta habilitarlo
- [ ] Al asignarle el rol Caja, pasa a ver Pedidos — y al intentar crear uno recibe 403: cobrar sí, crear no
- [ ] Cerrar sesión y renovar funcionan; dos renovaciones seguidas no cierran la sesión
- [ ] En el teléfono, la huella desbloquea la sesión guardada sin pedir la contraseña
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
