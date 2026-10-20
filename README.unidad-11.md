# U11 · Preparación, historial y firma

> Lo que se registra va en el mismo guardado que la acción. Quien prepara mueve ítems y el pedido
> resume; el historial lo escribe la acción que lo produce; la firma de quien recibe queda como
> imagen junto a la entrega.

| | |
|---|---|
| **Rama** | `version-11` |
| **Viene de** | `version-10` — entrada de datos sin teclado |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**Que lo que pasa adentro del local quede dicho, y bien dicho.** Hasta la versión 10 el pedido
pasaba de *confirmado* a *listo* con un botón, sin que quien prepara dijera qué salió y qué no; nadie
podía saber quién lo había movido; y una entrega se cerraba sin la conformidad de quien la recibió.

Ahora quien prepara tiene su pantalla, **Preparación**, y marca **ítem por ítem**: el estado del pedido no
se toca, se **deduce** de sus ítems. Cada cambio de estado deja una fila en el **historial**, de
solo lectura. Y la entrega en camino se **firma con el dedo** y sube como imagen.

Son tres cosas distintas con una misma regla: **o pasan junto con el cambio que las produjo, o no
pasan.** Un historial que dice que el pedido pasó a *listo* cuando el pedido no cambió es peor que
no tener historial. El modelo llega a **doce entidades**: la nueva es `Historial`.

---

## 🔄 Qué cambia desde la versión 10

| | `version-10` | `version-11` |
|---|---|---|
| **Preparación** | no había | **Preparación**: los pedidos confirmados en orden de llegada, movidos ítem por ítem |
| **El ítem del pedido** | nombre, precio y cantidad | + su **estado**: sin empezar, preparando o listo |
| **El estado del pedido en preparación** | se movía con un botón | desde **Preparación** se **calcula** de los ítems; al quedar todo listo se avisa a quien lo tomó |
| **Historial** | no había | quién hizo qué y cuándo, escrito **en el mismo `SaveChanges`** que la acción |
| **La entrega** | se cierra con un botón | + la **firma de recepción**, dibujada en pantalla y guardada como PNG |
| **La base** | — | tres migraciones: `Comandas`, `Historial`, `FirmaEntrega` |

---

## 🧰 Qué hay que instalar

**Nada**: ni programa ni plugin. La firma es un `<canvas>` con tres eventos de puntero. Ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. La vista de otro rol **no es una entidad**: es el mismo pedido
2. Estado por ítem: un pedido no se prepara de una sola vez
3. El estado del pedido se **deduce** de sus ítems, nunca se mueve a mano desde **Preparación**
4. Dos datos que se pueden mover por separado terminan diciendo cosas distintas
5. Auditoría centralizada: una sola forma de escribir el registro, sin `SaveChanges` propio
6. El historial es de **solo lectura**: no hay endpoint para escribirlo
7. Quién ve qué historial lo decide el token, no un parámetro
8. Dar de baja a alguien no puede borrar lo que hizo: `Restrict` en vez de `Cascade`
9. La conformidad de quien recibe: la firma en un `<canvas>` con tres eventos de puntero
10. La firma se guarda como **imagen**, no como lista de puntos

---

## 🖥️ Qué se ve en pantalla

| Pantalla | Qué hace ahora |
|---|---|
| **Preparación** | nueva, para administrador y proceso: los pedidos en orden de llegada, con cuántos ítems salieron y cuánto llevan esperando |
| **Ficha de preparación** | un botón redondo por ítem —empezar, salió— y **Marcar todo listo** |
| **Historial** | nuevo: quién hizo qué y cuándo. El administrador ve el del local; los demás, lo suyo |
| **Ficha de la entrega** | en camino o entregada, **Firmar recepción**; ya firmada, la firma |

> El momento que hace la semana es tener **Preparación** en un teléfono y **Pedidos** en otro: marcar el
> último ítem en uno y ver que en el otro el pedido pasa a *listo* solo, con su aviso y su fila de
> historial.

