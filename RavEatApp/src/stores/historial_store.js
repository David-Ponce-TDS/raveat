import { defineStore } from 'pinia';
import { obtener_historial } from '@/services/historial_service';

export const use_historial_store = defineStore('historial', {
	state: () => ({
		movimientos: [],
		resumen: {},
		total: 0,
		pagina: null,
		// Los filtros se recuerdan: sin esto, la pagina 2 volveria sin la busqueda aplicada.
		filtros: {tipo: '', busqueda: '', solo_mios: false},
		cargando: false,
		error: null
	}),
	getters: {
		hay_movimientos(state){
			return state.movimientos.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina && state.pagina.hay_mas);
		}
	},
	actions: {
		cargar: async function(filtros = null){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			if(filtros) vm.filtros = {...vm.filtros, ...filtros};
			try{
				const respuesta = await obtener_historial({...vm.filtros, pagina: 1});
				vm.movimientos = (respuesta && respuesta.historial) || [];
				vm.resumen = (respuesta && respuesta.resumen) || {};
				vm.total = (respuesta && respuesta.total) || 0;
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.movimientos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cargar el historial.';
				vm.movimientos = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.movimientos;
			vm.cargando = true;
			try{
				const respuesta = await obtener_historial({...vm.filtros, pagina: ((vm.pagina && vm.pagina.pagina) || 1) + 1});
				// Se descartan los ids ya conocidos: un movimiento nuevo entre dos paginas corre el
				// paginado y repetiria una fila.
				const conocidos = vm.movimientos.map(movimiento => movimiento.id);
				vm.movimientos = [...vm.movimientos, ...((respuesta && respuesta.historial) || []).filter(movimiento => !conocidos.includes(movimiento.id))];
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.movimientos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cargar más historial.';
				return vm.movimientos;
			}finally{
				vm.cargando = false;
			}
		}
	}
});
