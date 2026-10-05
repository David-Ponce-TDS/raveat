# U9 · Interfaz normalizada y push automática

> No se rediseña de cero: se mejora lo que la app ya tiene y se normaliza, para que todas las
> pantallas sigan las mismas reglas. La API no cambia su contrato. Y la push deja la consola de
> Firebase: la manda la API.

| | |
|---|---|
| **Rama** | `version-09` |
| **Viene de** | `version-08` — offline, firma y API publicada |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**Mejorar y normalizar lo que ya está, sin tocar el contrato.** No se tira la interfaz para
hacer otra: se toma la de la versión 8 y se la ordena. Lo que cada pantalla decidía por su cuenta
—el color de un estado, el radio de una tarjeta, el espacio entre piezas, cómo se espera o cómo
se confirma— pasa a decidirse una sola vez, y todas las pantallas lo usan igual. Sobre esa base se
suman las mejoras que faltaban: el recorte de la foto, el pedido armado desde la carta, el
buscador de productos y las fichas propias.

La API no cambia ni un endpoint de los que ya tenía. Es la comprobación, con la app entera de por
medio, de que las reglas de las ocho versiones anteriores servían para algo: si la pantalla nunca
llamó a la API por su cuenta y el store fue el único que supo de datos, mejorar toda la interfaz no
toca el backend.

La otra mitad es **la push automática**. En la versión 7 se mandaba a mano desde la consola de
Firebase; ahora cada aviso que entra a la bandeja sale también como push, y lo manda la API. El
teléfono le dice a la API dónde está (su token de FCM), la API se lo pide a FCM con la cuenta de
servicio del proyecto, y tocar el aviso abre el pedido. El modelo pasa a **once entidades**: la
nueva es `DispositivoPush`.

---

## 🔄 Qué cambia desde la versión 8

| | `version-08` | `version-09` |
|---|---|---|
| **Los estados** | color y texto decididos en cada pantalla | **una tabla por dominio** en `utils/estados.js`: texto, tinte e icono |
| **Las listas** | items de Ionic en un panel | **tarjetas**: icono de estado, importe destacado, píldoras al pie |
| **Los filtros** | chips de texto | **indicadores** con el contador de la API, que también filtran |
| **La carga** | `ion-skeleton-text` | esqueletos propios con la forma del contenido |
| **Paginar** | botón «Ver más» | **scroll infinito**: el sensor reemplaza al botón y se apaga cuando no hay más |
| **El detalle** | modal clásico | **hoja deslizante** con la foto o el color del estado de fondo |
| **La foto** | se sube como sale | **recorte cuadrado** antes de subir (`cropperjs`) |
| **El catálogo** | un color para todo y la baja a un deslizamiento | **acento e icono por categoría**, «Ver N más» por categoría, chips de categoría; el deslizamiento alterna la disponibilidad y la baja pasa al formulario |
| **El cliente** | tocarlo abría la edición | **ficha propia**: contacto, datos y pedidos anteriores (`?cliente_id=`), y editar desde ahí |
| **La entrega** | datos sueltos y tres botones | **recorrido** como línea de tiempo, mapa, ruta y contacto, y el admin elige al repartidor |
| **El pedido** | se arma en el formulario, con la carta entera listada abajo | se arma **desde la vitrina**, con el borrador siempre a la vista; en el formulario, «Agregar productos» abre la carta en su hoja, con buscador y un «+» por fila |
| **Llegar a un pedido** | buscarlo a mano | `?pedido_id=` y `?buscar=` en Pedidos: el cartel de pedido creado abre la ficha; crear y escanear van en un botón flotante |
| **Usuarios y avisos** | `ion-note` sueltos y bajas de a una | **`comp_aviso`** con cuatro tonos para errores y obligatorios, baja en lote con la sesión actual marcada, la notificación abre su pedido y las barras del sistema siguen el tema |
| **La push** | se manda a mano desde la consola de Firebase | **la manda la API** con cada aviso de la bandeja; tocarla abre el pedido |
| **La API** | 10 entidades, sus endpoints | los mismos endpoints con las mismas respuestas, más `DispositivoPush`, `PUT /api/dispositivos`, el envío a FCM y un controller de prueba para vaciar los pedidos |

