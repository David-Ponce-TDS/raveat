<template>
	<!-- Sin telefono no se dibuja nada: un boton que no puede hacer su trabajo no se ofrece. -->
	<template v-if="telefono_normalizado">
		<ion-button fill="outline" size="small" @click="abrir(`tel:${telefono_normalizado}`)">
			<ion-icon slot="start" :icon="icono_llamar" />
			Llamar
		</ion-button>
		<ion-button fill="outline" size="small" color="success" @click="abrir(`https://wa.me/${telefono_normalizado}`)">
			<ion-icon slot="start" :icon="icono_whatsapp" />
			WhatsApp
		</ion-button>
	</template>
</template>

<script>import {
	IonButton,
	IonIcon
} from '@ionic/vue';
import { callOutline, logoWhatsapp } from 'ionicons/icons';

// Llamar y WhatsApp con un telefono, para cualquier ficha que tenga a quien contactar. Los
// botones salen sueltos (sin contenedor): se acomodan en la botonera de quien los usa.
export default {
	name: 'comp_contacto_botones',
	components: {
		IonButton,
		IonIcon
	},
	props: {
		telefono: {
			type: [String, Number],
			default: ''
		}
	},
	data(){
		return {
			icono_llamar: callOutline,
			icono_whatsapp: logoWhatsapp
		};
	},
	computed: {
		// wa.me quiere solo digitos, sin +, espacios ni guiones; tel: tambien los acepta asi.
		telefono_normalizado(){
			var vm = this;
			return String(vm.telefono || '').replace(/\D/g, '');
		}
	},
	methods: {
		abrir: function(url){
			window.open(url, '_blank');
		}
	}
};</script>
