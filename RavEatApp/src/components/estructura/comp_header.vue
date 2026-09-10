<template>
	<ion-header>
		<ion-toolbar>
			<ion-buttons slot="start">
				<ion-menu-button />
			</ion-buttons>
			<ion-title>{{ titulo }}</ion-title>
			<ion-buttons slot="end">
				<slot name="acciones" />
			</ion-buttons>
		</ion-toolbar>
	</ion-header>
</template>

<script>import {
	IonButtons,
	IonHeader,
	IonMenuButton,
	IonTitle,
	IonToolbar
} from '@ionic/vue';

export default {
	name: 'comp_header',
	components: {
		IonButtons,
		IonHeader,
		IonMenuButton,
		IonTitle,
		IonToolbar
	},
	props: {
		titulo: {
			type: String,
			required: true
		}
	}
};</script>
