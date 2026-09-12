import {
	homeOutline,
	peopleOutline,
	personCircleOutline,
	pricetagsOutline,
	receiptOutline
} from 'ionicons/icons';

// Fuente de verdad unica de la navegacion. De este archivo salen cuatro cosas que antes
// se escribian por separado y se desincronizaban: las rutas del router, el guard, los
// items del menu lateral y los tabs de abajo. Agregar una pantalla es agregar un objeto aca.
const todos_roles = [
	'administrador',
	'vendedor',
	'proceso',
	'caja',
	'delivery'
];

// Dos grupos: el operativo va sin encabezado (es el cuerpo del menu) y el de ajustes si lo lleva.
export const grupos_menu = [
	{
		id: 'operacion',
		titulo: '',
		orden: 10
	},
	{
		id: 'configuracion',
		titulo: 'Configuración',
		orden: 20
	}
];

// Cada item declara TRES listas de roles distintas, y no son lo mismo:
//   roles       -> quien puede ENTRAR a la ruta (lo usa el guard)
//   menu_roles  -> quien la ve en el menu lateral
//   tab_roles   -> quien la ve en la barra de tabs
// Una pantalla de detalle, por ejemplo, es accesible pero no aparece en el menu ni en tabs.
export const navegacion = [
	{
		id: 'inicio',
		titulo: 'Inicio',
		ruta: '/app/inicio',
		icono: homeOutline,
		roles: todos_roles,
		menu_roles: todos_roles,
		tab_roles: todos_roles,
		grupo_menu: 'operacion',
		orden: 10,
		componente: () => import('@/views/inicio_page.vue')
	},
	{
		id: 'productos',
		titulo: 'Productos',
		ruta: '/app/productos',
		icono: pricetagsOutline,
		roles: ['administrador', 'vendedor'],
		menu_roles: ['administrador', 'vendedor'],
		tab_roles: ['administrador', 'vendedor'],
		grupo_menu: 'operacion',
		orden: 20,
		componente: () => import('@/views/productos_page.vue')
	},
	{
		id: 'clientes',
		titulo: 'Clientes',
		ruta: '/app/clientes',
		icono: peopleOutline,
		roles: ['administrador', 'vendedor'],
		menu_roles: ['administrador', 'vendedor'],
		tab_roles: ['administrador', 'vendedor'],
		grupo_menu: 'operacion',
		orden: 30,
		componente: () => import('@/views/clientes_page.vue')
	},
	{
		id: 'pedidos',
		titulo: 'Pedidos',
		ruta: '/app/pedidos',
		icono: receiptOutline,
		roles: ['administrador', 'vendedor', 'caja'],
		menu_roles: ['administrador', 'vendedor', 'caja'],
		tab_roles: ['administrador', 'vendedor', 'caja'],
		grupo_menu: 'operacion',
		orden: 40,
		componente: () => import('@/views/pedidos_page.vue')
	},
	{
		id: 'perfil',
		titulo: 'Mi cuenta',
		ruta: '/app/perfil',
		icono: personCircleOutline,
		roles: todos_roles,
		menu_roles: todos_roles,
		tab_roles: todos_roles,
		grupo_menu: 'configuracion',
		orden: 100,
		componente: () => import('@/views/perfil_page.vue')
	}
];

// Sin rol todavia no hay filtrado real: la sesion llega en v5. Hasta entonces estas tres
// funciones devuelven todo, y el dia que exista un rol activo empiezan a recortar solas.
export function obtener_navegacion_rol(rol){
	if(!rol) return [...navegacion].sort((item_a, item_b) => item_a.orden - item_b.orden);
	return navegacion
		.filter(item => item.roles.includes(rol))
		.sort((item_a, item_b) => item_a.orden - item_b.orden);
}

export function obtener_menu_rol(rol){
	if(!rol) return [...navegacion].sort((item_a, item_b) => item_a.orden - item_b.orden);
	return navegacion
		.filter(item => item.menu_roles.includes(rol))
		.sort((item_a, item_b) => item_a.orden - item_b.orden);
}

export function obtener_grupos_menu_rol(rol){
	const items = obtener_menu_rol(rol);
	return grupos_menu
		.map(grupo => ({
			...grupo,
			items: items.filter(item => item.grupo_menu === grupo.id)
		}))
		.filter(grupo => grupo.items.length > 0)
		.sort((grupo_a, grupo_b) => grupo_a.orden - grupo_b.orden);
}

// Ionic no maneja bien mas de 5 tabs: a partir de ahi la barra se apreta y deja de leerse.
// El tope se corta aca y no en el template, para que la regla viva con la configuracion.
export const MAXIMO_TABS = 5;

export function obtener_tabs_rol(rol){
	const items = !rol
		? [...navegacion].sort((item_a, item_b) => item_a.orden - item_b.orden)
		: navegacion
			.filter(item => item.tab_roles.includes(rol))
			.sort((item_a, item_b) => item_a.orden - item_b.orden);
	return items.slice(0, MAXIMO_TABS);
}
