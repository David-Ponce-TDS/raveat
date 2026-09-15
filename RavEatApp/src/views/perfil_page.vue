<template>
	<comp-page titulo="Mi cuenta">
		<div class="ion-padding raveat-page-stack">
			<ion-list v-if="sesion_store.usuario" class="raveat-card">
				<ion-item :lines="'none'">
					<ion-avatar slot="start" class="raveat-avatar-inicial">
						<span>{{ inicial }}</span>
					</ion-avatar>
					<ion-label>
						<h3 class="raveat-item-title">{{ sesion_store.usuario.nombre }}</h3>
						<p>{{ sesion_store.usuario.email }}</p>
					</ion-label>
					<ion-badge slot="end" :color="sesion_store.rol_activo ? 'primary' : 'warning'">
						{{ sesion_store.usuario.rol_nombre || 'Sin rol' }}
					</ion-badge>
				</ion-item>
			</ion-list>

			<!-- Autenticado y sin rol: entró, pero todavía no puede operar. Es un estado del
			     modelo, no un error, y por eso se explica en vez de mostrar pantallas vacías. -->
			<comp-estado-vacio
				v-if="sesion_store.pendiente_de_habilitacion"
				:icono="icono_espera"
				titulo="Tu cuenta todavía no está habilitada"
				mensaje="Ya iniciaste sesión, pero un administrador tiene que asignarte un rol para que puedas operar."
			/>

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
						v-if="estado != 'inicial'"
						class="raveat-diagnostico__resultado"
						:class="estado"
					>
						<ion-spinner v-if="estado == 'probando'" name="dots" />
						<ion-icon v-else :icon="estado == 'ok' ? icono_ok : icono_falla" />
						<span>{{ mensaje }}</span>
					</div>

					<ion-button
						expand="block"
						fill="outline"
						:disabled="estado == 'probando'"
						@click="probar"
					>
						Probar conexión
					</ion-button>
				</ion-card-content>
			</ion-card>

			<ion-button expand="block" color="danger" fill="outline" @click="salir">
				<ion-icon slot="start" :icon="icono_salir" />
				Cerrar sesión
			</ion-button>
		</div>
	</comp-page>
</template>

<script>import {
	IonAvatar,
	IonBadge,
	IonButton,
	IonCard,
	IonCardContent,
	IonCardHeader,
	IonCardSubtitle,
	IonCardTitle,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonSpinner,
	IonToggle,
	alertController
} from '@ionic/vue';
import {
	alertCircleOutline,
	checkmarkCircleOutline,
	cloudOutline,
	contrastOutline,
	hourglassOutline,
	logOutOutline
} from 'ionicons/icons';
import { mapActions, mapState } from 'pinia';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { obtener_api_url } from '@/config/debug';
import { consultar_health } from '@/services/health_service';
import { use_app_store } from '@/stores/app_store';
import { use_sesion_store } from '@/stores/sesion_store';

// Diagnostico: contra que URL pega la app y si esa API responde. En el telefono no hay consola a
// mano, y esta es la forma de verlo.
export default {
	name: 'perfil_page',
	components: {
		CompEstadoVacio: comp_estado_vacio,
		CompPage: comp_page,
		IonAvatar,
		IonBadge,
		IonButton,
		IonCard,
		IonCardContent,
		IonCardHeader,
		IonCardSubtitle,
		IonCardTitle,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonSpinner,
		IonToggle
	},
	data(){
		return {
			api_url: obtener_api_url(),
			estado: 'inicial',
			icono_api: cloudOutline,
			icono_espera: hourglassOutline,
			icono_falla: alertCircleOutline,
			icono_ok: checkmarkCircleOutline,
			icono_salir: logOutOutline,
			icono_tema: contrastOutline,
			mensaje: '',
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		...mapState(use_app_store, [
			'tema_oscuro'
		]),
		inicial(){
			var vm = this;
			return ((vm.sesion_store.usuario && vm.sesion_store.usuario.nombre) || '?').trim().charAt(0).toUpperCase();
		}
	},
	methods: {
		...mapActions(use_app_store, {
			alternar: 'alternar_tema'
		}),
		// El toggle emite en cada render inicial: solo se alterna si el valor cambio de verdad.
		cambiar_tema: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.tema_oscuro) return;
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
		},
		salir: async function(){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Cerrar sesión',
				message: '¿Querés salir de tu cuenta?',
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{
						text: 'Salir',
						role: 'destructive',
						handler: async function(){
							await vm.sesion_store.salir();
							vm.$router.replace('/login');
						}
					}
				]
			});
			await alerta.present();
		}
	}
};</script>
