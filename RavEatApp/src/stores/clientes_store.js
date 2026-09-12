import { defineStore } from 'pinia';
import {
	actualizar_cliente,
	crear_cliente,
	eliminar_cliente,
	obtener_clientes
} from '@/services/clientes_service';

// El store acumula paginas y recuerda los filtros aplicados. Son las dos cosas que hacen que
// "ver mas" funcione: sin acumular, cada pagina pisaria la anterior; sin recordar los filtros,
// la pagina 2 vendria sin la busqueda y aparecerian resultados que no coinciden.
export const use_clientes_store = defineStore('clientes', {
	state: () => ({
		clientes: [],
		pagina: null,
		filtros: {busqueda: ''},
		cargando: false,
		guardando: false,
		error: null
	}),
	getters: {
		hay_clientes(state){
			return state.clientes.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina?.hay_mas);
		},
		total(state){
			return state.pagina?.total || 0;
		}
	},
	actions: {
		// Cargar siempre vuelve a la pagina 1: cambiar un filtro invalida lo acumulado.
		cargar: async function(filtros = null){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			if(filtros) vm.filtros = {...vm.filtros, ...filtros};
			try{
				const respuesta = await obtener_clientes({...vm.filtros, pagina: 1});
				vm.clientes = respuesta?.clientes || [];
				vm.pagina = respuesta?.pagina || null;
				return vm.clientes;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar los clientes.';
				vm.clientes = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.clientes;
			vm.cargando = true;
			try{
				const respuesta = await obtener_clientes({...vm.filtros, pagina: (vm.pagina?.pagina || 1) + 1});
				// Se filtran los ya conocidos por si entro un cliente nuevo entre dos pedidos: sin
				// esto, un corrimiento de pagina repetiria una fila.
				const conocidos = vm.clientes.map(cliente => cliente.id);
				vm.clientes = [...vm.clientes, ...(respuesta?.clientes || []).filter(cliente => !conocidos.includes(cliente.id))];
				vm.pagina = respuesta?.pagina || null;
				return vm.clientes;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más clientes.';
				return vm.clientes;
			}finally{
				vm.cargando = false;
			}
		},
		// Despues de escribir se recarga desde la API en vez de tocar la lista en memoria: el
		// servidor es el que sabe el orden, el total y si el alta paso alguna validacion.
		guardar: async function(id, datos){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				if(id) await actualizar_cliente(id, datos);
				else await crear_cliente(datos);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo guardar el cliente.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		eliminar: async function(id){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await eliminar_cliente(id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo eliminar el cliente.';
				return false;
			}finally{
				vm.guardando = false;
			}
		}
	}
});
