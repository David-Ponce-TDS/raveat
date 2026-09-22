<template>
	<ion-modal :is-open="abierto" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ titulo }}</ion-title>
				<ion-buttons slot="end">
					<slot name="acciones" />
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content>
			<!-- El mismo gesto que en las listas: una ficha abierta tambien envejece —otro usuario
			     cobra o mueve el pedido mientras esta en pantalla— y cerrar y volver a abrir para
			     ver lo nuevo es pedirle al usuario que adivine que la pantalla no se actualiza. -->
			<ion-refresher v-if="mostrar_actualizar" slot="fixed" @ionRefresh="refrescar">
				<ion-refresher-content pulling-text="Desliza para actualizar" refreshing-text="Actualizando..." />
			</ion-refresher>
			<div class="ion-padding raveat-detalle">
				<slot />
			</div>
		</ion-content>
	</ion-modal>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonContent,
	IonHeader,
	IonModal,
	IonRefresher,
	IonRefresherContent,
	IonTitle,
	IonToolbar
} from '@ionic/vue';

// El armazon de cualquier modal de detalle de la app: cabecera con titulo, acciones opcionales y
// boton Cerrar, y el contenido en el slot por defecto. Es la contracara de comp-page: una pantalla
// completa tiene su cascara, una ficha tambien, y asi todas las fichas se abren y se cierran igual.
export default {
	name: 'comp_modal_detalle',
	components: {
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonModal,
		IonRefresher,
		IonRefresherContent,
		IonTitle,
		IonToolbar
	},
	emits: [
		'actualizar',
		'cerrar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		mostrar_actualizar: {
			type: Boolean,
			default: false
		},
		titulo: {
			type: String,
			default: ''
		}
	},
	methods: {
		refrescar: function(evento){
			var vm = this;
			vm.$emit('actualizar');
			// El spinner se cierra a mano, igual que en comp-page: sin datos que esperar, Ionic lo
			// dejaria girando para siempre.
			window.setTimeout(() => evento.detail.complete(), 350);
		}
	}
};</script>
