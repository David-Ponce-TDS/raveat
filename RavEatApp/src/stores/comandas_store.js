import { defineStore } from 'pinia';
import {
	cambiar_estado_item,
	marcar_comanda_lista,
	obtener_comandas
} from '@/services/comandas_service';

// La cocina. La API devuelve las comandas y sus items en listas separadas (una consulta por cada
// cosa, no una por comanda); aca se juntan una sola vez con `items_de`, y no en cada render.
export const use_comandas_store = defineStore('comandas', {
	state: () => ({
		comandas: [],
		items: [],
		resumen: {},
		total: 0,
		pagina: null,
		filtros: {estados: []},
		cargando: false,
		guardando: false,
		error: null
	}),
	getters: {
		hay_comandas(state){
			return state.comandas.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina && state.pagina.hay_mas);
		},
		items_por_pedido(state){
			return state.items.reduce(function(mapa, item){
				if(!mapa[item.pedido_id]) mapa[item.pedido_id] = [];
				mapa[item.pedido_id].push(item);
				return mapa;
			}, {});
		}
	},
	actions: {
		cargar: async function(filtros = null){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			if(filtros) vm.filtros = {...vm.filtros, ...filtros};
			try{
				const respuesta = await obtener_comandas({...vm.filtros, pagina: 1});
				vm.comandas = (respuesta && respuesta.comandas) || [];
				vm.items = (respuesta && respuesta.items) || [];
				vm.resumen = (respuesta && respuesta.resumen) || {};
				vm.total = (respuesta && respuesta.total) || 0;
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.comandas;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cargar la preparación.';
				vm.comandas = [];
				vm.items = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.comandas;
			vm.cargando = true;
			try{
				const respuesta = await obtener_comandas({...vm.filtros, pagina: ((vm.pagina && vm.pagina.pagina) || 1) + 1});
				const conocidas = vm.comandas.map(comanda => comanda.pedido_id);
				const nuevas = ((respuesta && respuesta.comandas) || []).filter(comanda => !conocidas.includes(comanda.pedido_id));
				vm.comandas = [...vm.comandas, ...nuevas];
				// Los items siguen a su comanda: si la comanda ya estaba, sus items tambien.
				const ids_nuevos = nuevas.map(comanda => comanda.pedido_id);
				vm.items = [...vm.items, ...((respuesta && respuesta.items) || []).filter(item => ids_nuevos.includes(item.pedido_id))];
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.comandas;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más pedidos.';
				return vm.comandas;
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
		// Despues de escribir se recarga desde la API y no se toca la lista en memoria: el estado
		// del pedido lo decide el servidor a partir de sus items, y adivinarlo aca seria copiar esa
		// regla en dos lados.
		mover_item: function(id, estado){
			var vm = this;
			return vm.escribir(() => cambiar_estado_item(id, estado), 'No se pudo mover el ítem.');
		},
		marcar_lista: function(pedido_id){
			var vm = this;
			return vm.escribir(() => marcar_comanda_lista(pedido_id), 'No se pudo marcar el pedido como listo.');
		},
		escribir: async function(accion, mensaje_error){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await accion();
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || mensaje_error;
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		items_de: function(pedido_id){
			var vm = this;
			return vm.items_por_pedido[pedido_id] || [];
		}
	}
});
