<template>
	<comp-page titulo="RavEat" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-vidriera lema="Nuestra carta, servida por la API" />

			<comp-esqueleto v-if="productos_store.cargando" />
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
						:class="{ activo: String(chip.id) === String(categoria_activa) }"
						@click="categoria_activa = chip.id"
					>
						{{ chip.nombre }}
					</button>
				</div>

				<section v-for="grupo in grupos" :key="grupo.id">
					<h2 class="raveat-seccion-titulo">{{ grupo.nombre }}</h2>
					<div class="raveat-grilla">
						<comp-producto-card
							v-for="producto in grupo.productos"
							:key="producto.id"
							:producto="producto"
						/>
					</div>
				</section>
			</template>
		</div>
	</comp-page>
</template>

<script>import { restaurantOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_producto_card from '@/components/dominio/comp_producto_card.vue';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import { use_productos_store } from '@/stores/productos_store';

// La vitrina: lo primero que ve alguien que abre la app. Los mismos tres estados que la lista
// de Productos —cargando, error y vacio—, con otra presentacion.
// El chip de categoria filtra en memoria a proposito: en v3 la carta entera ya esta en el
// store. Cuando en v4 la lista pase a estar paginada, este filtro tiene que mudarse a la API,
// porque si no solo miraria la pagina descargada.
export default {
	name: 'inicio_page',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		CompProductoCard: comp_producto_card,
		CompVidriera: comp_vidriera
	},
	data(){
		return {
			categoria_activa: 'todas',
			icono_carta: restaurantOutline,
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
			if(vm.categoria_activa === 'todas') return vm.productos_store.productos;
			return vm.productos_store.productos.filter(producto => String(producto.categoria_id) === String(vm.categoria_activa));
		},
		grupos(){
			var vm = this;
			const grupos = [];
			vm.productos_visibles.forEach(producto =>{
				let grupo = grupos.find(item => item.id === producto.categoria_id);
				if(!grupo){
					grupo = {id: producto.categoria_id, nombre: producto.categoria_nombre, productos: []};
					grupos.push(grupo);
				}
				grupo.productos.push(producto);
			});
			return grupos;
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.productos_store.cargar_productos();
		}
	}
};</script>
