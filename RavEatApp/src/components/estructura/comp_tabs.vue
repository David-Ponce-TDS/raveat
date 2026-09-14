<template>
	<ion-tab-bar slot="bottom">
		<ion-tab-button
			v-for="item in items_tabs"
			:key="item.id"
			:tab="item.id"
			:href="item.ruta"
		>
			<ion-icon :icon="item.icono" />
			<ion-label>{{ item.titulo }}</ion-label>
		</ion-tab-button>
	</ion-tab-bar>
</template>

<script>import {
	IonIcon,
	IonLabel,
	IonTabBar,
	IonTabButton
} from '@ionic/vue';
import { obtener_tabs_rol } from '@/config/navegacion';
import { use_sesion_store } from '@/stores/sesion_store';

export default {
	name: 'comp_tabs',
	components: {
		IonIcon,
		IonLabel,
		IonTabBar,
		IonTabButton
	},
	data(){
		return {
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		// Los tabs siguen al rol de la sesion: la misma lista que el menu, recortada al maximo.
		items_tabs(){
			var vm = this;
			return obtener_tabs_rol(vm.sesion_store.rol_activo);
		}
	}
};</script>
