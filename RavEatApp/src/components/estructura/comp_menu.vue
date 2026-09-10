<template>
	<ion-menu :content-id="content_id" type="overlay">
		<ion-header>
			<ion-toolbar>
				<ion-title>RavEat</ion-title>
			</ion-toolbar>
		</ion-header>

		<ion-content>
			<ion-list class="menu_lista">
				<section v-for="grupo in grupos_menu" :key="grupo.id" class="menu_grupo" :aria-label="grupo.titulo">
					<p v-if="grupo.titulo" class="menu_grupo_titulo">{{ grupo.titulo }}</p>
					<ion-menu-toggle v-for="item in grupo.items" :key="item.id" :auto-hide="false">
						<ion-item
							button
							:detail="false"
							:router-link="item.ruta"
							router-direction="root"
							:class="{ seleccionado: es_item_activo(item.ruta) }"
						>
							<ion-icon slot="start" :icon="item.icono" />
							<ion-label>{{ item.titulo }}</ion-label>
						</ion-item>
					</ion-menu-toggle>
				</section>
			</ion-list>
		</ion-content>
	</ion-menu>
</template>

<script>import {
	IonContent,
	IonHeader,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonMenu,
	IonMenuToggle,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { obtener_grupos_menu_rol } from '@/config/navegacion';

export default {
	name: 'comp_menu',
	components: {
		IonContent,
		IonHeader,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonMenu,
		IonMenuToggle,
		IonTitle,
		IonToolbar
	},
	props: {
		content_id: {
			type: String,
			required: true
		}
	},
	computed: {
		grupos_menu(){
			// Sin rol activo devuelve todo. En v5 se le pasa el rol de la sesion y filtra solo.
			return obtener_grupos_menu_rol(null);
		}
	},
	methods: {
		es_item_activo: function(ruta){
			var vm = this;
			return vm.$route.path === ruta || vm.$route.path.startsWith(`${ruta}/`);
		}
	}
};</script>

<style scoped>
.menu_lista {
	padding: 6px 0 18px;
}
.menu_grupo + .menu_grupo {
	margin-top: 8px;
}
.menu_grupo_titulo {
	margin: 0;
	padding: 8px 16px 5px;
	color: var(--raveat-muted);
	font-size: 0.68rem;
	font-weight: 700;
	letter-spacing: 0.08em;
	text-transform: uppercase;
}
</style>
