<template>
	<div>
		<ion-list class="raveat-card">
			<slot />
		</ion-list>

		<!-- El boton solo aparece si la API dijo que hay mas. No se deduce de la cantidad
		     descargada: con un tamano de pagina de 20 y exactamente 20 resultados, contar filas
		     diria "hay mas" cuando no hay. -->
		<ion-button
			v-if="hay_mas"
			class="raveat-ver-mas"
			expand="block"
			fill="outline"
			size="small"
			:disabled="cargando"
			@click="$emit('cargar_mas')"
		>
			<ion-spinner v-if="cargando" name="crescent" />
			<span v-else>Ver más</span>
		</ion-button>

		<p v-if="total > 0" class="raveat-lista-pie">Mostrando {{ mostrados }} de {{ total }}</p>
	</div>
</template>

<script>import { IonButton, IonList, IonSpinner } from '@ionic/vue';

// La lista paginada del proyecto. Recibe si hay mas paginas y avisa cuando hay que pedirlas; no
// sabe de que son los items ni como se piden.
export default {
	name: 'comp_lista',
	components: {
		IonButton,
		IonList,
		IonSpinner
	},
	emits: [
		'cargar_mas'
	],
	props: {
		cargando: {
			type: Boolean,
			default: false
		},
		hay_mas: {
			type: Boolean,
			default: false
		},
		mostrados: {
			type: Number,
			default: 0
		},
		// El total sale de la API y no de la lista en pantalla: es la unica forma de poder decir
		// "5 de 43".
		total: {
			type: Number,
			default: 0
		}
	}
};</script>
