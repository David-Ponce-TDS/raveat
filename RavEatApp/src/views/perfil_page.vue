<template>
	<comp-page titulo="Mi cuenta">
		<div class="ion-padding raveat-page-stack">
			<ion-card class="raveat-panel">
				<ion-card-header>
					<ion-card-subtitle>Apariencia</ion-card-subtitle>
					<ion-card-title>Tema de la app</ion-card-title>
				</ion-card-header>
				<ion-card-content>
					<ion-item :lines="'none'">
						<ion-icon slot="start" :icon="icono_tema" />
						<ion-label>Modo oscuro</ion-label>
						<ion-toggle
							slot="end"
							:checked="tema_oscuro"
							@ion-change="cambiar_tema"
						/>
					</ion-item>
				</ion-card-content>
			</ion-card>

			<comp-estado-vacio
				:icono="icono_usuario"
				titulo="Todavía no hay sesión"
				mensaje="El usuario, su rol y el ingreso con huella llegan en la versión 5."
			/>
		</div>
	</comp-page>
</template>

<script>import {
	IonCard,
	IonCardContent,
	IonCardHeader,
	IonCardSubtitle,
	IonCardTitle,
	IonIcon,
	IonItem,
	IonLabel,
	IonToggle
} from '@ionic/vue';
import { contrastOutline, personCircleOutline } from 'ionicons/icons';
import { mapActions, mapState } from 'pinia';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { use_app_store } from '@/stores/app_store';

export default {
	name: 'perfil_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompPage: comp_page,
		IonCard,
		IonCardContent,
		IonCardHeader,
		IonCardSubtitle,
		IonCardTitle,
		IonIcon,
		IonItem,
		IonLabel,
		IonToggle
	},
	data(){
		return {
			icono_tema: contrastOutline,
			icono_usuario: personCircleOutline
		};
	},
	computed: {
		...mapState(use_app_store, [
			'tema_oscuro'
		])
	},
	methods: {
		...mapActions(use_app_store, {
			alternar: 'alternar_tema'
		}),
		// El toggle emite en cada render inicial: solo se alterna si el valor cambio de verdad.
		cambiar_tema: function(evento){
			var vm = this;
			if(evento.detail.checked === vm.tema_oscuro) return;
			vm.alternar();
		}
	}
};</script>
