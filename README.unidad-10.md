# U10 · Entrada de datos sin teclado

> El dato correcto no siempre se escribe: a veces se **señala** y a veces se **dicta**. Y cada una
> de esas formas falla de un modo que el teclado no tiene. Diseñar el camino alternativo no es un
> extra de la versión: es la versión.

| | |
|---|---|
| **Rama** | `version-10` |
| **Viene de** | `version-09` — interfaz normalizada y push automática |
| **Stack** | Ionic Vue 3 · Capacitor 8 · Pinia 3 · Vite + ASP.NET Core (.NET 10) · EF Core 9 · MySQL 8 |
| **Corre en** | navegador y teléfono Android |
| **Requisitos** | [`README.requisitos.md`](README.requisitos.md) |
| **Comandos** | [`README.comandos.md`](README.comandos.md) |

---

## 🎯 Objetivo

**Que el usuario no tenga que tipear lo que no sabe o lo que le cuesta.** Nadie sabe la latitud de
la casa de un cliente, y hasta la versión 9 la única forma de cargarla era estar parado en la
puerta con el GPS. Ahora el punto se **señala en un mapa**, desde el mostrador o desde donde sea.
Y la referencia de una dirección («portón verde, timbre roto, entre calles») o la descripción de
un plato son textos largos que en un teléfono se dicen más rápido de lo que se escriben: ahora se
**dictan**.

Las dos entradas tienen algo en común: dependen de algo que puede faltar. El mapa necesita red y
un servidor de tiles; el dictado, un servicio de reconocimiento del sistema y el permiso del
micrófono. Por eso cada una tiene su camino manual, y el campo de texto sigue estando siempre.

La API no cambia: las coordenadas y los textos ya existían. Cambia **cómo llegan**.

---

## 🔄 Qué cambia desde la versión 9

| | `version-09` | `version-10` |
|---|---|---|
| **El punto del cliente** | solo con el GPS, estando en la puerta (delivery y admin) | **se señala en un mapa**, cualquier rol, desde cualquier lado; el GPS queda para el que está ahí |
| **El destino del pedido** | sin punto; había que ir a Clientes con el GPS | **«Marcar en el mapa»** desde el mismo formulario, y el punto va a la ficha del cliente |
| **La dirección escrita** | el respaldo para abrir la app de mapas | además, **el buscador del mapa**: la dirección centra el mapa y el pin se termina de poner a mano |
| **Referencia, observaciones, descripción** | se tipean | **se dictan** con el micrófono del campo, o se tipean |
| **El micrófono** | — | aparece **solo si el teléfono sabe dictar**: sin reconocedor no hay botón |
| **Android** | — | un plugin: `@capacitor-community/speech-recognition`, con `RECORD_AUDIO` por fusión de manifiestos |
| **La API** | — | **sin cambios**: los mismos endpoints, las mismas columnas |

---

## 🧰 Qué hay que instalar

**Nada en la máquina.** Entra un plugin con `npm install` y lo registra `npx cap sync android`. En
el teléfono hace falta el servicio de reconocimiento de voz, que viene con la app de Google. Ver
[`README.requisitos.md`](README.requisitos.md).

---

## 📚 Temas

1. Por qué un dato mal ingresado no siempre es culpa del usuario: nadie sabe su latitud
2. El mapa de tiles escrito a mano: proyección Web Mercator en veinte líneas, sin librería
3. El pin fijo al centro: se mueve el mapa debajo del pin, no el pin sobre el mapa
4. Arrastre y pellizco con pointer events
5. Sin claves de API: los tiles se piden a `tile.openstreetmap.org` como imágenes
6. El respaldo cuando no hay coordenadas: buscar por la dirección escrita
7. Dictado por voz: dónde sirve (texto libre y largo) y dónde estorba (un nombre, una altura)
8. Consultar antes de ofrecer: `SpeechRecognition.available()` y el botón que no debe aparecer
9. `partialResults: false`, y por qué con `true` el `then` resuelve vacío
10. La regla común: toda entrada asistida necesita su camino manual

