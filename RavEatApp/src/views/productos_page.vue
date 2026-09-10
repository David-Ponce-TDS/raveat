<template>
	<comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-esqueleto v-if="cargando" />
			<comp-estado-vacio
				v-else-if="productos.length === 0"
				:icono="icono"
				titulo="Todavía no hay productos"
				mensaje="Agregá productos a src/datos/carta.js para verlos acá."
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
				<div class="raveat-grilla">
					<comp-producto-card
						v-for="producto in productos_visibles"
						:key="producto.id"
						:producto="producto"
					/>
				</div>
			</template>
		</div>
	</comp-page>
</template>

<script>import { pricetagsOutline } from 'ionicons/icons';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_producto_card from '@/components/dominio/comp_producto_card.vue';
import { obtener_carta } from '@/datos/carta';

export default {
	name: 'productos_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		CompProductoCard: comp_producto_card
	},
	data(){
		return {
			cargando: true,
			categoria_activa: 'todas',
			categorias: [],
			icono: pricetagsOutline,
			productos: []
		};
	},
	computed: {
		// Los chips salen de las categorias, no de los productos en pantalla: con una categoria
		// elegida, derivarlos de la lista dejaria sin forma de volver al resto.
		chips(){
			var vm = this;
			return [
				{id: 'todas', nombre: 'Todos'},
				...vm.categorias.map(categoria => ({id: categoria.id, nombre: categoria.nombre}))
			];
		},
		productos_visibles(){
			var vm = this;
			if(vm.categoria_activa === 'todas') return vm.productos;
			return vm.productos.filter(producto => String(producto.categoria_id) === String(vm.categoria_activa));
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		// Los datos salen de un archivo, pero se piden async y con estado de carga, igual que
		// cuando vengan de la API en v3. Asi la pantalla no cambia al conectarla.
		cargar: async function(){
			var vm = this;
			vm.cargando = true;
			const carta = await obtener_carta();
			vm.categorias = carta.categorias;
			vm.productos = carta.productos;
			vm.cargando = false;
		}
	}
};</script>
