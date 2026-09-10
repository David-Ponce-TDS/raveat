# U3 · Backend, base de datos y primera lista

> El núcleo del sistema: persistencia, API REST y la primera pantalla con datos reales.

| | |
|---|---|
| **Rama** | `version-03` |
| **Viene de** | `version-02` — navegación por configuración |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite **+ ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8** |
| **Corre en** | navegador y teléfono Android, con la API local |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

Una API de ASP.NET Core sobre MySQL con dos entidades —categorías y productos— y las pantallas de
Inicio y Productos trayendo datos reales.

**El estado real vive en el servidor.** En la versión 2 las pantallas inventaban su contenido;
ahora el frontend no sabe nada: pide y muestra. Todavía no hay login, ni paginación, ni alta de
datos — todos los endpoints son de lectura y públicos.

---

## 🔄 Qué cambia desde la versión 2

| | `version-02` | `version-03` |
|---|---|---|
| **Módulos** | solo el frontend | + `RavEat.Api/`, el backend |
| **Datos de la carta** | `src/datos/carta.js` y `public/seed/` | la API — MySQL y `wwwroot/seed/` |
| **Datos de clientes y pedidos** | archivos locales | archivos locales todavía: entran a la API en la versión 4 |
| **Camino a la red** | no existía | `store → service → ajax_service → API` |
| **Estados de una lista** | cargando y vacío | + **error**, con botón Reintentar |
| **Componentes** | 8 | 10: + `comp_estado_error`, `comp_producto_lista_item` |
| **Configuración** | `navegacion.js`, `roles.js` | + `debug.js` (la URL de la API), `constantes.js` |
| **Secretos** | no había | User Secrets en la API, `.env` en el frontend |

---

## 🧰 Qué hay que instalar

Dos programas: **.NET SDK 10** y **MySQL Server 8**. Enlaces, verificación previa y configuración
en [`README.requisitos.md`](README.requisitos.md).

Es la regla de la versión 2 cumplida: la navegación se resolvió reorganizando; una base de datos y
una API no se resuelven reorganizando.

---

## 📚 Temas

1. La arquitectura: app → API → base de datos, y por qué el estado real vive en el servidor
2. ASP.NET Core: controllers, `DbContext` y entidades
3. Migraciones de EF Core: el esquema versionado, con datos de prueba incluidos
4. El contrato de la API: JSON en `snake_case`, DTOs que proyectan, nunca la entidad completa
5. El camino único a la red: `store → service → ajax_service`
6. Los tres estados de una lista: cargando, **error** y vacío
7. Secretos fuera del repositorio: User Secrets y `.env`
8. La app en el teléfono contra la API de tu PC: la IP de la red

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Datos |
|---|---|
| **Inicio** | la vitrina — grilla con foto, precio y chips de categoría — **desde la API** |
| **Productos** | la misma carta como lista, **desde la API** |
| **Clientes** y **Pedidos** | archivos locales, como en la versión 2 |
| **Mi cuenta** | interruptor de tema y **Diagnóstico**: contra qué URL pega la app y si responde |

Inicio y Productos comparten `productos_store`: se pide una sola vez y las dos leen de ahí. Que
Clientes y Pedidos sigan locales no es una deuda: **es el corte de la versión**. Sus archivos ya
tienen la forma que devolverá la API, así que conectarlos después no toca las pantallas — es
exactamente lo que acaba de pasar con la carta.

> El chip de categoría filtra **en memoria**, y vale mientras la carta entera quepa en una
> respuesta. Cuando la lista se pagine (versión 4), el filtro se muda a la API.

---

## 🔑 Un solo camino a la red

```
página/componente  →  store  →  service  →  ajax_service  →  API
```

Ningún componente llama a la red por su cuenta; no se usa `fetch` ni `axios` para endpoints de
negocio. Concentrar la salida en un archivo es lo que permite después agregar token, reintentos o
cola offline **en un solo lugar** en vez de en cuarenta.

| Archivo | Qué resuelve |
|---|---|
| `config/debug.js` | **el único** lugar que arma la URL base de la API |
| `services/ajax_service.js` | la llamada, el timeout y la normalización del error |
| `services/productos_service.js` | qué endpoint corresponde a cada operación |
| `services/health_service.js` | la prueba de vida, que usa el Diagnóstico de Mi cuenta |
| `stores/productos_store.js` | el estado: `cargando`, `error`, `productos` |
| `views/productos_page.vue` | solo dibuja los tres estados |

---

