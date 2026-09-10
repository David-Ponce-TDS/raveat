<template>
	<ion-searchbar
		class="raveat-buscador"
		:value="modelValue"
		:placeholder="placeholder"
		:debounce="0"
		@ionInput="al_escribir"
	/>
</template>

<script>import { IonSearchbar } from '@ionic/vue';

// Espera a que el usuario deje de tipear antes de salir a la API. Sin esto, escribir "hamburguesa"
// dispara once consultas y las respuestas pueden llegar desordenadas: la de "hambur" despues de la
// de "hamburguesa", dejando en pantalla resultados que no corresponden a lo que dice el campo.
export default {
	name: 'comp_buscador',
	components: {
		IonSearchbar
	},
	emits: [
		'update:modelValue',
		'buscar'
	],
	props: {
		espera: {
			type: Number,
			default: 400
		},
		modelValue: {
			type: String,
			default: ''
		},
		placeholder: {
			type: String,
			default: 'Buscar'
		}
	},
	data(){
		return {
			temporizador: null
		};
	},
	beforeUnmount(){
		var vm = this;
		if(vm.temporizador) window.clearTimeout(vm.temporizador);
	},
	methods: {
		al_escribir: function(evento){
			var vm = this;
			const texto = evento.detail.value || '';
			vm.$emit('update:modelValue', texto);
			if(vm.temporizador) window.clearTimeout(vm.temporizador);
			vm.temporizador = window.setTimeout(() => vm.$emit('buscar', texto.trim()), vm.espera);
		}
	}
};</script>
