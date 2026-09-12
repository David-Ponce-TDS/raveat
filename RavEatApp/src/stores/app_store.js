import { defineStore } from 'pinia';

export const use_app_store = defineStore('app', {
	state: () => ({
		tema: 'oscuro'
	}),
	getters: {
		tema_oscuro(state){
			return state.tema === 'oscuro';
		}
	},
	actions: {
		aplicar_tema_guardado(){
			const tema_guardado = localStorage.getItem('raveat_tema');
			this.tema = tema_guardado === 'claro' ? 'claro' : 'oscuro';
			this.aplicar_tema_actual();
		},
		alternar_tema(){
			this.tema = this.tema === 'oscuro' ? 'claro' : 'oscuro';
			localStorage.setItem('raveat_tema', this.tema);
			this.aplicar_tema_actual();
		},
		aplicar_tema_actual(){
			document.documentElement.classList.toggle('ion-palette-dark', this.tema === 'oscuro');
		}
	}
});
