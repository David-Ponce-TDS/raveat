<template>
	<ion-list class="raveat-panel">
		<ion-item v-for="fila in filas" :key="fila">
			<ion-thumbnail slot="start">
				<ion-skeleton-text :animated="true" />
			</ion-thumbnail>
			<ion-label>
				<h3><ion-skeleton-text :animated="true" style="width: 55%" /></h3>
				<p><ion-skeleton-text :animated="true" style="width: 80%" /></p>
			</ion-label>
		</ion-item>
	</ion-list>
</template>

<script>import {
	IonItem,
	IonLabel,
	IonList,
	IonSkeletonText,
	IonThumbnail
} from '@ionic/vue';

export default {
	name: 'comp_esqueleto',
	components: {
		IonItem,
		IonLabel,
		IonList,
		IonSkeletonText,
		IonThumbnail
	},
	props: {
		// Cuantas filas fantasma dibujar. La idea es que ocupen mas o menos lo mismo que
		// los datos reales, para que la pantalla no salte cuando terminan de llegar.
		filas: {
			type: Number,
			default: 4
		}
	}
};</script>
