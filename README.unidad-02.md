# U2 · Navegación por configuración

> Navegación centralizada y consistente entre rutas, menú lateral y tabs.

| | |
|---|---|
| **Rama** | `version-02` |
| **Viene de** | `version-01` — entorno, tema propio y primer APK |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite |
| **Corre en** | navegador y teléfono Android |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

Cinco pantallas, menú lateral y barra de tabs, generados desde un archivo de configuración.

**Rutas, menú, tabs y permisos son un solo dato visto de cuatro maneras.** Escribirlos por separado
garantiza que en algún momento digan cosas distintas.

Todavía no hay backend ni login: los datos salen de `src/datos/`, con la forma que después devuelve
la API.

---

## 🔄 Qué cambia desde la versión 1

| | `version-01` | `version-02` |
|---|---|---|
| **Pantallas** | una | cinco: Inicio, Productos, Clientes, Pedidos, Mi cuenta |
| **Navegación** | una ruta escrita a mano | menú lateral + tabs, generados |
| **Rutas** | escritas a mano | derivadas de `config/navegacion.js` |
| **Componentes** | ninguno | 8, en tres carpetas |
| **Roles** | no existen | cinco declarados |
| **Datos** | `carta.js` | + `clientes.js` y `pedidos.js` |
| **Botón de tema** | barra superior | Mi cuenta |
| **Ícono y splash** | de fábrica | propios, con variante oscura |
| **Dependencias** | — | ninguna nueva |

---

## 🧰 Qué hay que instalar

Nada: **es un cambio estructural**. Todo se arma con lo que la versión 1 ya tenía y `package.json`
no cambia.

La regla: antes de agregar una dependencia, ver si el problema se resuelve reorganizando lo que ya
está. En la versión 3, con API y base de datos, no va a alcanzar.

