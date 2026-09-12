import { defineStore } from 'pinia';
import {
	actualizar_producto,
	crear_producto,
	eliminar_producto,
	obtener_listado_productos,
	obtener_resumen_productos
} from '@/services/productos_service';

// Dos listas distintas conviviendo en un mismo store, y no es duplicacion:
//
//   vitrina  -> la carta entera, sin paginar, para mostrarla al comensal (Inicio)
//   catalogo -> paginado y filtrable, incluye los no disponibles, para gestionarla (Productos)
//
// Si compartieran una sola lista, abrir el catalogo con un filtro dejaria la vitrina mostrando
// media carta. Son dos vistas del mismo dato con reglas distintas.
export const use_productos_store = defineStore('productos', {
	state: () => ({
		resumen: null,
		categorias: [],
		productos: [],
		cargando: false,
		error: null,

		catalogo_productos: [],
		catalogo_resumen: null,
		catalogo_pagina: null,
		catalogo_filtros: {busqueda: '', categoria_id: null, disponible: null},
		catalogo_cargando: false,
		guardando: false
	}),
	getters: {
		hay_productos(state){
			return state.productos.length > 0;
		},
		catalogo_hay_productos(state){
			return state.catalogo_productos.length > 0;
		},
		catalogo_hay_mas(state){
			return Boolean(state.catalogo_pagina?.hay_mas);
		}
	},
	actions: {
		cargar_productos: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const respuesta = await obtener_resumen_productos();
				vm.resumen = respuesta?.resumen || null;
				vm.categorias = respuesta?.categorias || [];
				vm.productos = respuesta?.productos || [];
				return vm.productos;
			}catch(error){
				// El store se queda con el mensaje ya normalizado por ajax_service; la pantalla no
				// tiene que saber nada de codigos HTTP.
				vm.error = error.mensaje || 'No se pudo cargar el catálogo.';
				vm.productos = [];
				vm.categorias = [];
				vm.resumen = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_catalogo: async function(filtros = null){
			var vm = this;
			vm.catalogo_cargando = true;
			vm.error = null;
			if(filtros) vm.catalogo_filtros = {...vm.catalogo_filtros, ...filtros};
			try{
				const respuesta = await obtener_listado_productos({...vm.catalogo_filtros, pagina: 1});
				vm.catalogo_resumen = respuesta?.resumen || null;
				vm.catalogo_productos = respuesta?.productos || [];
				vm.catalogo_pagina = respuesta?.pagina || null;
				return vm.catalogo_productos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cargar el catálogo.';
				vm.catalogo_productos = [];
				vm.catalogo_pagina = null;
				return [];
			}finally{
				vm.catalogo_cargando = false;
			}
		},
		cargar_mas_catalogo: async function(){
			var vm = this;
			if(vm.catalogo_cargando || !vm.catalogo_hay_mas) return vm.catalogo_productos;
			vm.catalogo_cargando = true;
			try{
				const respuesta = await obtener_listado_productos({...vm.catalogo_filtros, pagina: (vm.catalogo_pagina?.pagina || 1) + 1});
				const conocidos = vm.catalogo_productos.map(producto => producto.id);
				vm.catalogo_productos = [...vm.catalogo_productos, ...(respuesta?.productos || []).filter(producto => !conocidos.includes(producto.id))];
				vm.catalogo_pagina = respuesta?.pagina || null;
				return vm.catalogo_productos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más productos.';
				return vm.catalogo_productos;
			}finally{
				vm.catalogo_cargando = false;
			}
		},
		// Al escribir se recargan las DOS listas: el catalogo porque es desde donde se edito, y la
		// vitrina porque quedo vieja. Es literalmente la idea del modulo — la pantalla es una vista
		// parcial y vieja de los datos, y despues de escribir hay que volver a preguntar.
		guardar_producto: async function(id, datos){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				if(id) await actualizar_producto(id, datos);
				else await crear_producto(datos);
				await vm.cargar_catalogo();
				await vm.cargar_productos();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo guardar el producto.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		eliminar_producto_catalogo: async function(id){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await eliminar_producto(id);
				await vm.cargar_catalogo();
				await vm.cargar_productos();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo eliminar el producto.';
				return false;
			}finally{
				vm.guardando = false;
			}
		}
	}
});
