<template>
	<!-- La ficha del cliente: quien es, como contactarlo y que pidio antes. Llamar o escribirle
	     esta aca porque el momento de usarlo es mirando lo que pidio. -->
	<comp-modal-detalle
		:abierto="abierto"
		hero_color="primary"
		:titulo="cliente ? cliente.nombre : 'Cliente'"
		@cerrar="$emit('cerrar')"
	>
		<template #hero>
			<div v-if="cliente" class="raveat-hero">
				<div class="raveat-hero__icono">{{ inicial }}</div>
				<h2 class="raveat-hero__titulo">{{ cliente.nombre }}</h2>
				<p class="raveat-hero__subtitulo">{{ cliente.telefono }}<template v-if="cliente.email"> · {{ cliente.email }}</template></p>
				<p class="raveat-hero__subtitulo">{{ cliente.direccion_linea || 'Sin dirección: solo retiro' }}</p>
			</div>
		</template>

		<template v-if="cliente">
			<comp-detalle-seccion titulo="Contacto" subtitulo="Llamar o escribirle sin copiar el número.">
				<div class="raveat-acciones">
					<comp-contacto-botones :telefono="cliente.telefono" />
				</div>
			</comp-detalle-seccion>

			<comp-detalle-seccion titulo="Datos del cliente" subtitulo="Lo que usan los pedidos y las entregas.">
				<div class="raveat-detalle-datos">
					<comp-detalle-dato label="Nombre" :valor="cliente.nombre" />
					<comp-detalle-dato label="Teléfono" :valor="cliente.telefono || 'Sin teléfono'" />
					<comp-detalle-dato label="Email" :valor="cliente.email || 'Sin email'" />
					<comp-detalle-dato label="Dirección" :valor="cliente.direccion_linea || 'Sin dirección'" />
					<comp-detalle-dato v-if="cliente.direccion_referencia" label="Referencia" :valor="cliente.direccion_referencia" />
					<comp-detalle-dato label="Ubicación" :valor="tiene_coordenadas ? 'Marcada' : 'Sin marcar'" />
				</div>
				<ion-button expand="block" fill="outline" @click="$emit('editar', cliente)">
					<ion-icon slot="start" :icon="icono_editar" />
					Editar cliente
				</ion-button>
			</comp-detalle-seccion>

			<!-- Los pedidos los filtra la API (?cliente_id=): filtrar aca solo miraria una pagina de
			     pedidos de todos los clientes. Son los ultimos; tocar uno abre su ficha en Pedidos. -->
			<comp-detalle-seccion titulo="Pedidos anteriores" :subtitulo="subtitulo_pedidos">
				<comp-esqueleto v-if="pedidos_cargando" :filas="2" />
				<ul v-else-if="pedidos.length > 0" class="raveat-detalle-items">
					<li v-for="pedido in pedidos" :key="pedido.id" class="raveat-detalle-items__boton" @click="$emit('ver_pedido', pedido)">
						<div>
							<strong>{{ pedido.codigo }}</strong>
							<p class="raveat-item-meta">{{ formatear_hora_corta(pedido.creado_en) }}</p>
						</div>
						<div class="raveat-fila__derecha">
							<strong>{{ formatear_importe(pedido.total) }}</strong>
							<span class="raveat-pill" :class="clase_tinte(estado_pedido(pedido.estado).color)">{{ estado_pedido(pedido.estado).texto }}</span>
						</div>
					</li>
				</ul>
				<p v-else class="raveat-item-meta">Sin pedidos anteriores.</p>
			</comp-detalle-seccion>
		</template>
	</comp-modal-detalle>
</template>

<script>import {
	IonButton,
	IonIcon
} from '@ionic/vue';
import { createOutline } from 'ionicons/icons';
import comp_contacto_botones from '@/components/base/comp_contacto_botones.vue';
import comp_detalle_dato from '@/components/base/comp_detalle_dato.vue';
import comp_detalle_seccion from '@/components/base/comp_detalle_seccion.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { clase_tinte, estado_pedido } from '@/utils/estados';
import { formatear_hora_corta } from '@/utils/formato_fecha';
import { formatear_importe } from '@/utils/formato_moneda';

// Igual que las otras fichas: no habla con la API. La pantalla le pasa el cliente y sus pedidos
// y ejecuta lo que la ficha emite.
export default {
	name: 'comp_cliente_detalle_modal',
	components: {
		CompContactoBotones: comp_contacto_botones,
		CompDetalleDato: comp_detalle_dato,
		CompDetalleSeccion: comp_detalle_seccion,
		CompEsqueleto: comp_esqueleto,
		CompModalDetalle: comp_modal_detalle,
		IonButton,
		IonIcon
	},
	emits: [
		'cerrar',
		'editar',
		'ver_pedido'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		cliente: {
			type: Object,
			default: null
		},
		pedidos: {
			type: Array,
			default: () => []
		},
		pedidos_cargando: {
			type: Boolean,
			default: false
		},
		// Cuantos tiene en total segun la API; la lista trae solo los ultimos.
		pedidos_total: {
			type: Number,
			default: 0
		}
	},
	data(){
		return {
			icono_editar: createOutline
		};
	},
	computed: {
		inicial(){
			var vm = this;
			return (vm.cliente.nombre || '?').trim().charAt(0).toUpperCase();
		},
		tiene_coordenadas(){
			var vm = this;
			return vm.cliente.direccion_latitud != null && vm.cliente.direccion_longitud != null;
		},
		subtitulo_pedidos(){
			var vm = this;
			if(vm.pedidos_cargando) return 'Buscando sus pedidos…';
			if(vm.pedidos_total == 1) return '1 pedido asociado.';
			if(vm.pedidos_total > vm.pedidos.length) return `${vm.pedidos_total} pedidos asociados; estos son los últimos ${vm.pedidos.length}.`;
			return `${vm.pedidos_total} pedidos asociados.`;
		}
	},
	methods: {
		clase_tinte,
		estado_pedido,
		formatear_hora_corta,
		formatear_importe
	}
};</script>
