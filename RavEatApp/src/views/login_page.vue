<template>
	<ion-page>
		<ion-content>
			<div class="ion-padding raveat-page-stack raveat-sin-header raveat-login">
				<!-- El logo vuelve a la vitrina: el que llego hasta aca sin querer entrar tiene
				     que poder volver a mirar la carta sin cerrar la app. -->
				<comp-vidriera
					lema="Entrá con tu cuenta del local"
					ruta="/vitrina"
					ruta_etiqueta="Volver a la vitrina"
				/>

				<!-- Un form de verdad y no dos inputs sueltos: asi el navegador y el teléfono
				     ofrecen guardar la contraseña y la completan la proxima vez. La app nunca la
				     guarda — eso es trabajo del gestor de contraseñas. -->
				<!-- La misma grilla de la pagina: sin ella, el aviso de error quedaba pegado a los
				     campos y al boton. -->
				<form class="raveat-page-stack" @submit.prevent="entrar">
					<ion-list class="raveat-card">
						<ion-item>
							<ion-input
								v-model="email"
								type="email"
								inputmode="email"
								autocomplete="username"
								name="email"
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
								name="password"
								label="Contraseña"
								label-placement="stacked"
								placeholder="Tu contraseña"
							/>
						</ion-item>
					</ion-list>

					<comp-aviso v-if="sesion_store.error" tono="error" :mensaje="sesion_store.error" />

					<!-- Sigue siendo el submit del form (Enter tambien entra), pero sin email o sin
					     contraseña no hay nada que mandar: apagado, en vez de un 400 de la API. -->
					<ion-button type="submit" expand="block" :disabled="sesion_store.cargando || !email.trim() || !password">
						<ion-spinner v-if="sesion_store.cargando" name="crescent" />
						<span v-else>Entrar</span>
					</ion-button>
				</form>

				<!-- Solo aparece si hay una sesión guardada Y el dispositivo tiene biometría. La
				     huella no reemplaza al login: desbloquea la sesión que ya existe. -->
				<ion-button v-if="puede_biometria" expand="block" fill="outline" @click="entrar_con_biometria">
					<ion-icon slot="start" :icon="icono_huella" />
					Entrar con huella
				</ion-button>

				<!-- Google solo aparece con el client id configurado: sin credencial no se muestra un
				     botón que no puede funcionar. En el teléfono es un botón nuestro (la pantalla de
				     cuentas la abre el plugin); en el navegador lo dibuja el propio Google. -->
				<template v-if="google_habilitado">
					<div class="raveat-login-separador">o</div>
					<!-- Fijo y no un aviso que se va solo: los dos errores que aparecen de verdad (la
					     configuración de Google Cloud y el email sin alta) son largos. -->
					<comp-aviso v-if="error_google" tono="error" :mensaje="error_google" />
					<ion-button v-if="google_es_nativo" expand="block" fill="outline" :disabled="sesion_store.cargando" @click="entrar_con_google">
						<ion-icon slot="start" :icon="icono_google" />
						{{ error_google ? 'Probar con otra cuenta' : 'Entrar con Google' }}
					</ion-button>
					<div v-show="!google_es_nativo" ref="boton_google" class="raveat-login-google" />
				</template>
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
	IonPage,
	IonSpinner
} from '@ionic/vue';
import { fingerPrintOutline, logoGoogle } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import { biometria_disponible, verificar_identidad } from '@/services/biometria_service';
import {
	entrar_con_google_nativo,
	google_es_nativo,
	google_habilitado,
	renderizar_boton_google
} from '@/services/google_auth_service';
import { restaurar_sesion } from '@/services/token_service';
import { use_sesion_store } from '@/stores/sesion_store';

// Esta es la unica pagina que NO usa comp-page: no tiene menu ni tabs, porque todavia no se sabe
// quien es el usuario ni que puede ver.
export default {
	name: 'login_page',
	components: {
		CompAviso: comp_aviso,
		CompVidriera: comp_vidriera,
		IonButton,
		IonContent,
		IonIcon,
		IonInput,
		IonItem,
		IonList,
		IonPage,
		IonSpinner
	},
	data(){
		return {
			email: '',
			// El error de Google vive en la pantalla y no en el store: puede venir del plugin
			// (configuracion, cuenta) sin que la sesion haya llegado a intentarse.
			error_google: null,
			google_es_nativo: google_es_nativo(),
			google_habilitado: google_habilitado(),
			icono_google: logoGoogle,
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
		if(vm.google_habilitado && !vm.google_es_nativo) renderizar_boton_google(vm.$refs.boton_google, vm.canjear_token_google);
	},
	methods: {
		entrar: async function(){
			var vm = this;
			vm.error_google = null;
			const entro = await vm.sesion_store.entrar(vm.email.trim(), vm.password);
			if(entro) vm.$router.replace('/app/inicio');
		},
		entrar_con_google: async function(){
			var vm = this;
			// Se limpia ANTES: si no, el error del intento anterior sigue en pantalla mientras se
			// elige la cuenta nueva y parece que ya fallo otra vez.
			vm.error_google = null;
			vm.sesion_store.error = null;
			const resultado = await entrar_con_google_nativo();
			if(!resultado.ok){
				// Sin mensaje = se cerro la pantalla de cuentas. No se avisa nada.
				vm.error_google = resultado.mensaje;
				return;
			}
			await vm.canjear_token_google(resultado.id_token);
		},
		// El paso que comparten los dos caminos: el token de Google se cambia por la sesion de
		// RavEat. Si el canje falla, el store ya solto la cuenta y el boton ofrece probar con otra.
		canjear_token_google: async function(id_token){
			var vm = this;
			const entro = await vm.sesion_store.entrar_con_google(id_token);
			if(entro){
				vm.$router.replace('/app/inicio');
				return;
			}
			vm.error_google = vm.sesion_store.error;
			vm.sesion_store.error = null;
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
