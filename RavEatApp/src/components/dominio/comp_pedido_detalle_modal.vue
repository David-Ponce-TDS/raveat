<template>
	<ion-modal :is-open="abierto" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ pedido ? pedido.codigo : 'Pedido' }}</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content>
			<div v-if="pedido" class="ion-padding raveat-page-stack">
				<ion-list class="raveat-card">
					<ion-item>
						<ion-label>
							<p class="raveat-item-meta">Cliente</p>
							<h3 class="raveat-item-title">{{ pedido.cliente_nombre || 'Mostrador' }}</h3>
						</ion-label>
						<ion-badge slot="end" :color="color">{{ etiqueta(pedido.estado) }}</ion-badge>
					</ion-item>
					<ion-item>
						<ion-label>
							<p class="raveat-item-meta">Tipo</p>
							<h3 class="raveat-item-title">{{ etiqueta(pedido.tipo) }}</h3>
						</ion-label>
					</ion-item>
					<ion-item :lines="pedido.observaciones ? 'full' : 'none'">
						<ion-label>
							<p class="raveat-item-meta">Pago</p>
							<h3 class="raveat-item-title">{{ etiqueta(pedido.estado_pago) }}{{ pedido.medio_pago ? ` · ${etiqueta(pedido.medio_pago)}` : '' }}</h3>
						</ion-label>
					</ion-item>
					<ion-item v-if="pedido.observaciones" lines="none">
						<ion-label class="ion-text-wrap">
							<p class="raveat-item-meta">Observaciones</p>
							<h3 class="raveat-item-title">{{ pedido.observaciones }}</h3>
						</ion-label>
					</ion-item>
				</ion-list>

				<h2 class="raveat-seccion-titulo">Productos</h2>
				<ion-list class="raveat-card">
					<ion-item v-for="item in items" :key="item.id">
						<ion-label>
							<h3 class="raveat-item-title">{{ item.cantidad }} × {{ item.producto_nombre }}</h3>
							<p v-if="item.observaciones" class="raveat-item-meta">{{ item.observaciones }}</p>
						</ion-label>
						<ion-note slot="end">{{ formatear_importe(item.subtotal) }}</ion-note>
					</ion-item>
				</ion-list>

				<div class="raveat-resumen">
					<span>Subtotal: {{ formatear_importe(pedido.subtotal) }}</span>
					<span v-if="pedido.descuento > 0">Descuento: -{{ formatear_importe(pedido.descuento) }}</span>
					<span v-if="pedido.propina_importe > 0">Propina: {{ formatear_importe(pedido.propina_importe) }}</span>
					<strong>Total: {{ formatear_importe(pedido.total + pedido.propina_importe) }}</strong>
				</div>

				<h2 class="raveat-seccion-titulo">Código QR</h2>
				<!-- El mismo QR que va impreso en el comprobante: escanearlo desde otro telefono abre
					este pedido. Lo genera la API; aca solo se muestra. -->
				<div class="raveat-qr">
					<img v-if="qr_url" :src="qr_url" :alt="`QR del pedido ${pedido.codigo}`" />
					<ion-spinner v-else name="dots" />
					<p class="raveat-item-meta">{{ pedido.codigo }}</p>
				</div>

				<ion-button v-if="siguiente" expand="block" :disabled="guardando" @click="$emit('cambiar_estado', siguiente)">
					Pasar a {{ etiqueta(siguiente) }}
				</ion-button>
				<div v-if="puede_cobrar" class="raveat-acciones">
					<ion-button expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'efectivo')">
						<ion-icon slot="start" :icon="icono_efectivo" />
						Efectivo
					</ion-button>
					<ion-button expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'transferencia')">
						<ion-icon slot="start" :icon="icono_transferencia" />
						Transferencia
					</ion-button>
				</div>
				<ion-button expand="block" fill="outline" :disabled="guardando" @click="$emit('comprobante')">
					<ion-icon slot="start" :icon="icono_compartir" />
					Comprobante
				</ion-button>
				<ion-button v-if="puede_cancelar" expand="block" fill="clear" color="danger" :disabled="guardando" @click="$emit('cancelar')">
					Cancelar pedido
				</ion-button>
			</div>
		</ion-content>
	</ion-modal>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonButtons,
	IonContent,
	IonHeader,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonModal,
	IonNote,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { cashOutline, shareOutline, swapHorizontalOutline } from 'ionicons/icons';
import { formatear_importe } from '@/utils/formato_moneda';

// El siguiente estado posible, para ofrecer una sola accion evidente. El servidor tiene la tabla
// completa de transiciones y es el que decide: esto es un atajo de la interfaz, no la regla.
const SIGUIENTE_ESTADO = {
	borrador: 'confirmado',
	confirmado: 'en_preparacion',
	en_preparacion: 'listo',
	listo: 'entregado',
	entregado: 'cerrado'
};

// El detalle de un pedido con todo lo que se puede hacer con el: cambiar de estado, cobrar,
// compartir el comprobante, cancelar. El modal no llama a la API: emite y la pagina resuelve
// con el store, que es el que sabe recargar la lista.
export default {
	name: 'comp_pedido_detalle_modal',
	components: {
		IonBadge,
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonModal,
		IonNote,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	emits: [
		'cambiar_estado',
		'cancelar',
		'cerrar',
		'cobrar',
		'comprobante'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		pedido: {
			type: Object,
			default: null
		},
		items: {
			type: Array,
			default: () => []
		},
		// La imagen del QR ya descargada, como URL de objeto. La pide la pagina junto con el detalle.
		qr_url: {
			type: String,
			default: ''
		},
		color: {
			type: String,
			default: 'medium'
		},
		guardando: {
			type: Boolean,
			default: false
		}
	},
	data(){
		return {
			icono_compartir: shareOutline,
			icono_efectivo: cashOutline,
			icono_transferencia: swapHorizontalOutline
		};
	},
	computed: {
		siguiente(){
			var vm = this;
			return vm.pedido ? SIGUIENTE_ESTADO[vm.pedido.estado] || null : null;
		},
		puede_cobrar(){
			var vm = this;
			return Boolean(vm.pedido) && vm.pedido.estado_pago == 'pendiente' && vm.pedido.estado != 'cancelado';
		},
		puede_cancelar(){
			var vm = this;
			return Boolean(vm.pedido) && vm.pedido.estado != 'cancelado' && vm.pedido.estado != 'cerrado';
		}
	},
	methods: {
		// Los valores viajan en snake_case porque asi los serializa la API.
		etiqueta: function(valor){
			if(!valor) return '';
			return valor.replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
		},
		formatear_importe
	}
};</script>