---

## 🧮 Un dato derivado no se guarda dos veces

Un pedido no se prepara de una sola vez —la ensalada sale antes que el guiso—, así que el estado
real de la preparación está **en cada ítem**. El estado del pedido no es otro dato: es un **resumen** de
esos ítems. Por eso `/api/comandas` no recibe el estado del pedido, solo el del ítem, y la API lo
recalcula:

| Ítems | El pedido queda |
|---|---|
| todos sin empezar | `confirmado` |
| alguno preparando, o algunos listos y otros no | `en_preparacion` |
| todos listos | `listo` — y se avisa a quien lo tomó |

| Pieza | Dónde | Qué hace |
|---|---|---|
| `EstadoItemPedido` | `Domain/Enums/Estados.cs` | el segundo juego de estados, el de preparación, por ítem |
| `RecalcularEstadoPedidoAsync` | `ComandasController` | la regla completa, escrita una sola vez |
| Consulta de entidades | `RecalcularEstadoPedidoAsync` | lee el ítem recién marcado con su valor nuevo, desde el change tracker |
| `items_de` | `stores/comandas_store` | junta pedidos e ítems, que la API manda en dos listas |

**Por qué entidades y no una proyección.** El ítem que se acaba de marcar todavía no se guardó.
Una proyección (`Select(x => x.Estado)`) va a la base y trae el valor **viejo**: el pedido quedaba
siempre un paso atrás de sus ítems. La consulta de entidades pasa por el change tracker y devuelve
la fila ya modificada.

Es la misma idea, mirada al revés, que el precio copiado en el ítem (versión 4): ahí se duplica **a
propósito** porque el valor tiene que congelarse en el tiempo. Acá no se duplica porque tiene que
seguir vivo.

---

## 📜 Lo que pasó no se edita

El historial es la única tabla **sin ABM**: no hay endpoint para escribirla. La escribe
`Data/Auditoria.cs` desde las acciones que registra, y **no llama a `SaveChanges`**: agrega la fila
al mismo cambio que la acción, así que se guardan las dos cosas o ninguna.

| Acción | Queda registrado |
|---|---|
| Cambiar el estado de un pedido | el estado anterior y el nuevo |
| Cobrar | `Pendiente → Pagado`, con el medio de pago |
| Cancelar | el estado de donde salió |
| Movimiento de preparación | el estado que calculó la API con los ítems |
| Tomar o asignar una entrega | `Pendiente → Asignada`, y si la tomó el repartidor o se la asignaron |
| Cambiar el estado de una entrega | el estado anterior y el nuevo |
| Firmar | «Se registró la firma de recepción» |

Tampoco tiene baja lógica, y su clave foránea al usuario es **`Restrict` en vez de `Cascade`** —al
revés que la bandeja de notificaciones—: dar de baja a alguien no puede borrar lo que hizo. Y
quién ve qué lo decide **el token, no un parámetro**: con un `?usuario_id=` alcanzaría con cambiar
el número para leer lo de otro.

El historial guarda los estados **crudos**, con el nombre del enum (`EnPreparacion`). El texto lo
arma la pantalla: las etiquetas de la interfaz cambian con el tiempo y el registro no.

---

## ✍️ La firma es una imagen, no un trazo

| Pieza | Dónde | Qué hace |
|---|---|---|
| `<canvas>` | `comp_firma_entrega_canvas` | `pointerdown`, `pointermove`, `pointerup`: un trazo, sin librería |
| `touch-action: none` | `.raveat-firma__lienzo` | sin esto, firmar arrastra la pantalla en vez de dibujar |
| `devicePixelRatio` | `preparar_lienzo` | el mapa de bits al tamaño real de la pantalla: si no, la firma sale pixelada |
| `ResizeObserver` | `comp_firma_entrega_canvas` | el lienzo nace en un modal que todavía se está abriendo; se prepara otra vez con el ancho final |
| `toBlob` | `obtener_archivo` | el PNG listo para el `FormData` |
| `PUT /api/entregas/{id}/firma` | `EntregasController` | multipart, como la foto del producto: el nombre del archivo lo decide el servidor |

