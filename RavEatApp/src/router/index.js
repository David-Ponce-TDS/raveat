import { createRouter, createWebHistory } from '@ionic/vue-router';
import { navegacion } from '@/config/navegacion';
import main_layout from '@/layouts/main_layout.vue';

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

// En v5 el rol sale del store de sesion. Hasta entonces no hay sesion, asi que devuelve null
// y el guard deja pasar todo: la estructura queda armada y solo cambia esta funcion.
function obtener_rol_activo(){
	return null;
}

// Guard preparado para roles. Lee la metadata que viene de navegacion.js, no una lista
// escrita a mano: por eso agregar una pantalla protegida no requiere tocar este archivo.
router.beforeEach(to =>{
	const roles_permitidos = to.meta.roles;
	if(!roles_permitidos || roles_permitidos.length === 0) return true;
	const rol_activo = obtener_rol_activo();
	if(!rol_activo) return true;
	if(roles_permitidos.includes(rol_activo)) return true;
	return {
		path: '/app/inicio',
		replace: true
	};
});
export default router;
