<template>
	<div class="raveat-estado-vacio">
		<ion-icon :icon="icono" class="raveat-estado-vacio__icono raveat-estado-vacio__icono--error" />
		<h3 class="raveat-estado-vacio__titulo">{{ titulo }}</h3>
		<p class="raveat-estado-vacio__mensaje">{{ mensaje }}</p>
		<ion-button size="small" fill="outline" @click="$emit('reintentar')">Reintentar</ion-button>
	</div>
</template>

<script>import { IonButton, IonIcon } from '@ionic/vue';
import { cloudOfflineOutline } from 'ionicons/icons';

// Una lista que no cargo no esta vacia: son dos situaciones distintas y decirle "no hay productos"
// a alguien que tiene la API apagada lo manda a buscar el problema donde no esta.
export default {
	name: 'comp_estado_error',
	components: {
		IonButton,
		IonIcon
	},
	emits: [
		'reintentar'
	],
	props: {
		icono: {
			type: String,
			default: cloudOfflineOutline
		},
		mensaje: {
			type: String,
			required: true
		},
		titulo: {
			type: String,
			default: 'No se pudo cargar'
		}
	}
};</script>