---

## 🧰 Qué hay que instalar

**Nada en la máquina**: `cropperjs` entra con `npm install` y es una librería web, no un plugin, y
`FirebaseAdmin` entra con el `restore` de la API. Lo nuevo es una credencial: la **cuenta de
servicio** de Firebase, sección 11 de [`README.comandos.md`](README.comandos.md). Ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Qué es un sistema visual: la escala tipográfica y el tinte de estado como reglas, no como estilos
2. Rediseño por componentes: tarjetas, píldoras, esqueletos e indicadores de resumen
3. Listas como tarjetas: jerarquía, agrupación y referencias al pie
4. Fichas como hoja deslizante, con la foto o el color del estado de fondo
5. Scroll infinito: el sensor que mide alto 0 y por qué nunca dispara
6. El recorte de la foto antes de subirla: encuadre cuadrado y peso del archivo
7. El pedido armado desde la vitrina: el carrito pasa a Pinia porque ahora es estado compartido
8. Los obligatorios controlados en el formulario, con el motivo a la vista
9. Herramientas de prueba: el borrado total de pedidos, cerrado a admin y en su propio controller
10. La push automática: el token del teléfono en la API, la cuenta de servicio y el envío a FCM al guardar el aviso

---

## 🖥️ Qué se ve en pantalla

**Todo, y eso es lo raro.** Ninguna pantalla queda igual, y todas quedan parejas: las listas como tarjetas con el tinte
de su estado, los indicadores que cuentan y filtran, los esqueletos que ocupan el lugar del
contenido, la ficha del plato con la foto de fondo, el borrador del pedido flotando sobre la
carta. Para ver el esqueleto hay que bajar la red a *Slow 3G*: con la API local no se alcanza a
ver.

> El momento que hace la semana es correr `git diff version-08..version-09 --stat -- RavEat.Api/`
> y mostrar el resultado: **decenas de archivos de interfaz cambiados y, del lado de la API, solo
> lo que suma**: el controller de prueba y la push. Ninguna respuesta que ya existía cambió.

Y la push, con dos sesiones: el navegador cambia el estado de un pedido y al teléfono, con la app
cerrada, le llega el aviso sin que nadie abra la consola de Firebase.

---

## 🔑 Mejorar la interfaz no toca el contrato

La tentación es «ya que estoy, le agrego un campo a la respuesta». Ese es el momento exacto en que
el frontend deja de ser reemplazable. El corte de la versión es cambiar la interfaz completa y que
los endpoints queden como estaban.

Y un **sistema visual** son reglas, no estilos sueltos por pantalla. Acá son tres archivos. Las
**variables de diseño** son los valores con nombre —`--raveat-radio: 16px`— que se deciden una vez
y usa toda la app; en otros textos se las llama *design tokens*, pero acá «token» ya es el de la
sesión y el del teléfono:

| Archivo | Qué fija |
|---|---|
| `theme/global.css` | las variables de diseño (`--raveat-radio`, `--raveat-sombra`, `--raveat-esqueleto`), la escala de tres pasos y los tintes `.raveat-tinte-*` |
| `theme/componentes.css` | la forma de cada pieza: fila-tarjeta, indicador, hoja, borrador, recorte |
| `utils/estados.js` | qué texto, color e icono tiene cada estado de cada dominio |

Sin las reglas, cada pantalla nueva vuelve a decidir todo de cero, y decide distinto.

---

## 🪜 El orden de la refactorización

