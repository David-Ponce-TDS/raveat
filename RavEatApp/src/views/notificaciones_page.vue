<template>
	<comp-page titulo="Notificaciones" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<ion-button
				v-if="store.no_leidas > 0"
				expand="block"
				fill="outline"
				size="small"
				:disabled="store.cargando"
				@click="store.marcar_leida()"
			>
				<ion-icon slot="start" :icon="icono_leidas" />
				Marcar todas como leídas ({{ store.no_leidas }})
			</ion-button>

			<comp-esqueleto v-if="store.cargando && !store.hay_notificaciones" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_notificaciones"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_notificaciones"
				:icono="icono"
				titulo="Sin notificaciones"
				mensaje="Cuando pase algo que te interese, va a aparecer acá."
			/>
			<comp-lista
				v-else
				:cargando="store.cargando"
				:hay_mas="store.hay_mas"
				:mostrados="store.notificaciones.length"
				:total="store.resumen ? store.resumen.total : 0"
				@cargar_mas="store.cargar_mas()"
			>
				<!-- Tocar una no leida la marca: el punto de color es el estado, no un adorno. -->
				<ion-item
					v-for="notificacion in store.notificaciones"
					:key="notificacion.id"
					:button="!notificacion.leida_en"
					@click="marcar(notificacion)"
				>
					<ion-icon slot="start" :icon="icono_tipo(notificacion.tipo)" :color="notificacion.leida_en ? 'medium' : 'primary'" />
					<ion-label :class="{ 'raveat-notificacion--leida': notificacion.leida_en }">
						<h3 class="raveat-item-title">{{ notificacion.titulo }}</h3>
						<p>{{ notificacion.mensaje }}</p>
						<p class="raveat-item-meta">{{ formatear_fecha(notificacion.creado_en) }}</p>
					</ion-label>
					<ion-badge v-if="!notificacion.leida_en" slot="end" color="danger">nueva</ion-badge>
				</ion-item>
			</comp-lista>
		</div>
	</comp-page>
</template>

<script>import {
	IonBadge,
	IonButton,
	IonIcon,
	IonItem,
	IonLabel
} from '@ionic/vue';
import {
	bicycleOutline,
	checkmarkDoneOutline,
	informationCircleOutline,
	notificationsOffOutline,
	receiptOutline
} from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { use_notificaciones_store } from '@/stores/notificaciones_store';

const ICONO_TIPO = {
	pedido: receiptOutline,
	entrega: bicycleOutline,
	informacion: informationCircleOutline
};

export default {
	name: 'notificaciones_page',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		IonBadge,
		IonButton,
		IonIcon,
		IonItem,
		IonLabel
	},
	data(){
		return {
			icono: notificationsOffOutline,
			icono_leidas: checkmarkDoneOutline,
			store: use_notificaciones_store()
		};
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		marcar: function(notificacion){
			var vm = this;
			if(notificacion.leida_en) return;
			return vm.store.marcar_leida(notificacion.id);
		},
		icono_tipo: function(tipo){
			return ICONO_TIPO[tipo] || informationCircleOutline;
		},
		formatear_fecha: function(fecha){
			if(!fecha) return '';
			return new Date(fecha).toLocaleString('es-AR', {day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit'});
		}
	}
};</script>
