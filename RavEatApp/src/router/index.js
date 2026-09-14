import { createRouter, createWebHistory } from '@ionic/vue-router';
import { navegacion, TODOS } from '@/config/navegacion';
import main_layout from '@/layouts/main_layout.vue';
import { use_sesion_store } from '@/stores/sesion_store';

// Las rutas ya no se escriben: se derivan de la configuracion. Agregar una pantalla es
// agregar un objeto en navegacion.js, y el router, el menu y los tabs se enteran solos.
const rutas_app = navegacion.map(item => ({
	path: item.ruta.replace('/app/', ''),
	name: item.id,
	component: item.componente,
	meta: {
		roles: item.roles
	}
}));

const routes = [
	{
		path: '/',
		redirect: '/app/inicio'
	},
	{
		// El login queda FUERA de /app a proposito: no tiene menu ni tabs, porque todavia no se
		// sabe quien es el usuario ni que le corresponde ver.
		path: '/login',
		name: 'login',
		component: () => import('@/views/login_page.vue'),
		meta: {
			publica: true
		}
	},
	{
		path: '/app',
		component: main_layout,
		children: [
			{
				path: '',
				redirect: '/app/inicio'
			},
			...rutas_app
		]
	},
	{
		path: '/:pathMatch(.*)*',
		redirect: '/app/inicio'
	}
];
const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes
});

// Desde v5 el rol sale del store de sesion, que ya lo entrega como id de navegacion
// ('administrador'). La estructura del guard estaba armada desde v2 y no se toco.
// Guard real. Lee la metadata que viene de navegacion.js, no una lista escrita a mano: por eso
// agregar una pantalla protegida no requiere tocar este archivo.
router.beforeEach(to =>{
	const sesion = use_sesion_store();

	// Mientras se restaura la sesion guardada no se puede decidir nada: rebotar al login aca haria
	// que la app mande al login en cada arranque, incluso teniendo sesion valida.
	if(sesion.restaurando) return true;

	if(to.meta.publica){
		// Si ya entro, el login no tiene nada que ofrecerle.
		return sesion.autenticado ? {path: '/app/inicio', replace: true} : true;
	}

	if(!sesion.autenticado) return {path: '/login', replace: true};

	const roles_permitidos = to.meta.roles;
	// Misma regla de lectura que navegacion.js: [TODOS] abre la ruta a cualquier autenticado,
	// tenga rol o no (es la marca que lleva Inicio), y la lista vacia no abre a nadie.
	if(!roles_permitidos) return true;
	if(roles_permitidos.includes(TODOS)) return true;

	if(sesion.rol_activo && roles_permitidos.includes(sesion.rol_activo)) return true;

	// Inicio es el refugio del guard: si el destino ya es Inicio, se deja pasar. Asi el guard
	// nunca redirige una ruta a si misma, sea cual sea la configuracion.
	if(to.path === '/app/inicio') return true;

	// Autenticado pero sin permiso: vuelve a Inicio, que es lo unico que todos pueden ver. No se
	// lo manda al login, porque su sesion es valida — lo que le falta es autorizacion.
	return {
		path: '/app/inicio',
		replace: true
	};
});
export default router;