## 🚦 Los tres estados de una lista

| Estado | Qué se ve | Por qué importa |
|---|---|---|
| Cargando | `comp-esqueleto` | ocupa el lugar del contenido: la pantalla no salta al llegar los datos |
| **Error** | `comp-estado-error` + Reintentar | una lista que **no cargó** no está vacía |
| Vacío | `comp-estado-vacio` | la API respondió, pero no hay nada que mostrar |

Confundir error con vacío es el clásico: decir «todavía no hay productos» con la API apagada manda
a buscar el problema al lugar equivocado. La prueba está en el cierre: con la API apagada, Inicio y
Productos tienen que mostrar **error**, no vacío.

---

## 🌐 La API

Todos los endpoints son `GET` y no piden token: la autenticación es el tema de la versión 5.

| Ruta | Devuelve |
|---|---|
| `/health` | `{status, utc}` — la prueba de vida |
| `/api/categorias` | `{categorias}` |
| `/api/productos/resumen` | `{resumen, categorias, productos}` — la carta entera |
| `/api/productos/{id}` | `{producto}` o 404 |

El contrato:

- **Todo el JSON en `snake_case`**, igual que las columnas de MySQL: una sola convención de punta a
  punta, sin mapear a mano en cada service.
- Las respuestas **nunca son la entidad completa**: cada endpoint proyecta a un `record` con los
  campos que hacen falta. Las fechas de auditoría y el flag `activo` son cosa de la base.
- Las imágenes se guardan como ruta relativa (`/seed/productos/pizza.jpg`): guardar el host
  invalidaría todas al mover la API de máquina. El frontend la completa en `utils/imagenes.js`.
- Las bajas son **lógicas** (`activo = false`). `disponible` es otra cosa: el producto sigue en la
  carta pero hoy no se puede pedir.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/                  el frontend, como en la versión 2, más:
│  ├─ src/
│  │  ├─ config/               + debug.js y constantes.js
│  │  ├─ services/             ajax_service.js y un service por recurso
│  │  ├─ stores/               + productos_store, categorias_store
│  │  └─ utils/                formato_moneda.js, imagenes.js
│  └─ .env.example             plantilla de variables (copiar como .env)
└─ RavEat.Api/                 el backend, nuevo
   ├─ Controllers/             CategoriasController, ProductosController
   ├─ Data/                    RavEatDbContext y Configurations/ (Fluent API)
   ├─ Domain/Entities/         Categoria, Producto, EntityBase
   ├─ Migrations/              el esquema, versionado
   ├─ wwwroot/seed/productos/  las fotos de la carta
   ├─ appsettings.json         plantilla de conexión — la clave real va en User Secrets
   └─ Program.cs               CORS, DbContext, JSON snake_case, /health
```

La versión del SDK la fija `global.json`; la de `dotnet-ef`, `dotnet-tools.json`. Las dos viven en
el repositorio para que el build sea el mismo en cualquier máquina.

---

## 🔐 Secretos fuera del repositorio

Dos mecanismos, uno por lado, la misma idea:

| Lado | Archivo versionado (plantilla) | El valor real |
|---|---|---|
| API | `appsettings.json` con `Password=CAMBIAR` | User Secrets, fuera del repo |
| Frontend | `.env.example` | `.env`, cortado por `.gitignore` |

Lo que se versiona dice **qué configurar**; lo que no se versiona dice **cuánto vale**. La regla es
la misma que la del keystore: una clave que entra a git ya no sale.

---

## ✅ Terminaste cuando

- [ ] `npm install` y `dotnet tool restore` terminan sin errores
- [ ] `dotnet tool run dotnet-ef -- database update` crea las tablas y carga la carta
- [ ] `http://localhost:5080/health` devuelve `{"status":"ok"}`
- [ ] `/api/productos/resumen` devuelve productos con sus categorías
- [ ] Inicio muestra la vitrina con fotos, precios y chips; Productos, la misma carta como lista
- [ ] Con la API apagada, Inicio y Productos muestran **error con Reintentar** — no el vacío
- [ ] Clientes y Pedidos siguen andando: sus datos son locales todavía
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores
- [ ] El APK instalado muestra la carta en el teléfono, con la API corriendo en tu PC
- [ ] En el teléfono, **Mi cuenta → Diagnóstico** muestra la IP de tu máquina —no `localhost`— y
      responde `ok` al probar
- [ ] `chrome://inspect` muestra la fila del pedido al tocar Reintentar, con esa misma dirección

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
