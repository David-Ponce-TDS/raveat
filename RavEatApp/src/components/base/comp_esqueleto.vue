<template>
	<!-- Tarjetas: la misma grilla y la misma tarjeta que dibuja la vitrina. -->
	<div v-if="variante == 'tarjetas'" class="raveat-grilla">
		<article v-for="fila in filas" :key="fila" class="raveat-producto-card">
			<div class="raveat-producto-card__imagen">
				<ion-skeleton-text :animated="true" style="width: 100%; height: 100%; margin: 0;" />
			</div>
			<div class="raveat-producto-card__info">
				<h3><ion-skeleton-text :animated="true" style="width: 70%" /></h3>
				<p><ion-skeleton-text :animated="true" style="width: 90%" /></p>
			</div>
		</article>
	</div>

	<!-- Lista: la fila con miniatura, titulo y subtitulo de las pantallas de gestion. -->
	<ion-list v-else class="raveat-panel">
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
		},
		// Con que forma dibujarlas: `lista` o `tarjetas`. El esqueleto tiene que anticipar lo que
		// viene despues; si la vitrina muestra tarjetas y el esqueleto filas, la pantalla se
		// reacomoda entera al llegar los datos y el efecto se nota como un parpadeo.
		variante: {
			type: String,
			default: 'lista'
		}
	}
};</script>
