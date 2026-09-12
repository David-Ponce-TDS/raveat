<template>
	<comp-page titulo="Pedidos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por código" @buscar="buscar" />

			<!-- Los contadores por estado los calcula la API sobre el total filtrado, no sobre la
			     página: son los botones con los que se elige el filtro, así que no pueden depender
			     de él. Se pueden combinar: sin ninguno elegido, la API devuelve todos. -->
			<div class="raveat-chips">
				<button
					v-for="estado in estados_disponibles"
					:key="estado"
					type="button"
					class="raveat-chip"
					:class="{ activo: estados_activos.includes(estado) }"
					@click="store.alternar_estado(estado)"
				>
					{{ etiqueta(estado) }} · {{ store.resumen[estado] || 0 }}
				</button>
			</div>

			<ion-button expand="block" fill="outline" size="small" @click="abrir_nuevo">
				<ion-icon slot="start" :icon="icono_agregar" />
				Nuevo pedido
			</ion-button>

			<comp-esqueleto v-if="store.cargando && !store.hay_pedidos" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_pedidos"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_pedidos"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay pedidos'"
				:mensaje="hay_filtros ? 'Probá con otro texto o quitá los filtros.' : 'Cargá el primero con el botón de arriba.'"
			/>
			<comp-lista
				v-else
				:cargando="store.cargando"
				:hay_mas="store.hay_mas"
				:mostrados="store.pedidos.length"
				:total="store.total"
				@cargar_mas="store.cargar_mas()"
			>
				<ion-item v-for="pedido in store.pedidos" :key="pedido.id" button @click="abrir_acciones(pedido)">
					<ion-label>
						<h3 class="raveat-item-title">{{ pedido.codigo }}</h3>
						<p>{{ pedido.cliente_nombre || 'Mostrador' }} · {{ etiqueta(pedido.tipo) }}</p>
						<p class="raveat-item-meta">{{ formatear_importe(pedido.total) }}</p>
					</ion-label>
					<div slot="end" class="raveat-pedido-estados">
						<ion-badge :color="color_estado(pedido.estado)">{{ etiqueta(pedido.estado) }}</ion-badge>
						<ion-badge :color="pedido.estado_pago === 'pagado' ? 'success' : 'medium'">{{ etiqueta(pedido.estado_pago) }}</ion-badge>
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<comp-pedido-formulario-modal
			:abierto="modal_abierto"
			:clientes="clientes_store.clientes"
			:error="store.error || ''"
			:guardando="store.guardando"
			:productos="productos_store.productos"
			@cerrar="modal_abierto = false"
			@crear="crear"
		/>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonItem,
	IonLabel,
	actionSheetController
} from '@ionic/vue';
import { addOutline, receiptOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_pedido_formulario_modal from '@/components/dominio/comp_pedido_formulario_modal.vue';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_clientes_store } from '@/stores/clientes_store';
import { use_pedidos_store } from '@/stores/pedidos_store';
import { use_productos_store } from '@/stores/productos_store';

// Cada estado con su color. Vive en la pantalla y no en el store porque es decisión de
// presentación: el store guarda el estado, no cómo se pinta.
const COLOR_ESTADO = {
	borrador: 'medium',
	confirmado: 'primary',
	en_preparacion: 'warning',
	listo: 'success',
	entregado: 'success',
	cerrado: 'medium',
	cancelado: 'danger'
};

// El siguiente estado posible, para ofrecer una sola acción evidente. El servidor tiene la tabla
// completa de transiciones y es el que decide: esto es un atajo de la interfaz, no la regla.
const SIGUIENTE_ESTADO = {
	borrador: 'confirmado',
	confirmado: 'en_preparacion',
	en_preparacion: 'listo',
	listo: 'entregado',
	entregado: 'cerrado'
};

export default {
	name: 'pedidos_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompPedidoFormularioModal: comp_pedido_formulario_modal,
		IonBadge,
		IonButton,
		IonIcon,
		IonItem,
		IonLabel
	},
	data(){
		return {
			busqueda: '',
			clientes_store: use_clientes_store(),
			estados_disponibles: [
				'borrador',
				'confirmado',
				'en_preparacion',
				'listo',
				'entregado',
				'cerrado',
				'cancelado'
			],
			icono: receiptOutline,
			icono_agregar: addOutline,
			modal_abierto: false,
			productos_store: use_productos_store(),
			store: use_pedidos_store()
		};
	},
	computed: {
		estados_activos(){
			var vm = this;
			return vm.store.filtros.estados || [];
		},
		hay_filtros(){
			var vm = this;
			return Boolean(vm.busqueda) || vm.estados_activos.length > 0;
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
		// Clientes y productos hacen falta para el formulario de alta. Los dos stores ya son
		// compartidos, asi que si otra pantalla los cargo, esto no vuelve a pedirlos de cero.
		vm.clientes_store.cargar({busqueda: ''});
		vm.productos_store.cargar_productos();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar({busqueda: texto});
		},
		color_estado: function(estado){
			return COLOR_ESTADO[estado] || 'medium';
		},
		// Los valores viajan en snake_case porque asi los serializa la API. Pasarlos a texto legible
		// en un solo lugar evita que cada pantalla invente el suyo.
		etiqueta: function(valor){
			if(!valor) return '';
			return valor.replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		crear: async function(datos){
			var vm = this;
			const creado = await vm.store.crear(datos);
			if(creado) vm.modal_abierto = false;
		},
		abrir_acciones: async function(pedido){
			var vm = this;
			const botones = [];
			const siguiente = SIGUIENTE_ESTADO[pedido.estado];
			if(siguiente) botones.push({text: `Pasar a ${vm.etiqueta(siguiente)}`, handler: () => vm.store.cambiar_estado(pedido.id, siguiente)});
			if(pedido.estado_pago === 'pendiente' && pedido.estado !== 'cancelado') {
				botones.push({text: 'Cobrar en efectivo', handler: () => vm.store.registrar_pago(pedido.id, 'efectivo', 0)});
				botones.push({text: 'Cobrar por transferencia', handler: () => vm.store.registrar_pago(pedido.id, 'transferencia', 0)});
			}
			if(pedido.estado !== 'cancelado' && pedido.estado !== 'cerrado') {
				botones.push({text: 'Cancelar pedido', role: 'destructive', handler: () => vm.store.cancelar(pedido.id)});
			}
			botones.push({text: 'Cerrar', role: 'cancel'});

			const hoja = await actionSheetController.create({
				header: `${pedido.codigo} · ${vm.formatear_importe(pedido.total)}`,
				buttons: botones
			});
			await hoja.present();
		},
		formatear_importe
	}
};</script>
