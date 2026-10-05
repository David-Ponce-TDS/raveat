<template>
	<comp-page titulo="Mi cuenta">
		<div class="ion-padding raveat-page-stack">
			<!-- La tarjeta de presentacion: la inicial sobre el gradiente de la app, nombre, email
			     y el rol como pildora. En advertencia cuando falta: todavia no puede operar. -->
			<div v-if="sesion_store.usuario" class="raveat-card raveat-perfil">
				<div class="raveat-perfil__avatar">{{ inicial }}</div>
				<div class="raveat-perfil__texto">
					<h2>{{ sesion_store.usuario.nombre }}</h2>
					<p>{{ sesion_store.usuario.email }}</p>
				</div>
				<span class="raveat-pill" :class="sesion_store.rol_activo ? 'raveat-tinte-primary' : 'raveat-tinte-warning'">
					<ion-icon :icon="sesion_store.rol_activo ? icono_rol : icono_espera" aria-hidden="true" />
					{{ sesion_store.usuario.rol_nombre || 'Sin rol' }}
				</span>
			</div>

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
					<ion-card-subtitle>Preferencias</ion-card-subtitle>
					<ion-card-title>Tema y vibración</ion-card-title>
				</ion-card-header>
				<ion-card-content>
					<ion-item>
						<ion-icon slot="start" :icon="icono_tema" />
						<ion-label>Modo oscuro</ion-label>
						<ion-toggle
							slot="end"
							:checked="tema_oscuro"
							@ion-change="cambiar_tema"
						/>
					</ion-item>
					<ion-item :lines="'none'">
						<ion-icon slot="start" :icon="icono_vibracion" />
						<ion-label>Vibración</ion-label>
						<ion-toggle
							slot="end"
							:checked="vibracion_activa"
							@ion-change="cambiar_vibracion"
						/>
					</ion-item>
				</ion-card-content>
			</ion-card>

			<!-- Solo el admin, con red, y al pie a proposito: no es una accion del local sino una
			     herramienta para volver a arrancar un ensayo desde cero. En la operacion normal un
			     pedido no se borra: se cancela. -->
			<ion-card v-if="sesion_store.es_admin && !offline_store.sin_conexion" class="raveat-panel raveat-zona-peligrosa">
				<ion-card-header>
					<ion-card-subtitle>Herramientas de prueba</ion-card-subtitle>
					<ion-card-title>Empezar de cero</ion-card-title>
				</ion-card-header>
				<ion-card-content>
					<p class="raveat-zona-peligrosa__aviso">
						Borra <strong>todos</strong> los pedidos con sus ítems, entregas y la bandeja de
						notificaciones. La carta, los clientes y los usuarios no se tocan. No se puede deshacer.
					</p>
					<ion-button expand="block" color="danger" :disabled="vaciando" @click="confirmar_vaciado">
						<ion-spinner v-if="vaciando" name="crescent" />
						<template v-else>
							<ion-icon slot="start" :icon="icono_borrar" />
							Borrar todos los pedidos
						</template>
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
	IonToggle,
	alertController
} from '@ionic/vue';
import {
	contrastOutline,
	hourglassOutline,
	logOutOutline,
	pulseOutline,
	shieldCheckmarkOutline,
	trashBinOutline
} from 'ionicons/icons';
import { mapActions, mapState } from 'pinia';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { avisar_error, avisar_exito } from '@/services/avisos_service';
import { vaciar_pedidos } from '@/services/mantenimiento_service';
import { vibrar_toque } from '@/services/vibracion_service';
import { use_app_store } from '@/stores/app_store';
import { use_offline_store } from '@/stores/offline_store';
import { use_pedidos_store } from '@/stores/pedidos_store';
import { use_sesion_store } from '@/stores/sesion_store';

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
			icono_borrar: trashBinOutline,
			icono_espera: hourglassOutline,
			icono_rol: shieldCheckmarkOutline,
			icono_salir: logOutOutline,
			icono_tema: contrastOutline,
			icono_vibracion: pulseOutline,
			offline_store: use_offline_store(),
			pedidos_store: use_pedidos_store(),
			sesion_store: use_sesion_store(),
			vaciando: false
		};
	},
	computed: {
		...mapState(use_app_store, [
			'tema_oscuro',
			'vibracion_activa'
		]),
		inicial(){
			var vm = this;
			return ((vm.sesion_store.usuario && vm.sesion_store.usuario.nombre) || '?').trim().charAt(0).toUpperCase();
		}
	},
	methods: {
		...mapActions(use_app_store, {
			alternar: 'alternar_tema',
			alternar_vibracion: 'alternar_vibracion'
		}),
		// El toggle emite en cada render inicial: solo se alterna si el valor cambio de verdad.
		cambiar_tema: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.tema_oscuro) return;
			vm.alternar();
		},
		cambiar_vibracion: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.vibracion_activa) return;
			vm.alternar_vibracion();
			// Al encenderla vibra una vez: es la forma de "ver" que funciona.
			if(vm.vibracion_activa) vibrar_toque();
		},
		// Confirmacion escrita: un borrado total detras de un solo toque es un accidente esperando
		// turno. Escribir BORRAR obliga a leer que se esta por hacer.
		confirmar_vaciado: async function(){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Borrar todos los pedidos',
				message: 'Se borran todos los pedidos, sus ítems, entregas y notificaciones. No se puede deshacer. Escribí BORRAR para confirmar.',
				inputs: [{name: 'confirmacion', type: 'text', placeholder: 'BORRAR'}],
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{
						text: 'Borrar todo',
						role: 'destructive',
						handler: function(datos){
							// Devolver false deja la alerta abierta: la palabra no coincidio.
							if((datos.confirmacion || '').trim().toUpperCase() != 'BORRAR') return false;
							vm.vaciar();
						}
					}
				]
			});
			await alerta.present();
		},
		vaciar: async function(){
			var vm = this;
			vm.vaciando = true;
			try{
				const respuesta = await vaciar_pedidos();
				// La cola offline se vacia con la base: un cambio encolado apuntaria a un pedido
				// que ya no existe. Y la lista de Pedidos se recarga para no mostrar lo borrado.
				vm.offline_store.guardar_cola([]);
				vm.pedidos_store.cargar();
				avisar_exito(respuesta.mensaje || 'Se borraron todos los pedidos.');
			}catch(error){
				avisar_error(error.mensaje || 'No se pudo borrar. Probá de nuevo.');
			}finally{
				vm.vaciando = false;
			}
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
