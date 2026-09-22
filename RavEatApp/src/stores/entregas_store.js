import { defineStore } from 'pinia';
import {
	asignar_entrega,
	cambiar_estado_entrega,
	obtener_entregas
} from '@/services/entregas_service';

export const use_entregas_store = defineStore('entregas', {
	state: () => ({
		entregas: [],
		resumen: {},
		total: 0,
		pagina: null,
		filtros: {estados: [], solo_mias: false},
		cargando: false,
		guardando: false,
		error: null
	}),
	getters: {
		hay_entregas(state){
			return state.entregas.length > 0;
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
				const respuesta = await obtener_entregas({...vm.filtros, pagina: 1});
				vm.entregas = (respuesta && respuesta.entregas) || [];
				vm.resumen = (respuesta && respuesta.resumen) || {};
				vm.total = (respuesta && respuesta.total) || 0;
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.entregas;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar las entregas.';
				vm.entregas = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.entregas;
			vm.cargando = true;
			try{
				const respuesta = await obtener_entregas({...vm.filtros, pagina: ((vm.pagina && vm.pagina.pagina) || 1) + 1});
				const conocidas = vm.entregas.map(entrega => entrega.id);
				vm.entregas = [...vm.entregas, ...((respuesta && respuesta.entregas) || []).filter(entrega => !conocidas.includes(entrega.id))];
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.entregas;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más entregas.';
				return vm.entregas;
			}finally{
				vm.cargando = false;
			}
		},
		alternar_estado: function(estado){
			var vm = this;
			const activos = vm.filtros.estados || [];
			const filtrados = activos.includes(estado)
				? activos.filter(item => item != estado)
				: [...activos, estado];
			return vm.cargar({estados: filtrados});
		},
		asignar: async function(id, repartidor_id = null){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await asignar_entrega(id, repartidor_id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo asignar la entrega.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		cambiar_estado: async function(id, estado){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await cambiar_estado_entrega(id, estado);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cambiar el estado.';
				return false;
			}finally{
				vm.guardando = false;
			}
		}
	}
});
