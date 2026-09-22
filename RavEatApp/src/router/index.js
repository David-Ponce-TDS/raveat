import { createRouter, createWebHistory } from '@ionic/vue-router';
import { navegacion, TODOS } from '@/config/navegacion';
import main_layout from '@/layouts/main_layout.vue';
import { use_notificaciones_store } from '@/stores/notificaciones_store';
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
		// La puerta de la app es la vitrina publica, no el login: el que llega sin cuenta tiene
		// que ver la carta. Si ya entro, el guard lo manda a /app/inicio.
		path: '/',
		redirect: '/vitrina'
	},
	{
		// Fuera de /app igual que el login: sin sesion no hay menu ni tabs que dibujar.
		path: '/vitrina',
		name: 'vitrina',
		component: () => import('@/views/vitrina_page.vue'),
		meta: {
			publica: true
		}
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
		// Una ruta que no existe cae en la vitrina, y desde ahi el guard decide: al de adentro lo
		// manda a /app/inicio y al anonimo lo deja ver la carta, en vez de rebotarlo al login.
		path: '/:pathMatch(.*)*',
		redirect: '/vitrina'
	}
];
const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes
});

// Desde v5 el rol sale del store de sesion. Es el unico cambio que hizo falta: la estructura del
// guard estaba armada desde v2 y no se toco.
// La API habla en codigos (`ADMIN`) y la navegacion en ids (`administrador`); la traduccion la
// hace `sesion_store` con `roles.js`, en el borde con el servidor. Aca ya llega el id, igual que
// al menu y a los tabs: una sola forma del rol para toda la app.
function obtener_rol_activo(){
	const sesion = use_sesion_store();
	return sesion.rol_activo;
}

// El refugio del guard: la pantalla que ve el que no puede ver otra cosa.
const RUTA_INICIO = '/app/inicio';

// Guard real. Lee la metadata que viene de navegacion.js, no una lista escrita a mano: por eso
// agregar una pantalla protegida no requiere tocar este archivo.
router.beforeEach(to =>{
	const sesion = use_sesion_store();

	// Mientras se restaura la sesion guardada no se puede decidir nada: rebotar al login aca haria
	// que la app mande al login en cada arranque, incluso teniendo sesion valida.
	if(sesion.restaurando) return true;

	if(to.meta.publica){
		// Las publicas son las de afuera: la vitrina y el login. Al que ya entro no le sirven —
		// tiene la vitrina de adentro, con menu y tabs.
		return sesion.autenticado ? {path: '/app/inicio', replace: true} : true;
	}

	if(!sesion.autenticado) return {path: '/login', replace: true};

	const roles_permitidos = to.meta.roles;
	// Lista vacia = NADIE. La marca [TODOS] es la que abre a cualquier autenticado, tenga rol o
	// no: es la que lleva Inicio. Sin este chequeo el guard rechazaba Inicio y lo redirigia a
	// Inicio — un bucle que dejaba la app clavada en el login sin ningun error a la vista.
	if(!roles_permitidos) return true;
	if(roles_permitidos.includes(TODOS)) return true;

	const rol_activo = obtener_rol_activo();
	if(rol_activo && roles_permitidos.includes(rol_activo)) return true;

	// Autenticado pero sin permiso: vuelve a Inicio, que es lo unico que todos pueden ver. No se
	// lo manda al login, porque su sesion es valida — lo que le falta es autorizacion.
	// El destino se compara antes de redirigir: mandar una ruta a si misma es el bucle de arriba,
	// y el guard tiene que ser incapaz de armarlo aunque la configuracion cambie.
	if(to.path == RUTA_INICIO) return true;
	return {
		path: RUTA_INICIO,
		replace: true
	};
});

// La campanita se refresca en cada navegacion, sin timers. Antes cargaba el header al montarse,
// pero Ionic CACHEA las paginas del outlet: al volver a una ya visitada el header no se vuelve a
// montar y el contador quedaba viejo. El afterEach corre siempre, este la pagina cacheada o no.
// Sin rol no hay campanita ni bandeja que pedir.
router.afterEach(() =>{
	const sesion = use_sesion_store();
	if(!sesion.autenticado || !sesion.rol_activo) return;
	use_notificaciones_store().cargar();
});
export default router;
