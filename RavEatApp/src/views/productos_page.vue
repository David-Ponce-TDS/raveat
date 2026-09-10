<template>
	<comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre o descripción" @buscar="buscar" />

			<!-- Los contadores son botones: tocarlos filtra. Por eso el resumen que devuelve la API
			     NO aplica el filtro de disponibilidad — si se contara a sí mismo, al filtrar por
			     "disponibles" el número de no disponibles daría siempre 0. -->
			<div v-if="store.catalogo_resumen" class="raveat-chips">
				<button type="button" class="raveat-chip" :class="{ activo: disponible === null }" @click="filtrar_disponible(null)">
					Todos · {{ store.catalogo_resumen.total }}
				</button>
				<button type="button" class="raveat-chip" :class="{ activo: disponible === true }" @click="filtrar_disponible(true)">
					Disponibles · {{ store.catalogo_resumen.disponibles }}
				</button>
				<button type="button" class="raveat-chip" :class="{ activo: disponible === false }" @click="filtrar_disponible(false)">
					No disponibles · {{ store.catalogo_resumen.no_disponibles }}
				</button>
			</div>

			<ion-button expand="block" fill="outline" size="small" @click="abrir_nuevo">
				<ion-icon slot="start" :icon="icono_agregar" />
				Nuevo producto
			</ion-button>

			<comp-esqueleto v-if="store.catalogo_cargando && !store.catalogo_hay_productos" />
			<comp-estado-error
				v-else-if="store.error && !store.catalogo_hay_productos"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.catalogo_hay_productos"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay productos'"
				:mensaje="hay_filtros ? 'Probá con otro texto o quitá el filtro.' : 'Cargá el primero con el botón de arriba.'"
			/>
			<comp-lista
				v-else
				:cargando="store.catalogo_cargando"
				:hay_mas="store.catalogo_hay_mas"
				:mostrados="store.catalogo_productos.length"
				:total="total_catalogo"
				@cargar_mas="store.cargar_mas_catalogo()"
			>
				<ion-item-sliding v-for="producto in store.catalogo_productos" :key="producto.id">
					<ion-item button @click="abrir_edicion(producto)">
						<ion-label>
							<h3 class="raveat-item-title">{{ producto.nombre }}</h3>
							<p>{{ producto.categoria_nombre }}</p>
							<p v-if="producto.descripcion" class="raveat-item-meta">{{ producto.descripcion }}</p>
						</ion-label>
						<ion-badge slot="end" :color="producto.disponible ? 'success' : 'medium'">
							{{ formatear_importe(producto.precio) }}
						</ion-badge>
					</ion-item>
					<ion-item-options side="end">
						<ion-item-option color="danger" @click="confirmar_baja(producto)">Baja</ion-item-option>
					</ion-item-options>
				</ion-item-sliding>
			</comp-lista>
		</div>

		<comp-producto-formulario-modal
			:abierto="modal_abierto"
			:categorias="store.categorias"
			:error="store.error || ''"
			:guardando="store.guardando"
			:producto="producto_editando"
			@cerrar="cerrar_modal"
			@guardar="guardar"
		/>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonItem,
	IonItemOption,
	IonItemOptions,
	IonItemSliding,
	IonLabel,
	alertController
} from '@ionic/vue';
import { addOutline, pricetagsOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_producto_formulario_modal from '@/components/dominio/comp_producto_formulario_modal.vue';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_productos_store } from '@/stores/productos_store';

// El catalogo de gestion: paginado, filtrable y con ABM. La vitrina de Inicio muestra los mismos
// productos con otra cara y sin paginar; las dos leen del mismo store, en listas separadas.
export default {
	name: 'productos_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompProductoFormularioModal: comp_producto_formulario_modal,
		IonBadge,
		IonButton,
		IonIcon,
		IonItem,
		IonItemOption,
		IonItemOptions,
		IonItemSliding,
		IonLabel
	},
	data(){
		return {
			busqueda: '',
			disponible: null,
			icono: pricetagsOutline,
			icono_agregar: addOutline,
			modal_abierto: false,
			producto_editando: null,
			store: use_productos_store()
		};
	},
	computed: {
		hay_filtros(){
			var vm = this;
			return Boolean(vm.busqueda) || vm.disponible !== null;
		},
		total_catalogo(){
			var vm = this;
			return vm.store.catalogo_pagina ? vm.store.catalogo_pagina.total : 0;
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
		// La vitrina se pide tambien porque de ahi salen las categorias que usa el formulario.
		vm.store.cargar_productos();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar_catalogo();
		},
		// La busqueda sale a la API. Filtrar aca solo miraria la pagina descargada y diria
		// "sin resultados" teniendo resultados en las paginas siguientes.
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar_catalogo({busqueda: texto});
		},
		filtrar_disponible: function(valor){
			var vm = this;
			vm.disponible = valor;
			return vm.store.cargar_catalogo({disponible: valor});
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.producto_editando = null;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		abrir_edicion: function(producto){
			var vm = this;
			vm.producto_editando = producto;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		cerrar_modal: function(){
			var vm = this;
			vm.modal_abierto = false;
			vm.producto_editando = null;
		},
		guardar: async function(evento){
			var vm = this;
			const guardado = await vm.store.guardar_producto(evento.id, evento.datos);
			// El modal solo se cierra si el servidor acepto: si no, queda abierto mostrando el
			// error y sin perder lo que el usuario habia escrito.
			if(guardado) vm.cerrar_modal();
		},
		confirmar_baja: async function(producto){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja "${producto.nombre}"? Los pedidos que ya lo incluyen no se modifican.`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: () => vm.store.eliminar_producto_catalogo(producto.id)}
				]
			});
			await alerta.present();
		},
		formatear_importe
	}
};</script>
