<template>
	<!-- La ficha de la entrega reemplaza a la hoja de acciones sin datos: sobre el fondo del color
	     del estado va a donde se lleva y cuanto, y debajo el recorrido, el contacto y los datos,
	     al lado de la informacion con la que se decide cada accion. -->
	<comp-modal-detalle
		:abierto="abierto"
		:hero_color="entrega ? estado_entrega(entrega.estado).color : ''"
		:titulo="entrega ? entrega.pedido_codigo : 'Entrega'"
		@cerrar="$emit('cerrar')"
	>
		<template #hero>
			<div v-if="entrega" class="raveat-hero">
				<div class="raveat-hero__icono">
					<ion-icon :icon="estado_entrega(entrega.estado).icono" aria-hidden="true" />
				</div>
				<h2 class="raveat-hero__titulo">{{ entrega.direccion_linea }}</h2>
				<p class="raveat-hero__subtitulo">{{ entrega.pedido_codigo }} · {{ estado_entrega(entrega.estado).texto }}</p>
				<p class="raveat-hero__importe">{{ formatear_importe(entrega.pedido_total) }}</p>
			</div>
		</template>

		<template v-if="entrega">
			<!-- El camino de vida del reparto, de arriba hacia abajo. Una cancelada corta el
			     recorrido y su paso negativo reemplaza al final. Solo el primer paso lleva fecha:
			     es la unica que la entrega trae (cuando se asigno o se entrego no viaja). -->
			<comp-detalle-seccion titulo="Recorrido" subtitulo="Estado operativo de la entrega.">
				<ol class="raveat-entrega-timeline">
					<li
						v-for="paso in pasos_timeline"
						:key="paso.id"
						class="raveat-entrega-timeline__paso"
						:class="{ completado: paso.completado, actual: paso.actual, negativo: paso.negativo }"
					>
						<span class="raveat-entrega-timeline__punto" aria-hidden="true"></span>
						<div class="raveat-entrega-timeline__texto">
							<strong>{{ paso.texto }}</strong>
							<span v-if="paso.fecha">{{ formatear_fecha_larga(paso.fecha) }}</span>
						</div>
					</li>
				</ol>

				<!-- Que le pasa al PEDIDO segun como va la entrega, contado antes de decidir. La
				     pildora va debajo de la frase: compartiendo renglon, en un telefono angosto se
				     comia las ultimas letras. -->
				<div class="raveat-entrega-final">
					<span>{{ estado_pedido_entrega }}</span>
					<span class="raveat-pill" :class="clase_tinte(estado_entrega(entrega.estado).color)">
						<ion-icon :icon="estado_entrega(entrega.estado).icono" aria-hidden="true" />
						{{ estado_entrega(entrega.estado).texto }}
					</span>
				</div>

				<ion-note v-if="error" color="danger">{{ error }}</ion-note>

				<!-- El paso siguiente lo dice utils/estados.js; tomarla es de delivery y admin. El
				     servidor valida el rol: esto solo evita ofrecer lo que va a ser rechazado. -->
				<div v-if="puede_tomar || siguiente" class="raveat-detalle-acciones">
					<ion-button v-if="puede_tomar" expand="block" :disabled="guardando" @click="$emit('tomar')">
						Tomar esta entrega
					</ion-button>
					<ion-button v-if="siguiente" expand="block" :disabled="guardando" @click="$emit('estado', siguiente)">
						Marcar {{ estado_entrega(siguiente).texto.toLowerCase() }}
					</ion-button>
				</div>
			</comp-detalle-seccion>

			<!-- Designar a otro es del admin: el listado de usuarios es solo suyo. Un delivery no
			     designa a nadie, la toma para si con el boton de arriba. -->
			<comp-detalle-seccion
				v-if="puede_asignar"
				titulo="Asignación"
				subtitulo="La entrega necesita un repartidor antes de salir."
			>
				<ion-item lines="none" class="raveat-panel">
					<ion-select
						label="Repartidor"
						label-placement="stacked"
						interface="popover"
						placeholder="Seleccionar"
						:value="null"
						:disabled="guardando || repartidores.length == 0"
						@ion-change="elegir_repartidor"
					>
						<ion-select-option v-for="usuario in repartidores" :key="usuario.id" :value="usuario.id">
							{{ usuario.nombre }}
						</ion-select-option>
					</ion-select>
				</ion-item>
				<ion-note v-if="repartidores.length == 0" color="warning">No hay usuarios con rol delivery para asignar.</ion-note>
			</comp-detalle-seccion>

			<!-- El mapa es externo: la app de mapas del telefono, con las coordenadas que la entrega
			     copio del cliente o, sin GPS, buscando la direccion escrita. Ruta abre el navegador
			     ya calculando como llegar. -->
			<comp-detalle-seccion titulo="Contacto y mapa" :subtitulo="entrega.cliente_nombre || 'Sin cliente'">
				<div class="raveat-acciones raveat-acciones--envuelve">
					<ion-button fill="outline" size="small" @click="abrir(url_mapa(entrega.direccion_latitud, entrega.direccion_longitud, entrega.direccion_linea))">
						<ion-icon slot="start" :icon="icono_mapa" />
						Mapa
					</ion-button>
					<ion-button fill="outline" size="small" @click="abrir(url_ruta(entrega.direccion_latitud, entrega.direccion_longitud, entrega.direccion_linea))">
						<ion-icon slot="start" :icon="icono_ruta" />
						Ruta
					</ion-button>
					<comp-contacto-botones :telefono="entrega.cliente_telefono" />
				</div>
			</comp-detalle-seccion>

			<comp-detalle-seccion titulo="Datos de la entrega" subtitulo="Copiados del cliente al crear el pedido.">
				<div class="raveat-kpis">
					<div class="raveat-kpi">
						<span>Pedido</span>
						<strong>{{ formatear_importe(entrega.pedido_total) }}</strong>
					</div>
					<div class="raveat-kpi">
						<span>Ubicación</span>
						<strong>{{ tiene_coordenadas ? 'Con GPS' : 'Por dirección' }}</strong>
					</div>
				</div>
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Dirección" :valor="entrega.direccion_linea" />
					<comp-detalle-dato v-if="entrega.direccion_referencia" label="Referencia" :valor="entrega.direccion_referencia" />
					<comp-detalle-dato label="Cliente" :valor="entrega.cliente_nombre || 'Sin cliente'" />
					<comp-detalle-dato label="Teléfono" :valor="entrega.cliente_telefono || 'Sin teléfono'" />
					<comp-detalle-dato label="Lleva" :valor="entrega.repartidor_nombre || 'Sin repartidor'" />
					<comp-detalle-dato v-if="tiene_coordenadas" label="Coordenadas" :valor="formatear_coordenadas(entrega.direccion_latitud, entrega.direccion_longitud)" />
				</div>
			</comp-detalle-seccion>

			<div v-if="puede_cancelar" class="raveat-detalle-acciones">
				<ion-button expand="block" fill="clear" color="danger" :disabled="guardando" @click="$emit('cancelar')">
					Cancelar entrega
				</ion-button>
			</div>
		</template>
	</comp-modal-detalle>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonItem,
	IonNote,
	IonSelect,
	IonSelectOption
} from '@ionic/vue';
import { mapOutline, navigateOutline } from 'ionicons/icons';
import comp_contacto_botones from '@/components/base/comp_contacto_botones.vue';
import comp_detalle_dato from '@/components/base/comp_detalle_dato.vue';
import comp_detalle_seccion from '@/components/base/comp_detalle_seccion.vue';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { formatear_coordenadas, url_mapa, url_ruta } from '@/services/geolocalizacion_service';
import { clase_tinte, estado_entrega, siguiente_estado_entrega } from '@/utils/estados';
import { formatear_fecha_larga } from '@/utils/formato_fecha';
import { formatear_importe } from '@/utils/formato_moneda';

