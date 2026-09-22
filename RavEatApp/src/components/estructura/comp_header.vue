<template>
	<ion-header>
		<ion-toolbar>
			<ion-buttons slot="start">
				<ion-menu-button />
			</ion-buttons>
			<ion-title>{{ titulo }}</ion-title>
			<ion-buttons slot="end">
				<!-- La campanita es el acceso a la bandeja. El contador sale del resumen de la API
				     —la bandeja completa—, nunca de contar las filas descargadas. -->
				<ion-button v-if="puede_ver_bandeja" class="raveat-campanita" @click="ir_a_notificaciones">
					<ion-icon slot="icon-only" :icon="icono_campanita" />
					<span v-if="no_leidas > 0" class="raveat-campanita__badge">{{ no_leidas > 99 ? '99+' : no_leidas }}</span>
				</ion-button>
				<slot name="acciones" />
			</ion-buttons>
		</ion-toolbar>
	</ion-header>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonHeader,
	IonIcon,
	IonMenuButton,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { notificationsOutline } from 'ionicons/icons';
import { use_notificaciones_store } from '@/stores/notificaciones_store';
import { use_sesion_store } from '@/stores/sesion_store';

export default {
	name: 'comp_header',
	components: {
		IonButton,
		IonButtons,
		IonHeader,
		IonIcon,
		IonMenuButton,
		IonTitle,
		IonToolbar
	},
	props: {
		titulo: {
			type: String,
			required: true
		}
	},
	data(){
		return {
			icono_campanita: notificationsOutline,
			notificaciones_store: use_notificaciones_store(),
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		puede_ver_bandeja(){
			var vm = this;
			return Boolean(vm.sesion_store.rol_activo);
		},
		no_leidas(){
			var vm = this;
			return vm.notificaciones_store.no_leidas;
		}
	},
	mounted(){
		var vm = this;
		// La primera carga, la de la pantalla que se abre. El refresco al navegar lo hace el
		// router: Ionic cachea las paginas y este mounted no vuelve a correr al regresar a una.
		if(vm.puede_ver_bandeja) vm.notificaciones_store.cargar();
	},
	methods: {
		ir_a_notificaciones: function(){
			var vm = this;
			vm.$router.push('/app/notificaciones');
		}
	}
};</script>
