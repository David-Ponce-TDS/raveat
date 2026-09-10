# U4 · ABM, listas y estado compartido

> Alta, baja y modificación con validación en el servidor, listas paginadas y estados.

| | |
|---|---|
| **Rama** | `version-04` |
| **Viene de** | `version-03` — backend, base de datos y primera lista |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android, con la API local |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

El modelo llega a **cinco entidades** —categorías, productos, clientes, pedidos e ítems—, todas las
listas paginan del lado del servidor y hay alta, baja y modificación con validación en la API. Es la
versión donde la app pasa a tener su **flujo central**.

**La pantalla es una vista parcial y vieja de los datos.** Parcial porque la lista pagina; vieja
porque entre que se pidió y se muestra, alguien más pudo escribir. Casi todas las decisiones de esta
versión salen de aceptar eso. Todavía no hay login: los endpoints siguen siendo públicos.

---

## 🔄 Qué cambia desde la versión 3

| | `version-03` | `version-04` |
|---|---|---|
| **Modelo** | 2 entidades: categorías y productos | 5: + clientes, pedidos e ítems de pedido |
| **Endpoints** | solo lectura | ABM completo, con validación en el servidor |
| **Listas** | la carta entera, de una | **paginadas**: `Skip`/`Take`, `hay_mas` y total |
| **Buscar y filtrar** | chips en memoria | en la API — en memoria solo la vitrina, que no pagina |
| **Escritura** | no había | formularios en modal + **recarga desde la API** al guardar |
| **Estados de pedido** | no había pedidos en la API | máquina de transiciones válidas en el servidor |
| **Componentes base** | estados de lista | + `comp_buscador`, `comp_lista` |

---

## 🧰 Qué hay que instalar

**Nada nuevo**: el entorno es el de la versión 3. La verificación está en
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Modelo completo: clientes, pedidos e ítems de pedido
2. Paginación del lado del servidor: `Skip`/`Take` y qué devolver además de los items
3. La regla que va con la paginación: buscar y filtrar en la API, nunca en memoria
4. Contadores que no se filtran a sí mismos
5. Alta, baja y modificación con validación en el servidor
6. Bajas lógicas y por qué un pedido se cancela en vez de borrarse
7. Formularios en modal y manejo del error que devuelve la API
8. Estado compartido: el store acumula páginas y recuerda filtros
9. Máquina de estados de un pedido y transiciones válidas
10. Datos que se copian: por qué el ítem guarda el precio y no lo lee del producto

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Qué hace ahora |
|---|---|
| **Inicio** | la vitrina, como en la versión 3 — filtra en memoria porque no pagina |
| **Productos** | catálogo **paginado**, con buscador, filtro por categoría y disponibilidad — desde la API |
| **Clientes** | lista paginada con buscador, y el ABM completo en modal |
| **Pedidos** | lista paginada con filtros por estado, alta eligiendo productos, cambio de estado y pago |
| **Mi cuenta** | interruptor de tema y **Diagnóstico**: contra qué URL pega la app y si responde |

> La vitrina y el catálogo muestran los mismos datos con distinta regla de filtrado, a propósito:
> **filtrar en memoria solo vale si tenés todo.** El catálogo pagina, así que el mismo gesto ahí
> sale a la API. Confundirlos da el bug que funciona con cinco filas y miente con doscientas:
> «sin resultados» con los resultados en la página siguiente.

---

## 🔑 La pantalla es una vista parcial y vieja

**Parcial**, porque en pantalla nunca hay más que un pedazo. De ahí la regla que acompaña siempre a
la paginación: si la lista pagina, buscar y filtrar se hace en la API.

**Vieja**, porque entre que se pidió y se muestra, alguien más pudo escribir. Por eso después de
guardar **se recarga desde la API** en vez de tocar la lista en memoria: el servidor es el que sabe
el orden, el total y si el alta pasó las validaciones. «Ya sé qué guardé, lo agrego al array» es
inventar.

| Decisión | Por qué |
|---|---|
| El botón «ver más» mira `hay_mas` de la API | Contar filas diría «hay más» con 20 resultados y páginas de 20 |
| El total sale de `pagina.total` | Es la única forma de poder decir «5 de 43» |
| Al acumular se descartan los ids ya conocidos | Un alta entre dos pedidos corre la paginación y repetiría una fila |
| La búsqueda espera 400 ms | Sin eso cada tecla dispara una consulta, y las respuestas llegan desordenadas |
| Los contadores no se filtran a sí mismos | Al filtrar «disponibles», el contador de no disponibles daría 0 |

---

## ⚖️ Dos reglas del servidor que no se negocian

**Los precios no llegan del cliente.** Al crear un pedido, el cuerpo manda producto y cantidad; el
precio lo lee el servidor de la base. Si viajara desde el teléfono, cualquiera podría pedir una
pizza a un peso cambiando la request. Y cada ítem **copia** el nombre y el precio al crearse: un
pedido de la semana pasada tiene que seguir diciendo lo que costó entonces.

