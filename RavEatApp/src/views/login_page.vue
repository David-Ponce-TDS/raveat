<template>
	<ion-page>
		<ion-content>
			<div class="ion-padding raveat-page-stack raveat-login">
				<comp-vidriera lema="Entrá con tu cuenta del local" />

				<ion-list class="raveat-card">
					<ion-item>
						<ion-input
							v-model="email"
							type="email"
							inputmode="email"
							autocomplete="username"
							label="Email"
							label-placement="stacked"
							placeholder="vos@raveat.local"
						/>
					</ion-item>
					<ion-item lines="none">
						<ion-input
							v-model="password"
							type="password"
							autocomplete="current-password"
							label="Contraseña"
							label-placement="stacked"
							placeholder="Tu contraseña"
							@keyup.enter="entrar"
						/>
					</ion-item>
				</ion-list>

				<ion-note v-if="sesion_store.error" color="danger">{{ sesion_store.error }}</ion-note>

				<ion-button expand="block" :disabled="sesion_store.cargando" @click="entrar">
					<ion-spinner v-if="sesion_store.cargando" name="crescent" />
					<span v-else>Entrar</span>
				</ion-button>

				<!-- Solo aparece si hay una sesión guardada Y el dispositivo tiene biometría. La
				     huella no reemplaza al login: desbloquea la sesión que ya existe. -->
				<ion-button v-if="puede_biometria" expand="block" fill="outline" @click="entrar_con_biometria">
					<ion-icon slot="start" :icon="icono_huella" />
					Entrar con huella
				</ion-button>
			</div>
		</ion-content>
	</ion-page>
</template>

<script>import {
	IonButton,
	IonContent,
	IonIcon,
	IonInput,
	IonItem,
	IonList,
	IonNote,
	IonPage,
	IonSpinner
} from '@ionic/vue';
import { fingerPrintOutline } from 'ionicons/icons';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import { biometria_disponible, verificar_identidad } from '@/services/biometria_service';
import { restaurar_sesion } from '@/services/token_service';
import { use_sesion_store } from '@/stores/sesion_store';

// Esta es la unica pagina que NO usa comp-page: no tiene menu ni tabs, porque todavia no se sabe
// quien es el usuario ni que puede ver.
export default {
	name: 'login_page',
	components: {
		CompVidriera: comp_vidriera,
		IonButton,
		IonContent,
		IonIcon,
		IonInput,
		IonItem,
		IonList,
		IonNote,
		IonPage,
		IonSpinner
	},
	data(){
		return {
			email: '',
			icono_huella: fingerPrintOutline,
			password: '',
			puede_biometria: false,
			sesion_store: use_sesion_store()
		};
	},
	async mounted(){
		var vm = this;
		// Dos condiciones, y las dos hacen falta: que el dispositivo tenga biometria y que haya
		// una sesion guardada para desbloquear. Sin login previo no hay nada que desbloquear.
		const hay_sesion_guardada = Boolean(await restaurar_sesion());
		vm.puede_biometria = hay_sesion_guardada && await biometria_disponible();
	},
	methods: {
		entrar: async function(){
			var vm = this;
			const entro = await vm.sesion_store.entrar(vm.email.trim(), vm.password);
			if(entro) vm.$router.replace('/app/inicio');
		},
		entrar_con_biometria: async function(){
			var vm = this;
			const confirmado = await verificar_identidad();
			// Si el sistema no confirma, no pasa nada: se queda en el login. No se muestra error
			// porque cancelar la huella es una decision del usuario, no una falla.
			if(!confirmado) return;
			const usuario = await vm.sesion_store.iniciar();
			if(usuario) vm.$router.replace('/app/inicio');
		}
	}
};</script>
