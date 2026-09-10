import { createRouter, createWebHistory } from '@ionic/vue-router';
import inicio_page from '@/views/inicio_page.vue';

const routes = [
	{
		path: '/',
		redirect: '/inicio'
	},
	{
		path: '/inicio',
		name: 'inicio',
		component: inicio_page
	},
	{
		path: '/:pathMatch(.*)*',
		redirect: '/inicio'
	}
];
const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes
});
export default router;
