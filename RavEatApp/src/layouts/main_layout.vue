<template>
	<ion-page>
		<comp-menu content_id="contenido_principal" />
		<ion-tabs id="contenido_principal">
			<ion-router-outlet />
			<comp-tabs />
		</ion-tabs>
	</ion-page>
</template>

<script>import {
	IonPage,
	IonRouterOutlet,
	IonTabs
} from '@ionic/vue';
import comp_menu from '@/components/estructura/comp_menu.vue';
import comp_tabs from '@/components/estructura/comp_tabs.vue';

export default {
	name: 'main_layout',
	components: {
		CompMenu: comp_menu,
		CompTabs: comp_tabs,
		IonPage,
		IonRouterOutlet,
		IonTabs
	}
};</script>