Mejorar una app que ya funciona tiene un orden, y es el de los commits de esta rama: **de lo
general a lo particular**. Si se arranca por una pantalla, cada pantalla vuelve a inventar sus
colores y sus espacios; si se normaliza primero, corregir cada pantalla es corto.

| Paso | Commit | Qué se hace | Por qué en este lugar |
|---|---|---|---|
| 1 | `refactor(normalizacion)` | variables de diseño, escala, tintes y la tabla de estados; la API devuelve el texto del estado | es lo que todas las pantallas van a usar: se decide una vez |
| 2 | `refactor(componentes)` | esqueleto, lista con scroll infinito, aviso, indicadores, hoja de detalle, panel de resumen | las piezas que se repiten, hechas una vez sobre las variables de diseño |
| 3 | `feat(mejoras)` | recorte de la foto, pedido desde la carta, buscador de productos, fichas de cliente y de entrega, baja en lote, borrado de prueba | lo nuevo ya nace con las piezas y las variables de diseño |
| 4 | `fix(diseno)` | cada pantalla al sistema: márgenes, jerarquía, confirmaciones, obligatorios con motivo, barras del sistema | con todo lo anterior, cada pantalla es un ajuste |
| 5 | `feat(push)` | el token del teléfono, el envío desde la API y el toque que abre el pedido | aparte de la interfaz: no depende de ella |

Para tu app vale el mismo orden: primero tus variables de diseño y tus estados, después tus piezas, después lo
nuevo y al final cada pantalla. Cada paso compila solo, así que podés parar en cualquiera.

---

## 🧭 Dieciséis reglas de diseño

Son las buenas prácticas que la versión aplica y que sirven para cualquier dominio. Cada una se
reconoce comparando la misma pantalla en `version-08` y en `version-09`.

| # | Regla | Cómo se aplica acá |
|---|---|---|
| 1 | **Un sistema visual son reglas** | radio, sombra, grises y escala se fijan una vez en `global.css`; ninguna pantalla los vuelve a decidir |
| 2 | **El estado se lee por color e ícono** | `utils/estados.js` da texto, tinte e ícono de cada estado; la API manda el texto con `Texto()`, nunca `en_preparacion` |
| 3 | **Lo importante, grande; lo secundario, gris** | cada fila es una tarjeta: nombre e importe arriba, código y fecha al lado, píldoras al pie |
| 4 | **Los números que importan, arriba y tocables** | el contador de cada estado es también el filtro de la lista |
| 5 | **Esperar con la forma del contenido** | `comp_esqueleto` dibuja tarjetas vacías del mismo tamaño que las reales |
| 6 | **Una pantalla vacía dice qué hacer** | sin datos dice por qué y ofrece la acción; sin conexión dice otra cosa |
| 7 | **La ficha muestra lo que es** | la foto o el color del estado arriba, y la hoja con los datos desliza por encima |
| 8 | **Las acciones al lado de los datos** | cambiar estado, asignar repartidor o llamar al cliente se hacen desde la ficha |
| 9 | **Un botón apagado dice por qué** | `comp_aviso` muestra el obligatorio que falta debajo del formulario |
| 10 | **Lo que no se deshace, se confirma** | cancelar, dar de baja o borrar todo pregunta nombrando la cosa y lo que se pierde |
| 11 | **Lo que se está armando, siempre a la vista** | el borrador del pedido flota sobre la carta con la cantidad y el total |
| 12 | **En una lista larga, buscar; no scrollear** | «Agregar productos» abre la carta con buscador y un «+» por fila que se vuelve contador |
| 13 | **El pulgar manda** | crear y escanear en el botón flotante, abajo a la derecha; los toques miden 48 px |
| 14 | **Los formularios flotan** | un formulario corto es una tarjeta sobre la pantalla, sin borde blanco de más |
| 15 | **Claro y oscuro con las mismas variables** | el tema cambia variables, no pantallas; la barra de estado se pinta como el header |
| 16 | **El aire también es una regla** | `.raveat-page-stack` pone el mismo espacio entre pieza y pieza en todas las pantallas |