Guardar la lista de puntos obligaría a redibujarla en cada pantalla que quiera verla, y a mantener
ese código para siempre. Un PNG se muestra con un `<img>`.

Y solo se firma una entrega **en camino o ya entregada**: antes no hay a quién pedirle conformidad.
La firma el repartidor que la lleva, o el administrador. Sin red no se encola: un `FormData` no
sobrevive guardado y llegaría vacío.

---

## 🌐 La API

Lo que suma esta versión al contrato de la 10:

| Método | Ruta | Devuelve |
|---|---|---|
| `GET` | `/api/comandas` | `{comandas, items, pagina, resumen, total}` — los pedidos en preparación, **en orden de llegada** |
| `PUT` | `/api/comandas/items/{id}/estado` | mueve un ítem y **recalcula** el estado del pedido |
| `PUT` | `/api/comandas/{id}/listo` | «salió todo»: marca todos los ítems y deja el pedido en `listo` |
| `GET` | `/api/historial` | `{historial, pagina, resumen, total}` — con `tipo`, `busqueda` y `solo_mios` |
| `PUT` | `/api/entregas/{id}/firma` | **multipart** con `firma` (PNG, ≤ 2 MB). Solo en camino o entregada |

El detalle del pedido suma el `estado` de cada ítem, y la lista de entregas la `firma_url`.
Preparación es de `ADMIN` y `PROCESO`; el historial lo ven los cinco roles, **cada uno lo suyo**.

---

## 📁 Cómo está organizado

```
RavEat/
├─ RavEat.Api/
│  ├─ Controllers/ComandasController.cs            preparación: lista, mueve ítems, recalcula el pedido
│  ├─ Controllers/HistorialController.cs           solo lectura; el alcance sale del token
│  ├─ Controllers/EntregasController.cs            más la firma
│  ├─ Data/Auditoria.cs                            una sola forma de escribir el registro, sin SaveChanges propio
│  ├─ Domain/Entities/Historial.cs                 la entidad doce
│  └─ Migrations/                                  + Comandas, Historial, FirmaEntrega
└─ RavEatApp/
   └─ src/
      ├─ views/comandas_page.vue                   Preparación
      ├─ views/historial_page.vue                  Historial
      ├─ components/dominio/comp_comanda_detalle_modal.vue   la ficha de preparación, ítem por ítem
      ├─ components/base/comp_firma_entrega_canvas.vue       el lienzo: no sabe de entregas ni de la API
      ├─ components/dominio/comp_entrega_detalle_modal.vue   Firmar recepción y la firma guardada
      └─ theme/componentes.css                     .raveat-comanda-item y .raveat-firma
```

---

## ✅ Terminaste cuando

- [ ] **Preparación** aparece para administrador y proceso, y en los tabs solo para proceso
- [ ] Preparación muestra los pedidos confirmados en orden de llegada, con los ítems listos y la espera
- [ ] Empezar un ítem pasa el pedido a **En preparación** solo; sacar el último lo deja **Listo**
- [ ] Al quedar listo, a quien tomó el pedido le llega el aviso
- [ ] **Marcar todo listo** deja todos los ítems listos de un toque
- [ ] El cambio de estado y su fila de historial se guardan **en el mismo `SaveChanges`**
- [ ] Historial: el administrador ve el movimiento del local; otro rol, solo lo suyo
- [ ] No existe forma de escribir el historial por la API: no hay endpoint
- [ ] La entrega en camino se firma con el dedo y la firma queda como imagen en la ficha
- [ ] Firmar una entrega en otro estado devuelve error
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
