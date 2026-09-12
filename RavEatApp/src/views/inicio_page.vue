<template>
	<comp-page titulo="RavEat">
		<div class="ion-padding raveat-page-stack">
			<comp-vidriera lema="Versión 2 · una sola fuente de verdad para navegar" />

			<ion-card class="raveat-card">
				<ion-card-header>
					<ion-card-title>Navegación por configuración</ion-card-title>
				</ion-card-header>
				<ion-card-content>
					El menú lateral, la barra de abajo y las rutas de esta app no se escriben por
					separado: los tres salen del mismo archivo,
					<strong>src/config/navegacion.js</strong>. Agregar una pantalla es agregar un
					objeto ahí.
				</ion-card-content>
			</ion-card>

			<h2 class="raveat-seccion-titulo">De la carta</h2>
			<comp-esqueleto v-if="cargando" :filas="2" />
			<div v-else class="raveat-grilla">
				<comp-producto-card
					v-for="producto in destacados"
					:key="producto.id"
					:producto="producto"
				/>
			</div>

			<h2 class="raveat-seccion-titulo">Qué sale de ese archivo</h2>
			<ion-list class="raveat-card">
				<ion-item v-for="(pieza, indice) in piezas" :key="pieza.nombre" :lines="indice === piezas.length - 1 ? 'none' : 'full'">
					<ion-icon slot="start" :icon="pieza.icono" />
					<ion-label>
						<h3 class="raveat-item-title">{{ pieza.nombre }}</h3>
						<p class="raveat-item-meta">{{ pieza.para_que }}</p>
					</ion-label>
				</ion-item>
			</ion-list>
		</div>
	</comp-page>
</template>

<script>import {
	IonCard,
	IonCardContent,
	IonCardHeader,
	IonCardTitle,
	IonIcon,
	IonItem,
	IonLabel,
	IonList
} from '@ionic/vue';
import {
	gitBranchOutline,
	listOutline,
	menuOutline,
	shieldCheckmarkOutline
} from 'ionicons/icons';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_producto_card from '@/components/dominio/comp_producto_card.vue';
import comp_vidriera from '@/components/estructura/comp_vidriera.vue';
import { obtener_carta } from '@/datos/carta';

export default {
	name: 'inicio_page',
	components: {
		CompEsqueleto: comp_esqueleto,
		CompPage: comp_page,
		CompProductoCard: comp_producto_card,
		CompVidriera: comp_vidriera,
		IonCard,
		IonCardContent,
		IonCardHeader,
		IonCardTitle,
		IonIcon,
		IonItem,
		IonLabel,
		IonList
	},
	data(){
		return {
			cargando: true,
			destacados: [],
			piezas: [
				{
					nombre: 'Rutas',
					para_que: 'El router se genera recorriendo la configuración.',
					icono: gitBranchOutline
				},
				{
					nombre: 'Menú lateral',
					para_que: 'Se arma con los items agrupados de la misma lista.',
					icono: menuOutline
				},
				{
					nombre: 'Tabs',
					para_que: 'Salen de la misma lista, con un máximo de cinco.',
					icono: listOutline
				},
				{
					nombre: 'Guard',
					para_que: 'Lee los roles declarados. Empieza a bloquear en v5.',
					icono: shieldCheckmarkOutline
				}
			]
		};
	},
	mounted(){
		var vm = this;
		vm.cargar_destacados();
	},
	methods: {
		// Los datos salen de un archivo, pero se piden como se pediran despues: async y con un
		// estado de carga. En v3 cambia de donde vienen y esta pantalla no se entera.
		cargar_destacados: async function(){
			var vm = this;
			vm.cargando = true;
			const carta = await obtener_carta();
			vm.destacados = carta.productos.slice(0, 4);
			vm.cargando = false;
		}
	}
};</script>
