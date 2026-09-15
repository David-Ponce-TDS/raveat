import { defineStore } from 'pinia';

export const use_app_store = defineStore('app', {
	state: () => ({
		tema: 'oscuro',
		// Sin efecto visual al arrancar: se lee al crear el store, sin paso propio en main.js.
		vibracion: localStorage.getItem('raveat_vibracion') != 'no'
	}),
	getters: {
		tema_oscuro(state){
			return state.tema == 'oscuro';
		},
		vibracion_activa(state){
			return state.vibracion;
		}
	},
	actions: {
		aplicar_tema_guardado(){
			const tema_guardado = localStorage.getItem('raveat_tema');
			this.tema = tema_guardado == 'claro' ? 'claro' : 'oscuro';
			this.aplicar_tema_actual();
		},
		alternar_tema(){
			this.tema = this.tema == 'oscuro' ? 'claro' : 'oscuro';
			localStorage.setItem('raveat_tema', this.tema);
			this.aplicar_tema_actual();
		},
		aplicar_tema_actual(){
			document.documentElement.classList.toggle('ion-palette-dark', this.tema == 'oscuro');
		},
		alternar_vibracion(){
			this.vibracion = !this.vibracion;
			localStorage.setItem('raveat_vibracion', this.vibracion ? 'si' : 'no');
		}
	}
});