---

## 🖥️ Qué se ve en pantalla

En **Clientes**, el formulario tiene **Marcar en el mapa**: se abre el mapa con la dirección escrita
en el buscador, se busca, se arrastra hasta que la punta del pin cae en la puerta y **Usar este
punto**. En el **pedido de entrega**, el mismo botón al lado del destino. Y al lado de cada texto
libre, un micrófono que late mientras escucha.

> El momento que hace la semana es abrir el formulario en un navegador que no sabe dictar (Firefox)
> y en el teléfono: **en uno el micrófono no está, en el otro sí**, y el formulario sirve igual en
> los dos.

---

## 📍 Señalar en vez de escribir

**Un mapa no necesita una librería de mapas.** El mundo está cortado en tiles de 256 × 256 por
nivel de zoom, y OpenStreetMap las sirve como imágenes: `tile.openstreetmap.org/{z}/{x}/{y}.png`.
Dibujar un mapa es calcular qué tiles cubren la pantalla y ponerlas con `left` y `top`. La única
matemática es la proyección Web Mercator, de grados a pixeles y vuelta:

| Pieza | Dónde | Qué hace |
|---|---|---|
| `a_pixel` / `a_grados` | `comp_selector_ubicacion` | la proyección: latitud y longitud ↔ pixel del plano en ese zoom |
| `tiles` (computed) | `comp_selector_ubicacion` | las tiles que cubren la superficie, con una de margen por lado |
| El pin | `.raveat-mapa__pin` | una gota de CSS **fija en el centro**; el punto elegido es siempre el centro |
| Arrastre y pellizco | `@pointerdown` / `@pointermove` | un dedo corre el centro; dos dedos cambian el zoom según su distancia |
| `touch-action: none` | `.raveat-mapa__superficie` | le quita al navegador el gesto: si no, el arrastre scrollea la página |
| El buscador | `buscar_direcciones` (Nominatim) | la dirección escrita centra el mapa; el pin se termina a mano |
| La dirección del punto | `buscar_direccion_de_punto` (Nominatim) | al confirmar, la dirección pasa a ser la del punto; el piso y el timbre van en la referencia |

**El pin va fijo al centro.** Arrastrar un pin con el dedo lo tapa justo cuando hay que ver dónde
cae. Se mueve el mapa debajo del pin, y así se marca la puerta y no la cuadra.

**Sin clave de API, pero con reglas.** Las tiles y Nominatim son gratis a cambio de dos cosas: la
atribución a la vista (`© OpenStreetMap`) y no abusar. Por eso el buscador consulta al enviar y no
con cada tecla.

**Y si el mapa no carga.** Sin red, ninguna tile llega: el mapa lo dice y ofrece lo que sí anda, la
dirección escrita. Un cliente sin punto es un estado válido: la app de mapas del repartidor busca
por la dirección.

---

## 🎙️ Dictar donde el texto es libre

El micrófono va donde el texto es largo y no tiene una forma exacta: la **referencia** de la
dirección, las **observaciones** del pedido y la **descripción** del plato. En el nombre, el
teléfono o la altura estorba: un nombre propio mal transcrito cuesta más corregirlo que tipearlo.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `dictado_disponible` | `services/dictado_service` | `SpeechRecognition.available()` en el teléfono; Web Speech API en el navegador |
| `v-if="disponible"` | `comp_dictado_boton` | sin reconocedor, el botón no se dibuja |
| `permiso_nativo` | `services/dictado_service` | el permiso del micrófono, pedido recién al primer toque |
| `dictar` | `services/dictado_service` | `{ok, texto, mensaje}`: el silencio no es un error, es texto vacío |
| `@actualizar` | `comp_dictado_boton` | emite el texto **sumado** al que había: dictar dos veces no borra lo primero |

