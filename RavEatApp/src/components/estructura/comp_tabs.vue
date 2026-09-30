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
import { use_offline_store } from '@/stores/offline_store';
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
			offline_store: use_offline_store(),
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		items_tabs(){
			var vm = this;
			// El mismo rol que usa el menu, recortado al maximo de tabs. Con null solo quedaban
			// las pantallas de [TODOS]: la barra mostraba Inicio y Mi cuenta para cualquiera.
			// Sin red quedan las pantallas que tienen sus datos guardados.
			return obtener_tabs_rol(vm.sesion_store.rol_activo, vm.offline_store.sin_conexion);
		}
	}
};</script>