---

## 🔔 La push la manda la API

En la versión 7 la push necesitaba a alguien en la consola de Firebase. Para que salga sola faltan
dos cosas que la API no tenía: **a dónde** mandarla y **con qué permiso** pedírsela a FCM.

| Pieza | Dónde | Qué hace |
|---|---|---|
| El token del teléfono | `stores/sesion_store` → `PUT /api/dispositivos` | al entrar, al restaurar la sesión y cuando FCM lo entrega, la app lo anota en `dispositivos_push` |
| La baja | `POST /api/sesion/logout` con `token_push` | al salir, el teléfono deja de recibir las push de ese usuario |
| La cuenta de servicio | `Firebase:CuentaDeServicio` (User Secrets o el panel) | el JSON con clave privada que autoriza a la API ante FCM; sin él no hay push, y todo lo demás anda |
| El envío | `Push/PushAlGuardar` + `Push/EnvioPush` | cada `Notificacion` nueva sale como push **después** del `SaveChanges`, a todos los teléfonos del destinatario |
| El toque | `main.js` → `al_tocar` | abre el pedido si el rol entra a Pedidos; si no, la bandeja |
| El banner | canal `avisos` (importancia alta) + `Priority.High` en la API | la push aparece flotando sobre la pantalla, no solo en la cortina |

**Por qué al guardar y no en cada controller.** Pedidos y entregas escriben la bandeja en tres
lugares. Con el envío repetido en cada uno, el cuarto aviso que se agregue se olvida de la push. Un
`SaveChangesInterceptor` lo hace una vez para todos: si entra a la bandeja, sale como push.

**Por qué después de guardar.** Si el `SaveChanges` falla, la acción no pasó y no hay nada que
avisar. Y si FCM no responde, el pedido ya cambió y el aviso quedó en la bandeja: la push es un
extra, nunca una condición.

**Un usuario por rol para probarlo.** La bandeja y la push nunca le avisan al que ejecuta, así que
con el administrador solo no llega ningún aviso. La migración `UsuariosDePrueba` siembra vendedor,
proceso, caja y delivery con la clave del taller —en producción, la de `UsuariosDePrueba__Password`—: el vendedor crea, proceso cambia el estado, y la
push le llega al vendedor.

**Dos JSON de Firebase, y no son lo mismo.** `google-services.json` va adentro del APK y es
público: identifica la app. La cuenta de servicio tiene una clave privada: identifica a la API, y
si se filtra, cualquiera manda push en nombre del proyecto. Ninguno de los dos entra al
repositorio, pero por motivos distintos.

---

## 📐 Tres piezas que conviene leer de cerca

**El sensor del scroll infinito.** `ion-infinite-scroll` ignora un elemento de alto 0, y con el
spinner condicional adentro el sensor medía 0 hasta el primer disparo, que por eso no llegaba
nunca. El contenedor del aviso se dibuja siempre, con alto mínimo. Y `comp_lista` recibe la
acción `cargar_mas` como función, no como evento: la espera para avisarle al sensor que terminó.

**La hoja deslizante.** La foto queda clavada arriba con `position: sticky` y la hoja, con un
`z-index` mayor y un margen negativo, le desliza por encima al scrollear. Ese par es todo el
truco: sin header ni librerías. Sin foto, el fondo es un degradado del color del estado.

