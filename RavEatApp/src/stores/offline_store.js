import { defineStore } from 'pinia';
import { Network } from '@capacitor/network';
import { avisar_error } from '@/services/avisos_service';
import { cambiar_estado_pedido } from '@/services/pedidos_service';
import { obtener_token } from '@/services/token_service';
import { use_pedidos_store } from '@/stores/pedidos_store';

// La cola vive en localStorage: el cambio que espera la red sobrevive al cierre de la app.
const clave_cola = 'raveat_cola_offline';

export const use_offline_store = defineStore('offline', {
	state: () => ({
		en_linea: true,
		// Los cambios de estado hechos sin red, en el orden en que se hicieron.
		cola: JSON.parse(localStorage.getItem(clave_cola) || '[]'),
		sincronizando: false
	}),
	getters: {
		sin_conexion(state){
			return !state.en_linea;
		}
	},
	actions: {
		// En el telefono el estado de la red lo informa el sistema, no navigator.onLine: el plugin
		// avisa el cambio (wifi que se corta, datos que vuelven) sin que nadie pregunte.
		iniciar: async function(){
			var vm = this;
			Network.addListener('networkStatusChange', function(estado){
				vm.en_linea = estado.connected;
				// La cola se envia apenas vuelve la red, no cuando el usuario se acuerda.
				if(estado.connected) vm.sincronizar();
			});
			const estado = await Network.getStatus();
			vm.en_linea = estado.connected;
			// Lo que quedo en la cola de la sesion anterior sale al arrancar.
			if(vm.en_linea) vm.sincronizar();
		},
		guardar_cola: function(cola){
			var vm = this;
			vm.cola = cola;
			localStorage.setItem(clave_cola, JSON.stringify(cola));
		},
		encolar: function(pedido_id, estado){
			var vm = this;
			vm.guardar_cola([...vm.cola, {id: Date.now(), pedido_id, estado}]);
		},
		sincronizar: async function(){
			var vm = this;
			// Sin sesion no se envia: la solicitud saldria sin token y el 401 mandaria al login.
			if(vm.sincronizando || vm.cola.length == 0 || !obtener_token()) return;
			vm.sincronizando = true;
			const en_cola = vm.cola.length;
			const rechazados = [];
			for(const cambio of [...vm.cola]){
				try{
					await cambiar_estado_pedido(cambio.pedido_id, cambio.estado);
				}catch(error){
					// Sin red, sesion vencida o servidor caido: sigue en la cola y sale despues.
					if(!error.estado_http || error.estado_http == 401 || error.estado_http >= 500) continue;
					// Un 400 o un 409 no mejora reintentando: sale de la cola y se avisa. Es lo que
					// pasa cuando otro usuario movio el pedido mientras este telefono estaba sin red.
					rechazados.push(error.mensaje);
				}
				vm.guardar_cola(vm.cola.filter(item => item.id != cambio.id));
			}
			vm.sincronizando = false;
			if(rechazados.length > 0) avisar_error(`La API no aceptó ${rechazados.length} de los cambios guardados: ${rechazados[0]}`);
			// Algo salio de la cola: llego a la API, o la API no lo acepto. La lista se pide una
			// sola vez, al terminar el envio, y muestra lo que de verdad quedo.
			if(vm.cola.length < en_cola) await use_pedidos_store().cargar();
		}
	}
});
