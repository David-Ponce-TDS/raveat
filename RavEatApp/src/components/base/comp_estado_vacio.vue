<template>
	<div class="raveat-estado-vacio">
		<ion-icon :icon="icono" class="raveat-estado-vacio__icono" />
		<h3 class="raveat-estado-vacio__titulo">{{ titulo }}</h3>
		<p v-if="mensaje" class="raveat-estado-vacio__mensaje">{{ mensaje }}</p>
		<slot name="accion" />
	</div>
</template>

<script>import { IonIcon } from '@ionic/vue';
import { fileTrayOutline } from 'ionicons/icons';

export default {
	name: 'comp_estado_vacio',
	components: {
		IonIcon
	},
	props: {
		icono: {
			type: String,
			default: fileTrayOutline
		},
		mensaje: {
			type: String,
			default: ''
		},
		titulo: {
			type: String,
			required: true
		}
	}
};</script>