**Consultar antes de ofrecer.** Que el plugin esté instalado no dice que el teléfono sepa dictar: el
reconocimiento lo presta un servicio del sistema que puede faltar. Un micrófono que no hace nada al
tocarlo es peor que no tenerlo.

**Primero el servicio, después el permiso.** Pedir el micrófono para un reconocedor que no existe
es molestar con un permiso que no sirve.

**`partialResults: false`.** Con `true`, `start()` resuelve en el acto y vacío, y el texto llega
por un evento aparte; escuchar solo el `then` era recibir siempre una cadena vacía. Con `false`,
resuelve cuando el reconocedor termina, y trae lo dictado.

---

## 🧭 La regla común

Toda entrada asistida necesita su camino manual. Es la misma idea de la cámara en la versión 6 y
del offline en la versión 8: la capacidad del teléfono se ofrece cuando está, y la app sirve igual
cuando no.

| Entrada | Puede fallar porque | El camino manual |
|---|---|---|
| El mapa | no hay red, o el servidor de tiles no responde | la dirección escrita; el repartidor la busca en su app de mapas |
| El buscador | Nominatim no encuentra la dirección | arrastrar el mapa hasta la puerta |
| El GPS | permiso negado, o el que carga no está en la puerta | el mapa |
| El dictado | sin servicio de voz, sin permiso o sin red | el teclado: el campo es escribible siempre |

---

## 📁 Cómo está organizado

```
RavEat/
└─ RavEatApp/
   └─ src/
      ├─ components/base/comp_selector_ubicacion.vue   el mapa: tiles, pin al centro, arrastre, pellizco, buscador
      ├─ components/base/comp_dictado_boton.vue        el micrófono: aparece solo si el teléfono sabe dictar
      ├─ services/dictado_service.js                   disponibilidad, permiso y dictado, nativo y web
      ├─ services/geolocalizacion_service.js           más buscar_direcciones (Nominatim)
      ├─ components/dominio/comp_cliente_formulario_modal.vue   Marcar en el mapa, referencia dictada
      ├─ components/dominio/comp_pedido_formulario_modal.vue    el punto del destino, observaciones dictadas
      ├─ components/dominio/comp_producto_formulario_modal.vue  la descripción dictada
      └─ theme/componentes.css                         .raveat-mapa y .raveat-dictado
```

---

## ✅ Terminaste cuando

- [ ] `git diff version-09..version-10 --stat -- RavEat.Api/` no muestra nada
- [ ] En el formulario del cliente, **Marcar en el mapa** abre el mapa con la dirección escrita en el buscador
- [ ] Buscar la dirección centra el mapa, y arrastrar mueve el mapa con el pin quieto en el centro
- [ ] En el teléfono, el pellizco acerca y aleja; en la PC, la rueda del mouse
- [ ] **Usar este punto** guarda las coordenadas y el cliente queda **Marcada**
- [ ] Y la **Dirección** pasa a ser la del punto; sin red queda la escrita
- [ ] Sin punto previo, **Usar este punto** está apagado hasta mover el mapa
- [ ] En un pedido de entrega, **Marcar en el mapa** guarda el punto en la ficha del cliente sin salir del pedido
- [ ] Con la red cortada, el mapa dice que no cargó y la dirección escrita alcanza para guardar
- [ ] El micrófono aparece en referencia, observaciones y descripción, y en ningún otro campo
- [ ] En un navegador sin reconocimiento de voz, el micrófono no aparece
- [ ] En el teléfono, el primer toque pide el permiso del micrófono; negado, avisa y el campo sigue escribible
- [ ] Dictar dos veces suma el texto, no lo reemplaza
- [ ] `npm run lint`, `npm run build` y `dotnet build` terminan sin errores

---

<sub>RavEat · Desarrollo Avanzado de Aplicaciones Móviles · Tecnicatura Universitaria en Desarrollo de Software · Universidad de La Punta</sub>
