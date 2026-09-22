<template>
	<comp-modal-detalle
		:abierto="abierto"
		:mostrar_actualizar="true"
		:titulo="pedido ? pedido.codigo : 'Pedido'"
		@actualizar="$emit('actualizar')"
		@cerrar="$emit('cerrar')"
	>
		<comp-esqueleto v-if="!pedido" :filas="4" />
		<template v-else>
			<comp-detalle-seccion titulo="Pedido" :subtitulo="etiqueta(pedido.tipo)">
				<div class="raveat-detalle-badges">
					<ion-badge :color="color_estado(pedido.estado)">{{ etiqueta(pedido.estado) }}</ion-badge>
					<ion-badge :color="pedido.estado_pago == 'pagado' ? 'success' : 'medium'">{{ etiqueta(pedido.estado_pago) }}</ion-badge>
				</div>
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Cliente" :valor="pedido.cliente_nombre || 'Mostrador'" />
					<comp-detalle-dato label="Creado" :valor="fecha_creado" />
					<comp-detalle-dato v-if="pedido.observaciones" label="Observaciones" :valor="pedido.observaciones" />
				</div>
			</comp-detalle-seccion>

			<comp-detalle-seccion titulo="Importes">
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Subtotal" :valor="formatear_importe(pedido.subtotal)" />
					<comp-detalle-dato v-if="pedido.descuento" label="Descuento" :valor="'- ' + formatear_importe(pedido.descuento)" />
					<comp-detalle-dato v-if="pedido.propina_importe" label="Propina" :valor="formatear_importe(pedido.propina_importe)" />
					<comp-detalle-dato label="Total" :valor="formatear_importe(pedido.total)" />
					<comp-detalle-dato v-if="pedido.medio_pago" label="Medio de pago" :valor="etiqueta(pedido.medio_pago)" />
				</div>
			</comp-detalle-seccion>

			<comp-detalle-seccion titulo="Ítems" :subtitulo="`${items.length} en el pedido`">
				<ul class="raveat-detalle-items">
					<li v-for="item in items" :key="item.id">
						<div>
							<strong>{{ item.cantidad }} × {{ item.producto_nombre }}</strong>
							<p v-if="item.observaciones">{{ item.observaciones }}</p>
							<p class="raveat-item-meta">{{ formatear_importe(item.precio_unitario) }} c/u</p>
						</div>
						<span>{{ formatear_importe(item.subtotal) }}</span>
					</li>
				</ul>
			</comp-detalle-seccion>

			<!-- El QR lleva el codigo del pedido, el mismo que imprime el comprobante: escanearlo
			     desde la pantalla de pedidos lo encuentra. Llega como Blob y no como <img src> a
			     la API, porque el endpoint pide token y una imagen no lo manda. -->
			<comp-detalle-seccion titulo="QR" subtitulo="Escanealo para encontrar este pedido">
				<div class="raveat-detalle-qr">
					<img v-if="qr" :src="qr" :alt="`QR del pedido ${pedido.codigo}`" />
					<ion-spinner v-else name="crescent" />
				</div>
			</comp-detalle-seccion>

			<ion-note v-if="error" color="danger">{{ error }}</ion-note>

			<div class="raveat-detalle-acciones">
				<ion-button v-if="siguiente" expand="block" :disabled="guardando" @click="$emit('estado', siguiente)">
					Pasar a {{ etiqueta(siguiente) }}
				</ion-button>
				<ion-button v-if="puede_cobrar" expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'efectivo')">
					Cobrar en efectivo
				</ion-button>
				<ion-button v-if="puede_cobrar" expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'transferencia')">
					Cobrar por transferencia
				</ion-button>
				<ion-button expand="block" fill="outline" :disabled="guardando" @click="$emit('comprobante')">
					<ion-icon slot="start" :icon="icono_comprobante" />
					Comprobante
				</ion-button>
				<ion-button v-if="puede_cancelar" expand="block" fill="clear" color="danger" :disabled="guardando" @click="$emit('cancelar')">
					Cancelar pedido
				</ion-button>
			</div>
		</template>
	</comp-modal-detalle>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonNote,
	IonSpinner
} from '@ionic/vue';
import { documentTextOutline } from 'ionicons/icons';
import comp_detalle_dato from '@/components/base/comp_detalle_dato.vue';
import comp_detalle_seccion from '@/components/base/comp_detalle_seccion.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { color_estado, etiqueta, siguiente_estado } from '@/utils/pedidos_presentacion';
import { formatear_importe } from '@/utils/formato_moneda';

// La ficha del pedido: lo que antes era una hoja de acciones sin datos. Muestra el pedido entero
// —estado, importes, items y su QR— y ofrece las mismas acciones, pero al lado de la informacion
// con la que se deciden.
// No habla con la API ni con el store: recibe el pedido y emite lo que hay que hacer. La pantalla
// orquesta, la ficha dibuja.
export default {
	name: 'comp_pedido_detalle_modal',
	components: {
		CompDetalleDato: comp_detalle_dato,
		CompDetalleSeccion: comp_detalle_seccion,
		CompEsqueleto: comp_esqueleto,
		CompModalDetalle: comp_modal_detalle,
		IonBadge,
		IonButton,
		IonIcon,
		IonNote,
		IonSpinner
	},
	emits: [
		'actualizar',
		'cancelar',
		'cerrar',
		'cobrar',
		'comprobante',
		'estado'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		},
		items: {
			type: Array,
			default: () => []
		},
		pedido: {
			type: Object,
			default: null
		},
		qr: {
			type: String,
			default: ''
		}
	},
	data(){
		return {
			icono_comprobante: documentTextOutline
		};
	},
	computed: {
		fecha_creado(){
			var vm = this;
			if(!vm.pedido || !vm.pedido.creado_en) return '';
			return new Date(vm.pedido.creado_en).toLocaleString('es-AR');
		},
		puede_cancelar(){
			var vm = this;
			if(!vm.pedido) return false;
			return vm.pedido.estado != 'cancelado' && vm.pedido.estado != 'cerrado';
		},
		puede_cobrar(){
			var vm = this;
			if(!vm.pedido) return false;
			return vm.pedido.estado_pago == 'pendiente' && vm.pedido.estado != 'cancelado';
		},
		siguiente(){
			var vm = this;
			if(!vm.pedido) return '';
			return siguiente_estado(vm.pedido.estado);
		}
	},
	methods: {
		color_estado,
		etiqueta,
		formatear_importe
	}
};</script>
