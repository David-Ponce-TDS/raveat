<template>
	<ion-page>
		<comp-header :titulo="titulo">
			<template #acciones>
				<slot name="acciones" />
			</template>
		</comp-header>

		<ion-content :fullscreen="true">
			<ion-refresher v-if="mostrar_actualizar" slot="fixed" @ionRefresh="refrescar">
				<ion-refresher-content pulling-text="Desliza para actualizar" refreshing-text="Actualizando..." />
			</ion-refresher>
			<slot />
		</ion-content>
	</ion-page>
</template>

<script>import {
	IonContent,
	IonPage,
	IonRefresher,
	IonRefresherContent
} from '@ionic/vue';
import comp_header from '@/components/estructura/comp_header.vue';

export default {
	name: 'comp_page',
	components: {
		CompHeader: comp_header,
		IonContent,
		IonPage,
		IonRefresher,
		IonRefresherContent
	},
	emits: [
		'actualizar'
	],
	props: {
		mostrar_actualizar: {
			type: Boolean,
			default: false
		},
		titulo: {
			type: String,
			required: true
		}
	},
	methods: {
		refrescar: function(evento){
			var vm = this;
			vm.$emit('actualizar');
			// El spinner se cierra a mano: sin datos que esperar, Ionic lo dejaria girando.
			window.setTimeout(() => evento.detail.complete(), 350);
		}
	}
};</script>
