<template>
	<comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-esqueleto v-if="productos_store.cargando" />
			<comp-estado-error
				v-else-if="productos_store.error"
				:mensaje="productos_store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!productos_store.hay_productos"
				:icono="icono"
				titulo="Todavía no hay productos"
				mensaje="La API respondió, pero la carta está vacía."
			/>
			<template v-else>
				<div class="raveat-resumen">
					<span>{{ productos_store.resumen.total }} productos</span>
					<span>{{ productos_store.resumen.disponibles }} disponibles</span>
					<span>{{ productos_store.categorias.length }} categorías</span>
				</div>
				<ion-list class="raveat-panel">
					<comp-producto-lista-item
						v-for="producto in productos_store.productos"
						:key="producto.id"
						:producto="producto"
					/>
				</ion-list>
			</template>
		</div>
	</comp-page>
</template>

<script>import { IonList } from '@ionic/vue';
import { pricetagsOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_producto_lista_item from '@/components/dominio/comp_producto_lista_item.vue';
import { use_productos_store } from '@/stores/productos_store';

// Primera pantalla con datos reales. La pagina no sabe que existe la API: le pide al store y lee
// `cargando`, `error` y `productos`. Los tres estados se dibujan siempre, porque los tres pasan.
export default {
	name: 'productos_page',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		CompProductoListaItem: comp_producto_lista_item,
		IonList
	},
	data(){
		return {
			icono: pricetagsOutline,
			productos_store: use_productos_store()
		};
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
