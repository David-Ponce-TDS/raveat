import { createApp } from 'vue';
import { IonicVue } from '@ionic/vue';
import App from './App.vue';
import router from './router';
import { inicializar_notificaciones_nativas } from './services/notificaciones_nativas_service';
import pinia from './stores/pinia';
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

const app = createApp(App)
	.use(IonicVue)
	.use(pinia);

// La sesion se restaura ANTES de enchufar el router, y el orden importa: instalar el router es lo
// que dispara la PRIMERA navegacion, y esa navegacion pasa por el guard una sola vez. Con el
// router puesto de entrada, el guard decidia sin saber todavia si habia usuario y dejaba pasar:
// abrir la app sin sesion terminaba dentro de Inicio, mostrando errores, en vez de en el login.
use_sesion_store().iniciar().finally(function(){
	app.use(router);
	router.isReady().then(function(){
		// El index.html ya aplico la clase del tema para evitar el flash inicial; aca el store lee
		// el mismo valor guardado para que el toggle de la pantalla arranque sincronizado con el.
		use_app_store().aplicar_tema_guardado();
		app.mount('#app');
		// Despues de montar, no antes: pedir el permiso de notificaciones sobre una pantalla en
		// blanco es la peor primera impresion posible. Si falta Firebase, el push se saltea solo.
		inicializar_notificaciones_nativas();
	});
});