Chequeo del entorno: [`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Un archivo de configuración como fuente de verdad única
2. Generar las rutas del router desde esa configuración
3. Menú lateral y tabs como dos lecturas del mismo dato
4. El layout: armazón contra página
5. Las tres carpetas de componentes
6. Estados de una lista: cargando y vacío
7. Datos de prueba con la forma de la API
8. Ícono, splash y favicon propios

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Muestra |
|---|---|
| **Inicio** | vidriera con el logo, tarjeta de la versión, destacados de la carta y qué sale de `navegacion.js` |
| **Productos** | la carta en grilla, con chips para filtrar por categoría |
| **Clientes** | lista con inicial, teléfono y dirección |
| **Pedidos** | código, cliente, total y dos etiquetas: estado del pedido y del pago |
| **Mi cuenta** | interruptor de tema; la sesión llega en la versión 5 |

Las cuatro listas se deslizan hacia abajo para actualizar y dibujan un esqueleto mientras cargan.
Los datos salen de un archivo local y aun así se piden con `await` y estado de carga: es la forma
que van a tener en la versión 3, así la pantalla no cambia al conectarla.

**El esqueleto ocupa el lugar del contenido real.** Sin él la pantalla salta cuando llegan los
datos, y ese salto es lo que hace que una app se sienta lenta.

---

## 🔑 Un archivo, tres consumidores

`src/config/navegacion.js` es el único lugar donde se declara una pantalla.

| Sale de ahí | Lo consume |
|---|---|
| Las rutas del router | `router/index.js`, recorriendo la lista |
| El menú lateral | `comp_menu` → `obtener_grupos_menu_rol()` |
| La barra de tabs | `comp_tabs` → `obtener_tabs_rol()` |

Agregar una pantalla es agregar un objeto. No se toca el router, ni el menú, ni los tabs:

```javascript
{
	id: 'productos',
	titulo: 'Productos',
	ruta: '/app/productos',
	icono: pricetagsOutline,
	roles:      ['administrador', 'vendedor'],   // quién puede ENTRAR
	menu_roles: ['administrador', 'vendedor'],   // quién lo ve en el MENÚ
	tab_roles:  ['administrador', 'vendedor'],   // quién lo ve en los TABS
	grupo_menu: 'operacion',
	orden: 20,
	componente: () => import('@/views/productos_page.vue')
}
```

**Tres listas de roles porque son tres preguntas distintas.** Una pantalla de detalle es accesible
pero no va en el menú; una de configuración va en el menú y no en los tabs.

Sin sesión todavía, los roles no filtran: las funciones se llaman sin rol y devuelven todo. Se
declaran igual para que la configuración tenga desde hoy su forma final.

```javascript
export const MAXIMO_TABS = 5;
```

Ionic no maneja bien más de cinco tabs: la barra se aprieta y deja de leerse. El recorte vive con la
configuración, no en el template.

---

## 🧩 Las tres carpetas de componentes

En la versión 1 la única pantalla escribía a mano su `ion-page`, su header y su `ion-content`. Con
cinco pantallas eso se repite cinco veces, así que se extrae.

| ¿Es…? | Va en | Hoy |
|---|---|---|
| el armazón de la app | `estructura/` | `comp_page`, `comp_header`, `comp_menu`, `comp_tabs`, `comp_vidriera` |
| genérico, no habla del negocio | `base/` | `comp_esqueleto`, `comp_estado_vacio` |
| del negocio | `dominio/` | `comp_producto_card` |

**El dominio es el negocio del que trata la app.** El de RavEat son productos, clientes y pedidos;
el de un gimnasio serían socios, rutinas y clases. Un componente de `dominio/` habla ese
vocabulario: `comp_producto_card` sabe que un producto tiene precio y disponibilidad.
`comp_esqueleto` no sabe de qué son las filas que dibuja, y por eso es de `base/`.

La prueba: **¿lo podrías copiar tal cual a una app de otro rubro?** Sí → `base/`. No → `dominio/`.
Sostiene la app entera → `estructura/`.

Importa el jueves: tu `dominio/` va a tener otros nombres, tu `base/` y tu `estructura/` van a ser
casi las mismas.

Las tres llegan hasta el final. En la versión 12 hay 34 componentes:

| Carpeta | v2 | v12 |
|---|---|---|
| `estructura/` | 5 | 6 |
| `base/` | 2 | 15 |
| `dominio/` | 1 | 13 |

El armazón es finito y no crece con las pantallas. Lo que crece es el negocio y el repertorio de
piezas genéricas.

**Nombres:** el archivo y el `name:` se llaman igual —`comp_page`—, el alias de registro es
`CompPage` y en el template se escribe `<comp-page>`.

Una pantalla completa queda así:

```vue
<comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
	<comp-esqueleto v-if="cargando" />
	<comp-estado-vacio v-else-if="productos.length === 0" titulo="Todavía no hay productos" />
	<template v-else>…</template>
</comp-page>
```

Ni `ion-page`, ni header, ni `ion-content`: eso lo pone `comp_page`.

---

## 🎨 Ícono y splash propios

Cinco imágenes fuente en `RavEatApp/assets/`, y un comando genera el resto —
[`README.comandos.md`](README.comandos.md), paso 7.

| Fuente | Genera |
|---|---|
| `icon-only.png` | el ícono de la app y los favicones |
| `icon-foreground.png` · `icon-background.png` | las dos capas del adaptive icon |
| `splash.png` · `splash-dark.png` | la pantalla de arranque, claro y oscuro |

**El ícono de Android son dos imágenes.** Desde Android 8 es adaptativo: el sistema recibe fondo y
figura por separado y decide la forma del recorte —círculo, cuadrado redondeado, gota— según el
launcher. La figura va centrada y con aire: lo que toca el borde se recorta.

**El splash lo dibuja Android, no la app**, mientras la webview todavía no existe. Misma idea de la
versión 1 —parte nativa y parte web se turnan—, ahora con imagen propia.

El ícono vive en el paquete: cambiarlo es compilar de nuevo.

---

## 📁 Cómo está organizado

```
RavEatApp/
├─ src/
│  ├─ components/
│  │  ├─ estructura/   page, header, menu, tabs, vidriera
│  │  ├─ base/         esqueleto, estado vacío
│  │  └─ dominio/      producto_card
│  ├─ config/          navegacion.js y roles.js  ← la fuente de verdad
│  ├─ datos/           carta, clientes y pedidos de prueba
│  ├─ layouts/         main_layout.vue: menú + tabs + contenedor
│  ├─ router/          generado desde la configuración
│  ├─ stores/          stores Pinia (por ahora solo el tema)
│  ├─ theme/           global.css y componentes.css
│  ├─ views/           una página por ruta
│  ├─ App.vue          el contenedor raíz
│  └─ main.js          arranque de la app
├─ android/            proyecto Android generado por Capacitor
├─ assets/             las cinco fuentes de ícono y splash
└─ public/             logo, fotos de la carta y favicones
```

| Archivo de estilos | Qué tiene |
|---|---|
| `theme/global.css` | las variables `--raveat-*` y el lenguaje visual |
| `theme/componentes.css` | los componentes del armazón, importado desde `main.js` |
| `<style scoped>` | solo lo exclusivo de un componente |

Si dos pantallas lo comparten, no va scoped.

---

## 📦 Los datos de prueba

`clientes.js` y `pedidos.js` tienen los mismos nombres de campo y los mismos valores de estado que
después devuelve la API:

```
pedido -> borrador, confirmado, en_preparacion, listo, entregado, cerrado, cancelado
pago   -> pendiente, pagado, anulado
tipo   -> presencial, retiro, entregas
```

Las direcciones son reales de CABA con coordenadas verdaderas, porque en la versión 7 se dibujan en
un mapa. La lista cubre varios estados a la vez: si todos los pedidos estuvieran en el mismo, no se
vería que cada uno se pinta distinto.

---

## ✅ Terminaste cuando

- [ ] `npm install` termina sin errores
- [ ] La app abre en `http://localhost:5173` y arranca en **Inicio**
- [ ] El menú lateral abre desde la barra superior y lista las cinco pantallas en dos grupos
- [ ] La barra de tabs navega entre las cinco y marca sola la pestaña activa
- [ ] Las cuatro listas muestran el esqueleto mientras cargan y después los datos
- [ ] Deslizar hacia abajo en una lista la actualiza
- [ ] El interruptor de tema de **Mi cuenta** alterna claro y oscuro, y sobrevive a un recargado
- [ ] Agregaste un objeto a `config/navegacion.js` y la pantalla apareció sola en el menú y en los tabs
- [ ] `npm run lint` y `npm run build` terminan sin errores
- [ ] `.\gradlew assembleDebug` genera el APK
- [ ] El APK instalado abre en el teléfono y navega igual que en el navegador
- [ ] En la pantalla de inicio del celular se ve tu ícono, y al abrir la app, tu splash

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
