<template>
	<!-- La ficha de la comanda: sobre el fondo del color del estado va el pedido y cuánto salió;
	     debajo, ítem por ítem, con el botón del paso que sigue. -->
	<comp-modal-detalle
		:abierto="abierto"
		:hero_color="comanda ? estado_pedido(comanda.estado).color : ''"
		:mostrar_actualizar="true"
		:titulo="comanda ? comanda.codigo : 'Preparación'"
		@actualizar="$emit('actualizar')"
		@cerrar="$emit('cerrar')"
	>
		<template #hero>
			<div v-if="comanda" class="raveat-hero">
				<div class="raveat-hero__icono">
					<ion-icon :icon="estado_pedido(comanda.estado).icono" aria-hidden="true" />
				</div>
				<h2 class="raveat-hero__titulo">{{ comanda.codigo }}</h2>
				<p class="raveat-hero__subtitulo">{{ comanda.cliente_nombre || 'Mostrador' }} · {{ tipo_pedido(comanda.tipo).texto }}</p>
				<p class="raveat-hero__importe">{{ comanda.items_listos }} / {{ comanda.total_items }} listos</p>
			</div>
		</template>

		<template v-if="comanda">
			<div class="raveat-kpis">
				<div class="raveat-kpi">
					<span>Estado</span>
					<strong>{{ estado_pedido(comanda.estado).texto }}</strong>
				</div>
				<div class="raveat-kpi">
					<span>Esperando</span>
					<strong>{{ formatear_espera(comanda.desde_en) }}</strong>
				</div>
			</div>

			<!-- Las observaciones del pedido son media comanda: "sin sal", "sin tacc". -->
			<comp-aviso v-if="comanda.observaciones" tono="pendiente" :mensaje="comanda.observaciones" />

			<!-- Ítem por ítem: es lo que una cocina realmente maneja. El estado del pedido lo
			     recalcula el servidor con esto — acá no se toca. -->
			<comp-detalle-seccion titulo="Preparación" subtitulo="Marcá cada ítem cuando lo empezás y cuando sale.">
				<ul class="raveat-detalle-items">
					<li v-for="item in items" :key="item.id">
						<div>
							<strong>{{ item.cantidad }} × {{ item.producto_nombre }}</strong>
							<p v-if="item.observaciones">{{ item.observaciones }}</p>
						</div>
						<div class="raveat-comanda-item__acciones">
							<span class="raveat-pill" :class="clase_tinte(estado_item(item.estado).color)">
								<ion-icon :icon="estado_item(item.estado).icono" aria-hidden="true" />
								{{ estado_item(item.estado).texto }}
							</span>
							<!-- Un solo botón redondo con el gesto que sigue: play para empezar, tilde
							     para sacarlo. La píldora de al lado ya dice el estado. -->
							<ion-button
								v-if="SIGUIENTE[item.estado]"
								class="raveat-comanda-item__boton"
								:color="SIGUIENTE[item.estado].color"
								:fill="SIGUIENTE[item.estado].fill"
								:disabled="guardando"
								:aria-label="SIGUIENTE[item.estado].etiqueta"
								@click="$emit('mover_item', {item, estado: SIGUIENTE[item.estado].estado})"
							>
								<ion-icon slot="icon-only" :icon="SIGUIENTE[item.estado].icono" />
							</ion-button>
						</div>
					</li>
				</ul>
				<ion-note v-if="error" color="danger">{{ error }}</ion-note>
			</comp-detalle-seccion>

			<!-- El atajo del mostrador: casi siempre el pedido sale entero. -->
			<comp-detalle-seccion titulo="Salida" subtitulo="Al quedar todo listo, la app avisa a quien tomó el pedido.">
				<div class="raveat-detalle-acciones">
					<ion-button expand="block" :disabled="guardando || todo_listo" @click="$emit('marcar_lista', comanda)">
						<ion-icon slot="start" :icon="icono_listo" />
						{{ todo_listo ? 'Todo listo' : 'Marcar todo listo' }}
					</ion-button>
				</div>
			</comp-detalle-seccion>
		</template>
	</comp-modal-detalle>
</template>

<script>import { IonButton, IonIcon, IonNote } from '@ionic/vue';
import { checkmarkDoneOutline, checkmarkOutline, playOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_detalle_seccion from '@/components/base/comp_detalle_seccion.vue';
import comp_modal_detalle from '@/components/base/comp_modal_detalle.vue';
import { clase_tinte, estado_item, estado_pedido, tipo_pedido } from '@/utils/estados';
import { formatear_espera } from '@/utils/formato_fecha';

// El camino de un ítem en cocina: pendiente -> en preparación -> listo. Un solo paso por vez, y
// nunca para atrás: si salió, salió. El servidor lo vuelve a validar igual.
// El tilde va en outline a propósito: sólido y verde se leía como "ya salió" cuando en realidad
// es la acción pendiente. El contorno dice "tocá para completar"; el lleno queda para lo hecho.
const SIGUIENTE = {
	pendiente: {estado: 'en_preparacion', etiqueta: 'Empezar', icono: playOutline, color: 'primary', fill: 'solid'},
	en_preparacion: {estado: 'listo', etiqueta: 'Salió', icono: checkmarkOutline, color: 'success', fill: 'outline'}
};

export default {
	name: 'comp_comanda_detalle_modal',
	components: {
		CompAviso: comp_aviso,
		CompDetalleSeccion: comp_detalle_seccion,
		CompModalDetalle: comp_modal_detalle,
		IonButton,
		IonIcon,
		IonNote
	},
	emits: [
		'actualizar',
		'cerrar',
		'marcar_lista',
		'mover_item'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		comanda: {
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
		items: {
			type: Array,
			default: function(){
				return [];
			}
		}
	},
	data(){
		return {
			icono_listo: checkmarkDoneOutline,
			SIGUIENTE
		};
	},
	computed: {
		todo_listo(){
			var vm = this;
			return vm.items.length > 0 && vm.items.every(item => item.estado == 'listo');
		}
	},
	methods: {
		clase_tinte,
		estado_item,
		estado_pedido,
		formatear_espera,
		tipo_pedido
	}
};</script>