// Que le pasa al pedido segun el estado de su entrega, en castellano y antes de decidir.
const ESTADO_PEDIDO_ENTREGA = {
	pendiente: 'El pedido sigue pendiente de entrega.',
	asignada: 'El pedido está asignado a reparto.',
	en_camino: 'El pedido está en camino y aún no está entregado.',
	entregada: 'El pedido queda Entregado.',
	cancelada: 'La entrega quedó cancelada. El pedido no se cancela solo.'
};

export default {
	name: 'comp_entrega_detalle_modal',
	components: {
		CompContactoBotones: comp_contacto_botones,
		CompDetalleDato: comp_detalle_dato,
		CompDetalleSeccion: comp_detalle_seccion,
		CompModalDetalle: comp_modal_detalle,
		IonButton,
		IonIcon,
		IonItem,
		IonNote,
		IonSelect,
		IonSelectOption
	},
	emits: [
		'asignar',
		'cancelar',
		'cerrar',
		'estado',
		'tomar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		entrega: {
			type: Object,
			default: null
		},
		error: {
			type: String,
			default: ''
		},
		guardando: {
			type: Boolean,
			default: false
		},
		// Lo deciden la pantalla, que conoce el rol: la ficha no sabe quien la mira.
		puede_asignar: {
			type: Boolean,
			default: false
		},
		puede_tomar: {
			type: Boolean,
			default: false
		},
		repartidores: {
			type: Array,
			default: function(){
				return [];
			}
		}
	},
	data(){
		return {
			icono_mapa: mapOutline,
			icono_ruta: navigateOutline
		};
	},
	computed: {
		siguiente(){
			var vm = this;
			if(!vm.entrega) return '';
			return siguiente_estado_entrega(vm.entrega.estado);
		},
		puede_cancelar(){
			var vm = this;
			if(!vm.entrega) return false;
			return vm.entrega.estado != 'entregada' && vm.entrega.estado != 'cancelada';
		},
		tiene_coordenadas(){
			var vm = this;
			return vm.entrega.direccion_latitud != null && vm.entrega.direccion_longitud != null;
		},
		estado_pedido_entrega(){
			var vm = this;
			return ESTADO_PEDIDO_ENTREGA[vm.entrega.estado] || 'El pedido sigue en reparto.';
		},
		// Los hitos en orden. El actual se marca; los anteriores, completados. Entregada y
		// cancelada son finales: su paso tambien queda completado.
		pasos_timeline(){
			var vm = this;
			if(!vm.entrega) return [];
			const estado = vm.entrega.estado;
			const cancelada = estado == 'cancelada';
			const orden = ['pendiente', 'asignada', 'en_camino', 'entregada'];
			const indice_actual = cancelada ? 3 : Math.max(orden.indexOf(estado), 0);
			const pasos = [
				{id: 'pendiente', texto: 'Pendiente', fecha: vm.entrega.creado_en},
				{id: 'asignada', texto: 'Asignada', fecha: null},
				{id: 'en_camino', texto: 'En camino', fecha: null},
				cancelada
					? {id: 'cancelada', texto: 'Cancelada', fecha: null, negativo: true}
					: {id: 'entregada', texto: 'Entregada', fecha: null}
			];
			return pasos.map((paso, indice) => ({
				...paso,
				completado: indice < indice_actual || (indice == indice_actual && (estado == 'entregada' || cancelada)),
				actual: indice == indice_actual,
				negativo: Boolean(paso.negativo)
			}));
		}
	},
	methods: {
		elegir_repartidor: function(evento){
			var vm = this;
			if(!evento.detail.value) return;
			vm.$emit('asignar', evento.detail.value);
		},
		abrir: function(url){
			window.open(url, '_blank');
		},
		clase_tinte,
		estado_entrega,
		formatear_coordenadas,
		formatear_fecha_larga,
		formatear_importe,
		url_mapa,
		url_ruta
	}
};</script>