**La validación vive en el servidor.** Los formularios no la repiten: la misma regla en dos lados
garantiza que en algún momento digan cosas distintas, y la que vale es siempre la de la API. El
modal solo se cierra si el servidor aceptó; si no, queda abierto con el mensaje y sin perder lo que
se escribió.

---

## 🧩 Componentes: ahora sí, las tres carpetas

| ¿Es…? | Va en | Hoy |
|---|---|---|
| el armazón de la app | `components/estructura/` | `comp_page`, `comp_header`, `comp_menu`, `comp_tabs`, `comp_vidriera` |
| genérico, no habla del negocio | `components/base/` | `comp_estado_vacio`, `comp_estado_error`, `comp_esqueleto`, `comp_buscador`, `comp_lista` |
| del negocio | `components/dominio/` | `comp_producto_card`, `comp_producto_lista_item` y los modales de formulario |

`comp_lista` y `comp_buscador` son de `base/` justamente porque **no saben de qué son los items**:
la lista recibe si hay más páginas y avisa cuándo pedirlas; el buscador espera a que dejes de
tipear y emite el texto. Sirven igual para clientes, productos o pedidos.

La fuente de verdad única sigue de la versión 2: `config/navegacion.js` declara cada pantalla con
sus tres listas de roles (`roles`, `menu_roles`, `tab_roles`). El guard ya lee `meta.roles` pero
**no bloquea**: no hay sesión hasta la versión 5.

---

## 🌐 La API

Ningún endpoint pide token todavía: la autenticación es el tema de la versión 5.

| Método | Ruta | Devuelve |
|---|---|---|
| `GET` | `/health` | `{status, utc}` — la prueba de vida |
| `GET` | `/api/categorias` | `{categorias}` |
| `POST` `PUT` `DELETE` | `/api/categorias[/{id}]` | ABM. La baja falla si la categoría tiene productos activos |
| `GET` | `/api/productos/resumen` | la carta entera, sin paginar — la vitrina |
| `GET` | `/api/productos/listado` | paginado, con `busqueda`, `categoria_id` y `disponible` |
| `POST` `PUT` `DELETE` | `/api/productos[/{id}]` | ABM. La baja es lógica |
| `GET` | `/api/clientes` | paginado, con `busqueda` |
| `POST` `PUT` `DELETE` | `/api/clientes[/{id}]` | ABM. El teléfono no se puede repetir |
| `GET` | `/api/pedidos` | paginado, con `busqueda`, `estados` (CSV) y `cliente_id` |
| `POST` | `/api/pedidos` | crea el pedido. **El cuerpo no lleva precios** |
| `PUT` | `/api/pedidos/{id}/estado` | cambia el estado si la transición es válida |
| `PUT` | `/api/pedidos/{id}/pago` | registra medio de pago y propina |
| `DELETE` | `/api/pedidos/{id}` | **cancela**, no borra |

Paginación: `?pagina=&tamano=`, tope 100. La respuesta trae `pagina: {pagina, tamano, total, hay_mas}`.

```
borrador ──> confirmado ──> en_preparacion ──> listo ──> entregado ──> cerrado
    │            │                │              │
    └────────────┴────────────────┴──────────────┴──> cancelado
```

Un pedido entregado no vuelve a preparación y uno cancelado no revive. La tabla de transiciones
vive en un diccionario en `PedidosController`; el frontend no la duplica. El contrato sigue el de
la versión 3: JSON en `snake_case`, respuestas que proyectan a `record`, imágenes como ruta
relativa y bajas lógicas.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/                  el frontend, como en la versión 3, más:
│  └─ src/
│     ├─ components/base/      + comp_buscador, comp_lista
│     ├─ components/dominio/   + los modales de formulario de cliente, producto y pedido
│     ├─ services/             un service por recurso: clientes, pedidos
│     ├─ stores/               + clientes_store, pedidos_store — acumulan páginas y recuerdan filtros
│     └─ utils/                + consulta.js: arma la query string de filtros
└─ RavEat.Api/
   ├─ Controllers/             + ClientesController, PedidosController
   ├─ Data/                    + Paginacion.cs
   ├─ Domain/                  Entities/ (5 entidades) y Enums/ (estados de pedido)
   └─ Migrations/              + clientes, pedidos e ítems, con datos de prueba
```

---

## ✅ Terminaste cuando

- [ ] `dotnet tool run dotnet-ef -- database update` crea las tablas y carga la carta y los clientes
- [ ] `/api/clientes?tamano=2` devuelve 2 clientes y `pagina.hay_mas: true`
- [ ] Productos y Clientes muestran sus listas paginadas con buscador
- [ ] Se puede crear un cliente, editarlo y darlo de baja sin recargar la página
- [ ] Se puede crear un pedido eligiendo productos, y el total que devuelve la API coincide
- [ ] Pasar un pedido de `borrador` a `entregado` devuelve `transicion_invalida`
- [ ] Con la API apagada, todas las listas muestran **error con Reintentar** — no el vacío
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores
- [ ] El APK instalado crea un registro desde el teléfono y aparece en la lista de la PC

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
