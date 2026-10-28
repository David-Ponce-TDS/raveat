<template>
	<comp-modal-detalle
		:abierto="abierto"
		:hero_color="pedido ? estado_pedido(pedido.estado).color : ''"
		:mostrar_actualizar="true"
		:titulo="pedido ? pedido.codigo : 'Pedido'"
		@actualizar="$emit('actualizar')"
		@cerrar="$emit('cerrar')"
	>
		<!-- El fondo tenido con el color del estado: el icono, el codigo, quien y cuanto. Lo que
		     se lee de un vistazo antes de bajar a los datos. -->
		<template #hero>
			<div v-if="pedido" class="raveat-hero">
				<div class="raveat-hero__icono">
					<ion-icon :icon="estado_pedido(pedido.estado).icono" aria-hidden="true" />
				</div>
				<h2 class="raveat-hero__titulo">{{ pedido.codigo }}</h2>
				<p class="raveat-hero__subtitulo">{{ pedido.cliente_nombre || 'Mostrador' }} · {{ tipo_pedido(pedido.tipo).texto }}</p>
				<p class="raveat-hero__importe">{{ formatear_importe(pedido.total) }}</p>
			</div>
		</template>

		<comp-esqueleto v-if="!pedido" :filas="4" />
		<template v-else>
			<!-- Las mismas pildoras que la fila de la lista: el estado, el pago y, si espera en
			     la cola, la marca de que la API todavia no lo sabe. -->
			<div class="raveat-detalle-badges">
				<span class="raveat-pill" :class="clase_tinte(estado_pedido(pedido.estado).color)">
					<ion-icon :icon="estado_pedido(pedido.estado).icono" aria-hidden="true" />
					{{ estado_pedido(pedido.estado).texto }}
				</span>
				<span class="raveat-pill" :class="clase_tinte(estado_pago(pedido.estado_pago).color)">
					<ion-icon :icon="estado_pago(pedido.estado_pago).icono" aria-hidden="true" />
					{{ estado_pago(pedido.estado_pago).texto }}
				</span>
				<span v-if="pedido.sin_enviar" class="raveat-pill raveat-tinte-warning">
					<ion-icon :icon="icono_sin_enviar" aria-hidden="true" />
					Sin enviar
				</span>
			</div>

			<comp-detalle-seccion titulo="Pedido" :subtitulo="tipo_pedido(pedido.tipo).texto">
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Cliente" :valor="pedido.cliente_nombre || 'Mostrador'" />
					<comp-detalle-dato label="Creado" :valor="formatear_fecha_larga(pedido.creado_en)" />
					<comp-detalle-dato v-if="pedido.observaciones" label="Observaciones" :valor="pedido.observaciones" />
				</div>
			</comp-detalle-seccion>

			<comp-detalle-seccion titulo="Importes">
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Subtotal" :valor="formatear_importe(pedido.subtotal)" />
					<comp-detalle-dato v-if="pedido.descuento" label="Descuento" :valor="'- ' + formatear_importe(pedido.descuento)" />
					<comp-detalle-dato v-if="pedido.propina_importe" label="Propina" :valor="formatear_importe(pedido.propina_importe)" />
					<comp-detalle-dato label="Total" :valor="formatear_importe(pedido.total)" />
					<comp-detalle-dato v-if="pedido.medio_pago" label="Medio de pago" :valor="medio_pago(pedido.medio_pago).texto" />
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

			<!-- El QR lleva la direccion publica del pedido, la misma que imprime el comprobante:
			     cualquier camara la abre, y escanearlo desde Pedidos lo encuentra. Llega como Blob y no como <img src> a
			     la API, porque el endpoint pide token y una imagen no lo manda.
			     Sin red no se puede pedir: la sección no se dibuja, en vez de quedar cargando. -->
			<comp-detalle-seccion v-if="!sin_conexion" titulo="QR" subtitulo="Con cualquier cámara abre el estado del pedido">
				<div class="raveat-detalle-qr">
					<img v-if="qr" :src="qr" :alt="`QR del pedido ${pedido.codigo}`" />
					<ion-spinner v-else name="crescent" />
				</div>
			</comp-detalle-seccion>

			<ion-note v-if="error" color="danger">{{ error }}</ion-note>

			<!-- Sin red queda una sola acción: el cambio de estado, que espera en la cola. Cobrar,
			     el comprobante y cancelar le piden a la API, y no se dibujan. -->
			<div class="raveat-detalle-acciones">
				<ion-button v-if="siguiente" expand="block" :disabled="guardando" @click="$emit('estado', siguiente)">
					Pasar a {{ estado_pedido(siguiente).texto }}
				</ion-button>
				<ion-button v-if="puede_cobrar && !sin_conexion" expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'efectivo')">
					Cobrar en efectivo
				</ion-button>
				<ion-button v-if="puede_cobrar && !sin_conexion" expand="block" fill="outline" :disabled="guardando" @click="$emit('cobrar', 'transferencia')">
					Cobrar por transferencia
				</ion-button>
				<ion-button v-if="!sin_conexion" expand="block" fill="outline" :disabled="guardando" @click="$emit('comprobante')">
					<ion-icon slot="start" :icon="icono_comprobante" />
					Comprobante
				</ion-button>
				<!-- Los datos como texto, para mandarlos por donde sea. No le pide nada a la API: con
				     lo que la ficha ya tiene alcanza, asi que tambien se ofrece sin red. -->
				<ion-button expand="block" fill="clear" @click="$emit('compartir')">
					<ion-icon slot="start" :icon="icono_compartir" />
					Compartir datos del pedido
				</ion-button>
				<ion-button v-if="puede_cancelar && !sin_conexion" expand="block" fill="clear" color="danger" :disabled="guardando" @click="$emit('cancelar')">
					Cancelar pedido
				</ion-button>
			</div>
		</template>
	</comp-modal-detalle>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonNote,
	IonSpinner
} from '@ionic/vue';
import { cloudUploadOutline, documentTextOutline, shareSocialOutline } from 'ionicons/icons';
import comp_detalle_dato from '@/components/base/comp_detalle_dato.vue';
import comp_detalle_seccion from '@/components/base/comp_detalle_seccion.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { clase_tinte, estado_pago, estado_pedido, medio_pago, siguiente_estado_pedido, tipo_pedido } from '@/utils/estados';
import { formatear_fecha_larga } from '@/utils/formato_fecha';
import { formatear_importe } from '@/utils/formato_moneda';

// La ficha del pedido como hoja: el resumen sobre el fondo del color del estado y, debajo, los
// datos, los items, el QR y las acciones, al lado de la informacion con la que se deciden.
// No habla con la API ni con el store: recibe el pedido y emite lo que hay que hacer. La pantalla
// orquesta, la ficha dibuja.
export default {
	name: 'comp_pedido_detalle_modal',
	components: {
		CompDetalleDato: comp_detalle_dato,
		CompDetalleSeccion: comp_detalle_seccion,
		CompEsqueleto: comp_esqueleto,
		CompModalDetalle: comp_modal_detalle,
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
		'compartir',
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
		},
		sin_conexion: {
			type: Boolean,
			default: false
		}
	},
	data(){
		return {
			icono_compartir: shareSocialOutline,
			icono_comprobante: documentTextOutline,
			icono_sin_enviar: cloudUploadOutline
		};
	},
	computed: {
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
			return siguiente_estado_pedido(vm.pedido.estado);
		}
	},
	methods: {
		clase_tinte,
		estado_pago,
		estado_pedido,
		formatear_fecha_larga,
		formatear_importe,
		medio_pago,
		tipo_pedido
	}
};</script>
