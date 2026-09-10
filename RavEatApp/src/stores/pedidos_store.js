import { defineStore } from 'pinia';
import {
	cambiar_estado_pedido,
	cancelar_pedido,
	crear_pedido,
	obtener_pedido,
	obtener_pedidos,
	registrar_pago_pedido
} from '@/services/pedidos_service';

export const use_pedidos_store = defineStore('pedidos', {
	state: () => ({
		pedidos: [],
		pagina: null,
		// Contadores por estado, calculados por la API sobre el total filtrado. Son los botones con
		// los que se elige el filtro, asi que no pueden depender del filtro elegido.
		resumen: {},
		total: 0,
		filtros: {busqueda: '', estados: []},
		cargando: false,
		guardando: false,
		error: null,
		detalle: null,
		detalle_items: []
	}),
	getters: {
		hay_pedidos(state){
			return state.pedidos.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina?.hay_mas);
		}
	},
	actions: {
		cargar: async function(filtros = null){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			if(filtros) vm.filtros = {...vm.filtros, ...filtros};
			try{
				const respuesta = await obtener_pedidos({...vm.filtros, pagina: 1});
				vm.pedidos = respuesta?.pedidos || [];
				vm.pagina = respuesta?.pagina || null;
				vm.resumen = respuesta?.resumen || {};
				vm.total = respuesta?.total || 0;
				return vm.pedidos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar los pedidos.';
				vm.pedidos = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.pedidos;
			vm.cargando = true;
			try{
				const respuesta = await obtener_pedidos({...vm.filtros, pagina: (vm.pagina?.pagina || 1) + 1});
				const conocidos = vm.pedidos.map(pedido => pedido.id);
				vm.pedidos = [...vm.pedidos, ...(respuesta?.pedidos || []).filter(pedido => !conocidos.includes(pedido.id))];
				vm.pagina = respuesta?.pagina || null;
				return vm.pedidos;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más pedidos.';
				return vm.pedidos;
			}finally{
				vm.cargando = false;
			}
		},
		// Alternar un estado en el filtro. Sin estados elegidos la API devuelve todos.
		alternar_estado: async function(estado){
			var vm = this;
			const actuales = vm.filtros.estados || [];
			const nuevos = actuales.includes(estado)
				? actuales.filter(item => item !== estado)
				: [...actuales, estado];
			return vm.cargar({estados: nuevos});
		},
		cargar_detalle: async function(id){
			var vm = this;
			vm.error = null;
			try{
				const respuesta = await obtener_pedido(id);
				vm.detalle = respuesta?.pedido || null;
				vm.detalle_items = respuesta?.items || [];
				return vm.detalle;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cargar el pedido.';
				vm.detalle = null;
				vm.detalle_items = [];
				return null;
			}
		},
		limpiar_detalle: function(){
			var vm = this;
			vm.detalle = null;
			vm.detalle_items = [];
		},
		crear: async function(datos){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				const respuesta = await crear_pedido(datos);
				await vm.cargar();
				return respuesta;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo crear el pedido.';
				return null;
			}finally{
				vm.guardando = false;
			}
		},
		// El servidor rechaza las transiciones invalidas; la pantalla solo muestra el mensaje.
		// Duplicar la tabla de transiciones en el frontend seria tener dos reglas que se
		// desincronizan.
		cambiar_estado: async function(id, estado){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await cambiar_estado_pedido(id, estado);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cambiar el estado.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		registrar_pago: async function(id, medio_pago, propina_importe = 0){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await registrar_pago_pedido(id, medio_pago, propina_importe);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo registrar el pago.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		cancelar: async function(id){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await cancelar_pedido(id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cancelar el pedido.';
				return false;
			}finally{
				vm.guardando = false;
			}
		}
	}
});
