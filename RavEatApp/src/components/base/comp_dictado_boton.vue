<template>
	<!-- Sin reconocedor no hay boton: un microfono que no hace nada es peor que no tenerlo. El
	     campo sigue escribible siempre, que es el camino manual. -->
	<ion-button
		v-if="disponible"
		class="raveat-dictado"
		:class="{ 'raveat-dictado--escuchando': escuchando }"
		fill="clear"
		size="small"
		:color="escuchando ? 'danger' : 'medium'"
		:aria-label="escuchando ? 'Dejar de escuchar' : 'Dictar'"
		@click="alternar"
	>
		<ion-icon slot="icon-only" :icon="escuchando ? icono_detener : icono_microfono" />
	</ion-button>
</template>

<script>import { IonButton, IonIcon } from '@ionic/vue';
import { micOutline, stopCircleOutline } from 'ionicons/icons';
import { detener_dictado, dictado_disponible, dictar } from '@/services/dictado_service';
import { avisar_error } from '@/services/avisos_service';

// El boton no maneja el campo: emite el texto ya sumado al que habia, para asignarlo directo.
export default {
	name: 'comp_dictado_boton',
	components: {
		IonButton,
		IonIcon
	},
	emits: ['actualizar'],
	props: {
		valor: {
			type: String,
			default: ''
		}
	},
	data(){
		return {
			disponible: false,
			escuchando: false,
			icono_detener: stopCircleOutline,
			icono_microfono: micOutline
		};
	},
	async mounted(){
		var vm = this;
		vm.disponible = await dictado_disponible();
	},
	methods: {
		alternar: async function(){
			var vm = this;
			if(vm.escuchando){
				detener_dictado();
				return;
			}
			vm.escuchando = true;
			const resultado = await dictar();
			vm.escuchando = false;
			if(!resultado.ok){
				avisar_error(resultado.mensaje);
				return;
			}
			const texto = resultado.texto.trim();
			if(!texto) return;
			// Se suma al final: dictar dos veces no borra lo primero.
			const base = String(vm.valor || '').trim();
			vm.$emit('actualizar', base ? `${base} ${texto}` : texto);
		}
	}
};</script>
