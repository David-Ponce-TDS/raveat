import { createApp } from 'vue';
import { IonicVue } from '@ionic/vue';
import App from './App.vue';
import router from './router';
import pinia from './stores/pinia';
import { al_expirar } from './services/token_service';
import { use_app_store } from './stores/app_store';
import { use_sesion_store } from './stores/sesion_store';
import '@ionic/vue/css/core.css';
import '@ionic/vue/css/normalize.css';
import '@ionic/vue/css/structure.css';
import '@ionic/vue/css/typography.css';
import '@ionic/vue/css/padding.css';
import '@ionic/vue/css/float-elements.css';
import '@ionic/vue/css/text-alignment.css';
import '@ionic/vue/css/text-transformation.css';
import '@ionic/vue/css/flex-utils.css';
import '@ionic/vue/css/display.css';
import '@ionic/vue/css/palettes/dark.class.css';
import './theme/global.css';
import './theme/componentes.css';

// El router se instala mas abajo y no aca: instalarlo dispara su primera navegacion en el acto,
// y el guard necesita saber si hay sesion antes de decidir.
const app = createApp(App)
	.use(IonicVue)
	.use(pinia);

// La sesion se puede caer sola: una baja, un cambio de rol o un refresh vencido. ajax_service
// avisa por este callback, y se engancha aca porque hacen falta las dos mitades (el store y el
// router) y ninguna de las dos puede importar a la otra sin ciclo. El guard no alcanza: solo corre
// al navegar, y el usuario puede estar parado en una pantalla sin tocar nada.
const sesion_store = use_sesion_store(pinia);
al_expirar(async function(){
	await sesion_store.limpiar();
	router.replace('/login');
});

// El token guardado se lee ANTES de instalar el router y de montar: el guard no puede decidir
// nada sin saber si hay sesion. Leerlo no es entrar: la app siempre arranca en el login, y ahi
// la huella desbloquea la sesion que ya existe en vez de volver a pedir email y contrasena.
sesion_store.preparar().finally(function(){
	app.use(router);
	router.isReady().then(function(){
		// El index.html ya aplico la clase del tema para evitar el flash inicial; aca el store lee
		// el mismo valor guardado para que el toggle de la pantalla arranque sincronizado con el.
		use_app_store().aplicar_tema_guardado();
		app.mount('#app');
	});
});
