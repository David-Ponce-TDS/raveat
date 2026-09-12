import {
	homeOutline,
	peopleOutline,
	peopleCircleOutline,
	personCircleOutline,
	pricetagsOutline,
	receiptOutline
} from 'ionicons/icons';

// Fuente de verdad unica de la navegacion. De aca salen cuatro cosas que antes se escribian
// por separado y se desincronizaban:
//   - las rutas del router
//   - el guard
//   - los items del menu lateral
//   - los tabs de abajo
// Agregar una pantalla es agregar un objeto aca.

// "Cualquier usuario autenticado, tenga rol o no". Es un valor explicito y no una lista vacia:
//   - []      -> nadie, sin excepciones
//   - [TODOS] -> cualquiera que haya iniciado sesion
// Vacio se leia igual de bien como "todos" que como "nadie", y esa ambiguedad ya dejo una
// pantalla de administracion visible en los tabs de todo el mundo.
export const TODOS = 'todos';

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
		// Cualquier autenticado, incluso sin rol: es lo que le deja ver la vitrina mientras
		// espera que un admin lo habilite.
		roles: [TODOS],
		menu_roles: [TODOS],
		tab_roles: [TODOS],
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
		roles: [TODOS],
		menu_roles: [TODOS],
		tab_roles: [TODOS],
		grupo_menu: 'configuracion',
		orden: 100,
		componente: () => import('@/views/perfil_page.vue')
	},
	{
		id: 'usuarios',
		titulo: 'Usuarios',
		ruta: '/app/usuarios',
		icono: peopleCircleOutline,
		roles: ['administrador'],
		menu_roles: ['administrador'],
		// A proposito NO va en los tabs: los tabs son para lo que se usa todo el dia, y
		// administrar usuarios no lo es. Es el caso que muestra para que sirve tener tres listas
		// separadas en vez de una.
		tab_roles: [],
		grupo_menu: 'configuracion',
		orden: 90,
		componente: () => import('@/views/usuarios_page.vue')
	}
];

// Desde v5 el filtrado es real. La regla de lectura es una sola y aplica a las tres listas:
//
//   [TODOS]            -> cualquier usuario autenticado, tenga rol o no
//   ['admin', ...]     -> solo esos roles
//   []                 -> nadie
//
// Hasta v4 estas funciones devolvian TODO cuando no habia rol, porque no habia sesion. Mantener
// ese atajo ahora seria un agujero: un usuario recien creado, todavia sin habilitar, veria el
// menu completo.
function filtrar(lista_de_roles, rol){
	return navegacion
		.filter(item =>{
			const permitidos = item[lista_de_roles] || [];
			if(permitidos.includes(TODOS)) return true;
			return Boolean(rol) && permitidos.includes(rol);
		})
		.sort((item_a, item_b) => item_a.orden - item_b.orden);
}

export function obtener_navegacion_rol(rol){
	return filtrar('roles', rol);
}

export function obtener_menu_rol(rol){
	return filtrar('menu_roles', rol);
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
	return filtrar('tab_roles', rol).slice(0, MAXIMO_TABS);
}
