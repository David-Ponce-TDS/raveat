<template>
	<div class="raveat-aviso" :class="`raveat-aviso--${tono}`" role="status">
		<ion-icon class="raveat-aviso__icono" :icon="icono" aria-hidden="true" />
		<div class="raveat-aviso__texto">
			<strong v-if="titulo">{{ titulo }}</strong>
			<span>{{ mensaje }}</span>
		</div>
	</div>
</template>

<script>import { IonIcon } from '@ionic/vue';
import {
	alertCircleOutline,
	checkmarkCircleOutline,
	informationCircleOutline,
	warningOutline
} from 'ionicons/icons';

// El aviso con forma propia: barra de color, icono y texto. Reemplaza a los ion-note sueltos,
// que se leian como un parrafo gris mas y se perdian justo cuando explicaban por que un boton
// estaba apagado. Cuatro tonos y ninguno inventa color: salen de la paleta de Ionic.
const ICONOS = {
	info: informationCircleOutline,
	pendiente: warningOutline,
	error: alertCircleOutline,
	exito: checkmarkCircleOutline
};

export default {
	name: 'comp_aviso',
	components: {
		IonIcon
	},
	props: {
		mensaje: {
			type: String,
			default: ''
		},
		titulo: {
			type: String,
			default: ''
		},
		// info, pendiente, error o exito.
		tono: {
			type: String,
			default: 'info'
		}
	},
	computed: {
		icono(){
			var vm = this;
			return ICONOS[vm.tono] || ICONOS.info;
		}
	}
};</script>
