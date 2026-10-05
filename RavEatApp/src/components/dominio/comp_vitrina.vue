<template>
	<!-- La vitrina muestra tarjetas: el esqueleto tambien. Con la variante de lista, al
	     llegar los datos la pantalla cambiaba de forma de golpe. -->
	<comp-esqueleto v-if="productos_store.cargando && !productos_store.hay_productos" :filas="6" variante="tarjetas" />
	<comp-estado-error
		v-else-if="productos_store.error"
		:mensaje="productos_store.error"
		@reintentar="cargar"
	/>
	<comp-estado-vacio
		v-else-if="!productos_store.hay_productos"
		:icono="icono_carta"
		titulo="Todavía no hay productos"
		mensaje="La API respondió, pero la carta está vacía."
	/>
	<template v-else>
		<div class="raveat-chips">
			<button
				v-for="chip in chips"
				:key="chip.id"
				type="button"
				class="raveat-chip"
				:class="{ activo: String(chip.id) == String(categoria_activa) }"
				@click="categoria_activa = chip.id"
			>
				{{ chip.nombre }}
			</button>
		</div>

		<section v-for="grupo in grupos" :key="grupo.id">
			<!-- El titulo toma el acento de la categoria: la variable pinta la barrita y el texto
			     en global.css, y es el mismo color que el rotulo de la fila de productos. -->
			<h2 class="raveat-seccion-titulo" :style="{'--raveat-seccion-acento': `var(--ion-color-${grupo.acento})`}">{{ grupo.nombre }}</h2>
			<div class="raveat-grilla">
				<comp-producto-card
					v-for="producto in grupo.visibles"
					:key="producto.id"
					:producto="producto"
					@seleccionar="abrir_producto"
				/>
			</div>
			<!-- Esto NO es paginacion: la carta entera ya llego en una respuesta, y el tope
			     recorta el dibujo, no los resultados. El boton dice CUANTOS faltan: "ver mas" a
			     secas no deja saber si queda uno o cuarenta. -->
			<ion-button
				v-if="grupo.restantes > 0"
				class="raveat-seccion__ver-mas"
				expand="block"
				fill="outline"
				@click="ver_mas(grupo.id)"
			>
				Ver {{ grupo.proxima_tanda }} más ({{ grupo.restantes }} sin mostrar)
			</ion-button>
		</section>
	</template>

	<!-- La ficha del plato vive con la vitrina: la abren las dos —la de adentro y la publica—
	     y no pide nada a la API, asi que cualquiera la puede ver. -->
	<comp-producto-detalle-modal
		:abierto="detalle_abierto"
		:producto="producto_detalle"
		@cerrar="cerrar_producto"
	/>
</template>

<script>import { IonButton } from '@ionic/vue';
import { restaurantOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_producto_card from '@/components/dominio/comp_producto_card.vue';
import comp_producto_detalle_modal from '@/components/dominio/comp_producto_detalle_modal.vue';
import { acento_categoria } from '@/utils/categorias';
import { use_productos_store } from '@/stores/productos_store';

// Cuantos platos se dibujan por categoria antes del boton de ver mas.
const POR_TANDA = 10;

// La carta con sus chips y sus tres estados —cargando, error y vacio—, sin cascara de pagina.
// Se extrae de inicio_page en v7 porque ahora la misma vitrina se dibuja en dos lugares: la de
// adentro (/app/inicio, con menu y tabs) y la publica (/vitrina, sin sesion). Una sola copia de
// la carta: si se duplicaba, las dos vitrinas se iban a separar en el primer cambio.
// El chip de categoria filtra en memoria a proposito: la carta entera ya esta en el store.
// Sin nodo raiz propio, a proposito: las secciones tienen que seguir siendo hijas directas de
// .raveat-page-stack. Envueltas en un div, la columna del grid se estiraba al ancho de la fila
// de chips y la pantalla entera se salia por la derecha en el telefono.
export default {
	name: 'comp_vitrina',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompProductoCard: comp_producto_card,
		CompProductoDetalleModal: comp_producto_detalle_modal,
		IonButton
	},
	data(){
		return {
			categoria_activa: 'todas',
			detalle_abierto: false,
			icono_carta: restaurantOutline,
			// Cuantos van a la vista por categoria; la que no figura muestra la primera tanda.
			mostrados: {},
			producto_detalle: null,
			productos_store: use_productos_store()
		};
	},
	computed: {
		// Los chips salen de las categorias que informa la API, no de los productos en pantalla:
		// con una categoria elegida, derivarlos de la lista dejaria sin forma de volver al resto.
		chips(){
			var vm = this;
			return [
				{id: 'todas', nombre: 'Todos'},
				...vm.productos_store.categorias.map(categoria => ({id: categoria.id, nombre: categoria.nombre}))
			];
		},
		productos_visibles(){
			var vm = this;
			if(vm.categoria_activa == 'todas') return vm.productos_store.productos;
			return vm.productos_store.productos.filter(producto => String(producto.categoria_id) == String(vm.categoria_activa));
		},
		grupos(){
			var vm = this;
			const grupos = [];
			vm.productos_visibles.forEach(producto =>{
				let grupo = grupos.find(item => item.id == producto.categoria_id);
				if(!grupo){
					grupo = {id: producto.categoria_id, nombre: producto.categoria_nombre, productos: []};
					grupos.push(grupo);
				}
				grupo.productos.push(producto);
			});
			// El recorte se resuelve aca: el template pide visibles y restantes ya calculados.
			return grupos.map(grupo =>{
				const tope = vm.mostrados[grupo.id] || POR_TANDA;
				const restantes = Math.max(0, grupo.productos.length - tope);
				return {
					...grupo,
					acento: acento_categoria(grupo.id),
					visibles: grupo.productos.slice(0, tope),
					restantes,
					proxima_tanda: Math.min(POR_TANDA, restantes)
				};
			});
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		// Publico a proposito: las paginas que la contienen lo llaman desde su "deslizar para
		// actualizar" con un ref.
		cargar: function(){
			var vm = this;
			return vm.productos_store.cargar_productos();
		},
		ver_mas: function(categoria_id){
			var vm = this;
			vm.mostrados[categoria_id] = (vm.mostrados[categoria_id] || POR_TANDA) + POR_TANDA;
		},
		abrir_producto: function(producto){
			var vm = this;
			vm.producto_detalle = producto;
			vm.detalle_abierto = true;
		},
		cerrar_producto: function(){
			var vm = this;
			vm.detalle_abierto = false;
			vm.producto_detalle = null;
		}
	}
};</script>
