<template>
	<ion-page>
		<ion-content :fullscreen="true">
			<ion-refresher slot="fixed" @ionRefresh="refrescar">
				<ion-refresher-content pulling-text="Desliza para actualizar" refreshing-text="Actualizando..." />
			</ion-refresher>

			<div class="ion-padding raveat-page-stack">
				<!-- El acceso al login es el icono de la esquina; el logo del login es el camino
				     de vuelta. Un solo gesto en cada sentido y ningun boton grande que compita
				     con la carta, que es lo que se viene a ver. -->
				<comp-vidriera lema="Nuestra carta, abierta a todos">
					<template #esquina>
						<ion-button
							class="raveat-vidriera__login"
							fill="clear"
							router-link="/login"
							aria-label="Entrar con tu cuenta"
						>
							<ion-icon slot="icon-only" :icon="icono_login" />
						</ion-button>
					</template>
				</comp-vidriera>

				<comp-vitrina ref="vitrina" />
			</div>
		</ion-content>
	</ion-page>
</template>

<script>import {
	IonButton,
	IonContent,
	IonIcon,
	IonPage,
	IonRefresher,
	IonRefresherContent
} from '@ionic/vue';
import { logInOutline } from 'ionicons/icons';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import comp_vitrina from '@/components/dominio/comp_vitrina.vue';

// La vitrina publica: la carta que ve cualquiera que abre la app, sin cuenta. Es la unica
// pagina ademas del login que no usa comp-page — no tiene menu ni tabs porque todavia no hay
// sesion, y su unica salida hacia adentro es el icono de la esquina.
// Los endpoints que alimentan la carta estan marcados [AllowAnonymous] en la API desde v3: esta
// pantalla no necesita token para nada.
export default {
	name: 'vitrina_page',
	components: {
		CompVidriera: comp_vidriera,
		CompVitrina: comp_vitrina,
		IonButton,
		IonContent,
		IonIcon,
		IonPage,
		IonRefresher,
		IonRefresherContent
	},
	data(){
		return {
			icono_login: logInOutline
		};
	},
	methods: {
		refrescar: function(evento){
			var vm = this;
			vm.$refs.vitrina.cargar();
			// El spinner se cierra a mano: sin datos que esperar, Ionic lo dejaria girando.
			window.setTimeout(() => evento.detail.complete(), 350);
		}
	}
};</script>
