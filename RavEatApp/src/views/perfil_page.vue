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

			<ion-card class="raveat-panel">
				<ion-card-header>
					<ion-card-subtitle>Conexión</ion-card-subtitle>
					<ion-card-title>Diagnóstico</ion-card-title>
				</ion-card-header>
				<ion-card-content>
					<ion-item :lines="'none'" class="raveat-diagnostico__api">
						<ion-icon slot="start" :icon="icono_api" />
						<ion-label>
							<p>Esta app le pide los datos a</p>
							<h3 class="raveat-item-title">{{ api_url }}</h3>
						</ion-label>
					</ion-item>

					<div
						v-if="estado !== 'inicial'"
						class="raveat-diagnostico__resultado"
						:class="estado"
					>
						<ion-spinner v-if="estado === 'probando'" name="dots" />
						<ion-icon v-else :icon="estado === 'ok' ? icono_ok : icono_falla" />
						<span>{{ mensaje }}</span>
					</div>

					<ion-button
						expand="block"
						fill="outline"
						:disabled="estado === 'probando'"
						@click="probar"
					>
						Probar conexión
					</ion-button>
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
	IonButton,
	IonCard,
	IonCardContent,
	IonCardHeader,
	IonCardSubtitle,
	IonCardTitle,
	IonIcon,
	IonItem,
	IonLabel,
	IonSpinner,
	IonToggle
} from '@ionic/vue';
import {
	alertCircleOutline,
	checkmarkCircleOutline,
	cloudOutline,
	contrastOutline,
	personCircleOutline
} from 'ionicons/icons';
import { mapActions, mapState } from 'pinia';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { obtener_api_url } from '@/config/debug';
import { consultar_health } from '@/services/health_service';
import { use_app_store } from '@/stores/app_store';

// El panel de diagnostico contesta las dos preguntas con las que empieza cualquier problema de
// red: contra que direccion esta pegando la app, y si esa API responde. Sin esto la unica forma
// de saberlo es abrir la consola del navegador, que en el telefono no esta a mano.
export default {
	name: 'perfil_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompPage: comp_page,
		IonButton,
		IonCard,
		IonCardContent,
		IonCardHeader,
		IonCardSubtitle,
		IonCardTitle,
		IonIcon,
		IonItem,
		IonLabel,
		IonSpinner,
		IonToggle
	},
	data(){
		return {
			api_url: obtener_api_url(),
			estado: 'inicial',
			icono_api: cloudOutline,
			icono_falla: alertCircleOutline,
			icono_ok: checkmarkCircleOutline,
			icono_tema: contrastOutline,
			icono_usuario: personCircleOutline,
			mensaje: ''
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
		},
		probar: async function(){
			var vm = this;
			vm.estado = 'probando';
			vm.mensaje = 'Probando…';
			try{
				const respuesta = await consultar_health();
				vm.estado = 'ok';
				vm.mensaje = `La API respondió: ${respuesta.status} · ${vm.formatear_hora(respuesta.utc)}`;
			}catch(error){
				vm.estado = 'error';
				vm.mensaje = error.mensaje;
			}
		},
		// La hora la manda la API, no el telefono: si difieren, lo que se ve es el reloj del server.
		formatear_hora: function(utc){
			if(!utc) return '';
			return new Date(utc).toLocaleTimeString('es-AR');
		}
	}
};</script>