**El carrito en Pinia.** Hasta la versión 8 vivía en el `data()` del formulario, porque nacía y
moría con el modal. Ahora se arma en la vitrina y se cierra en Pedidos: es estado compartido entre
pantallas, y eso es lo único que decide si algo va a un store. La regla no cambió; cambió el ciclo
de vida del dato.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEatApp/
│  └─ src/
│     ├─ components/base/      comp_lista (scroll infinito), comp_esqueleto, comp_resumen_indicadores,
│     │                        comp_modal_detalle (la hoja), comp_aviso, comp_contacto_botones
│     ├─ components/estructura/ comp_panel_resumen (la cabecera de las pantallas de gestión)
│     ├─ components/dominio/   las fichas de producto, pedido, entrega y cliente; la tarjeta con el carrito
│     ├─ stores/carrito_store  el borrador del pedido, compartido entre Inicio y Pedidos
│     ├─ theme/                global.css (variables de diseño, escala, tintes) y componentes.css (las piezas)
│     ├─ utils/categorias.js   acento e icono de cada categoría
│     ├─ services/dispositivos_service  el token del teléfono hacia la API
│     ├─ services/notificaciones_nativas_service  registro en FCM, push recibida y push tocada
│     ├─ utils/estados.js      texto, color e icono de cada estado, por dominio
│     └─ views/                todas las pantallas, al nuevo lenguaje
└─ RavEat.Api/
   ├─ Controllers/MantenimientoController.cs   el borrado de prueba de los pedidos
   ├─ Controllers/DispositivosController.cs    PUT /api/dispositivos: el teléfono se anota
   ├─ Domain/Entities/DispositivoPush.cs       la entidad once: usuario y token de FCM
   ├─ Push/EnvioPush.cs                        la API le pide a FCM la push, con la cuenta de servicio
   ├─ Push/PushAlGuardar.cs                    cada aviso nuevo de la bandeja sale como push
   ├─ Migrations/…_DispositivosPush            la tabla dispositivos_push
   └─ Migrations/…_UsuariosDePrueba            un usuario por rol, para probar quién avisa a quién
```

---

## ✅ Terminaste cuando

- [ ] `git diff version-08..version-09 --stat -- RavEat.Api/` solo suma: ninguna respuesta que ya existía cambió
- [ ] Las listas son tarjetas, con el tinte de estado consistente en toda la app
- [ ] Con *Slow 3G*, cada pantalla muestra su esqueleto con la forma del contenido
- [ ] Las listas paginan con scroll infinito y el sensor se apaga en la última página
- [ ] La ficha del plato se abre desde la vitrina con la foto de fondo
- [ ] La foto del producto se recorta cuadrada antes de subir
- [ ] El pedido se arma desde la vitrina y el borrador se ve mientras se elige
- [ ] En el formulario del pedido, «Agregar productos» abre la carta con buscador; el «+» suma y se vuelve contador
- [ ] Cada categoría tiene su acento en la vitrina y en Productos, y la vitrina muestra de a 10 con «Ver N más»
- [ ] En Productos, deslizar alterna Disponible / No disponible y el admin crea una categoría sin salir del formulario
- [ ] La ficha del cliente lista sus pedidos anteriores y tocar uno abre su ficha en Pedidos
- [ ] La ficha de la entrega muestra el recorrido y el admin le asigna repartidor desde ahí
- [ ] Al crear un pedido desde la carta, «Ver pedido» abre su ficha, y la ficha comparte sus datos como texto
- [ ] Usuarios: «Seleccionar» permite dar de baja varios a la vez, y la propia cuenta aparece como «Sesión actual» y no se puede marcar
- [ ] Tocar una notificación que nombra un pedido abre ese pedido; la leída muestra cuándo se leyó
- [ ] El administrador puede borrar todos los pedidos desde Mi cuenta, escribiendo BORRAR
- [ ] La consola de la API no dice `Push apagado`, y `dispositivos_push` tiene una fila por teléfono con sesión
- [ ] Un cambio de estado hecho desde el navegador le llega como push al creador del pedido, con la app cerrada, y tocarla abre el pedido
- [ ] La push aparece como banner flotante sobre la pantalla, con el gorro de RavEat como icono
- [ ] Con la app abierta, la push aparece como aviso del sistema y la campanita sube
- [ ] Después de **Salir**, ese teléfono ya no recibe las push del usuario
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
