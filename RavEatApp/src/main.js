import { createApp } from 'vue';
import { IonicVue } from '@ionic/vue';
import App from './App.vue';
import router from './router';
import pinia from './stores/pinia';
import { use_app_store } from './stores/app_store';
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

const app = createApp(App)
	.use(IonicVue)
	.use(pinia)
	.use(router);

router.isReady().then(function(){
	// El index.html ya aplico la clase del tema para evitar el flash inicial; aca el store lee
	// el mismo valor guardado para que el toggle de la pantalla arranque sincronizado con el.
	use_app_store().aplicar_tema_guardado();
	app.mount('#app');
});
