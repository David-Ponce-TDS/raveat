<template>
	<ion-page>
		<ion-header>
			<ion-toolbar>
				<ion-title>RavEat</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="cambiar_tema">
						<ion-icon slot="icon-only" :icon="icono_tema" />
					</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>

		<ion-content class="ion-padding">
			<div class="raveat-page-stack">
				<header class="raveat-vidriera">
					<img class="raveat-vidriera__logo" src="/logo_final.png" alt="RavEat" />
					<p class="raveat-vidriera__lema">Version 1 · el proyecto ya corre en el telefono</p>
				</header>

				<ion-card class="raveat-card">
					<ion-card-header>
						<ion-card-title>Hola, RavEat</ion-card-title>
					</ion-card-header>
					<ion-card-content>
						Proyecto Ionic Vue corriendo en el navegador y en el telefono con Capacitor.
						El tema actual es <strong>{{ nombre_tema }}</strong> y queda guardado para el
						proximo arranque.
					</ion-card-content>
				</ion-card>

				<h2 class="raveat-seccion-titulo">De la carta</h2>
				<div v-if="cargando" class="ion-text-center ion-padding">
					<ion-spinner name="crescent" />
				</div>
				<div v-else class="raveat-grilla">
					<article v-for="producto in destacados" :key="producto.id" class="raveat-producto-card" :class="{ agotado: !producto.disponible }">
						<div class="raveat-producto-card__imagen">
							<img :src="producto.imagen_url" :alt="producto.nombre" loading="lazy" />
							<span class="raveat-producto-card__precio">{{ formatear_precio(producto.precio) }}</span>
							<span v-if="!producto.disponible" class="raveat-producto-card__agotado">No disponible</span>
						</div>
						<div class="raveat-producto-card__info">
							<h3>{{ producto.nombre }}</h3>
							<p>{{ producto.descripcion }}</p>
						</div>
					</article>
				</div>

				<h2 class="raveat-seccion-titulo">Con que esta hecha</h2>
				<ion-list class="raveat-card">
					<ion-item v-for="(pieza, indice) in piezas" :key="pieza.nombre" :lines="indice === piezas.length - 1 ? 'none' : 'full'">
						<ion-icon slot="start" :icon="pieza.icono" />
						<ion-label>
							<h3 class="raveat-item-title">{{ pieza.nombre }}</h3>
							<p class="raveat-item-meta">{{ pieza.para_que }}</p>
						</ion-label>
					</ion-item>
				</ion-list>

				<ion-button expand="block" @click="saludar">
					Probar un boton de Ionic
				</ion-button>
			</div>
		</ion-content>
	</ion-page>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonCard,
	IonCardContent,
	IonCardHeader,
	IonCardTitle,
	IonContent,
	IonHeader,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonPage,
	IonSpinner,
	IonTitle,
	IonToolbar,
	alertController
} from '@ionic/vue';
import { contrastOutline, logoAndroid, logoVue, phonePortraitOutline, serverOutline } from 'ionicons/icons';
import { mapActions, mapState } from 'pinia';
import { obtener_carta } from '@/datos/carta';
import { use_app_store } from '@/stores/app_store';

export default {
	name: 'inicio_page',
	components: {
		IonButton,
		IonButtons,
		IonCard,
		IonCardContent,
		IonCardHeader,
		IonCardTitle,
		IonContent,
		IonHeader,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonPage,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	data(){
		return {
			cargando: true,
			destacados: [],
			piezas: [
				{
					nombre: 'Vue 3',
					para_que: 'El framework de la interfaz, con Options API y JavaScript.',
					icono: logoVue
				},
				{
					nombre: 'Ionic',
					para_que: 'Los componentes con apariencia de app movil.',
					icono: phonePortraitOutline
				},
				{
					nombre: 'Capacitor',
					para_que: 'Empaqueta el proyecto web como app nativa.',
					icono: logoAndroid
				},
				{
					nombre: 'Vite',
					para_que: 'Servidor de desarrollo y compilador de produccion.',
					icono: serverOutline
				}
			]
		};
	},
	computed: {
		...mapState(use_app_store, [
			'tema_oscuro'
		]),
		nombre_tema(){
			var vm = this;
			return vm.tema_oscuro ? 'oscuro' : 'claro';
		},
		icono_tema(){
			return contrastOutline;
		}
	},
	mounted(){
		var vm = this;
		vm.cargar_destacados();
	},
	methods: {
		...mapActions(use_app_store, {
			cambiar_tema: 'alternar_tema'
		}),
		// En v1 los datos salen de un archivo, pero se piden como se pediran despues: async y con
		// un estado de carga. En v3 cambia de donde vienen y esta pantalla no se entera.
		cargar_destacados: async function(){
			var vm = this;
			vm.cargando = true;
			const carta = await obtener_carta();
			vm.destacados = carta.productos.slice(0, 4);
			vm.cargando = false;
		},
		formatear_precio: function(valor){
			return new Intl.NumberFormat('es-AR', {style: 'currency', currency: 'ARS', maximumFractionDigits: 0}).format(valor || 0);
		},
		saludar: async function(){
			const alerta = await alertController.create({
				header: 'Funciona',
				message: 'Este dialogo lo dibuja Ionic, igual en el navegador y en el telefono.',
				buttons: [
					'Listo'
				]
			});
			await alerta.present();
		}
	}
};</script>
