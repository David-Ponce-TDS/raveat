import { defineStore } from 'pinia';
import {
	cambiar_estado_pedido,
	cancelar_pedido,
	crear_pedido,
	obtener_pedido,
	obtener_pedidos,
	obtener_qr_pedido,
	registrar_pago_pedido
} from '@/services/pedidos_service';
import { use_offline_store } from '@/stores/offline_store';

// El cambio de estado que espera en la cola se dibuja encima de lo que dice la API: el pedido
// muestra el estado nuevo, con la marca de que todavia no se envio.
function con_cambios_sin_enviar(pedido){
	const cambios = use_offline_store().cola.filter(cambio => cambio.pedido_id == pedido.id);
	if(cambios.length == 0) return pedido;
	return {...pedido, estado: cambios[cambios.length - 1].estado, sin_enviar: true};
}

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
		detalle_items: [],
		// La direccion publica del pedido, la misma que lleva el QR: compartirla es mandar un
		// enlace que cualquiera abre, con la app o sin ella.
		detalle_enlace: '',
		// URL temporal del PNG del QR, creada con createObjectURL. Es del navegador, no del
		// servidor: hay que devolverla al cerrar la ficha o el Blob queda en memoria.
		detalle_qr: ''
	}),
	getters: {
		hay_pedidos(state){
			return state.pedidos.length > 0;
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
				const respuesta = await obtener_pedidos({...vm.filtros, pagina: 1});
				vm.pedidos = ((respuesta && respuesta.pedidos) || []).map(con_cambios_sin_enviar);
				vm.pagina = (respuesta && respuesta.pagina) || null;
				vm.resumen = (respuesta && respuesta.resumen) || {};
				vm.total = (respuesta && respuesta.total) || 0;
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
				const respuesta = await obtener_pedidos({...vm.filtros, pagina: ((vm.pagina && vm.pagina.pagina) || 1) + 1});
				const conocidos = vm.pedidos.map(pedido => pedido.id);
				vm.pedidos = [...vm.pedidos, ...((respuesta && respuesta.pedidos) || []).filter(pedido => !conocidos.includes(pedido.id)).map(con_cambios_sin_enviar)];
				vm.pagina = (respuesta && respuesta.pagina) || null;
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
				? actuales.filter(item => item != estado)
				: [...actuales, estado];
			return vm.cargar({estados: nuevos});
		},
		cargar_detalle: async function(id){
			var vm = this;
			vm.error = null;
			try{
				const respuesta = await obtener_pedido(id);
				vm.detalle = respuesta && respuesta.pedido ? con_cambios_sin_enviar(respuesta.pedido) : null;
				vm.detalle_items = (respuesta && respuesta.items) || [];
				vm.detalle_enlace = (respuesta && respuesta.enlace_publico) || '';
				return vm.detalle;
			}catch(error){
				// La ficha se queda con lo que trajo la lista: sin red, y sin copia de este pedido,
				// igual se lee y se le puede cambiar el estado.
				vm.error = error.mensaje || 'No se pudo cargar el pedido.';
				vm.detalle_items = [];
				return null;
			}
		},
		cargar_qr: async function(id){
			var vm = this;
			vm.soltar_qr();
			try{
				const blob = await obtener_qr_pedido(id);
				vm.detalle_qr = window.URL.createObjectURL(blob);
				return vm.detalle_qr;
			}catch(error){
				// El QR es un extra de la ficha: que falle no puede romper el resto del detalle,
				// asi que no toca vm.error. Queda el spinner y el pedido se lee igual.
				vm.detalle_qr = '';
				return '';
			}
		},
		limpiar_detalle: function(){
			var vm = this;
			vm.detalle = null;
			vm.detalle_items = [];
			vm.detalle_enlace = '';
			vm.soltar_qr();
		},
		soltar_qr: function(){
			var vm = this;
			if(!vm.detalle_qr) return;
			window.URL.revokeObjectURL(vm.detalle_qr);
			vm.detalle_qr = '';
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
			const offline = use_offline_store();
			vm.error = null;
			// Sin red el cambio no se pierde: espera en la cola y el pedido se dibuja con el
			// estado nuevo.
			if(offline.sin_conexion){
				offline.encolar(id, estado);
				vm.pedidos = vm.pedidos.map(con_cambios_sin_enviar);
				if(vm.detalle) vm.detalle = con_cambios_sin_enviar(vm.detalle);
				return 'pendiente';
			}
			vm.guardando = true;
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
