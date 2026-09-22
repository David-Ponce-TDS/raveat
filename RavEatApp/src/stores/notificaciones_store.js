import { defineStore } from 'pinia';
import {
	marcar_notificaciones_leidas,
	obtener_notificaciones
} from '@/services/notificaciones_service';

// La bandeja del usuario. El contador de no leidas viene del resumen de la API (calculado sobre
// la bandeja completa), nunca contando las filas descargadas: la campanita mentiria apenas se
// junte mas de una pagina.
export const use_notificaciones_store = defineStore('notificaciones', {
	state: () => ({
		notificaciones: [],
		resumen: null,
		pagina: null,
		cargando: false,
		error: null
	}),
	getters: {
		hay_notificaciones(state){
			return state.notificaciones.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina && state.pagina.hay_mas);
		},
		no_leidas(state){
			return (state.resumen && state.resumen.no_leidas) || 0;
		}
	},
	actions: {
		cargar: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const respuesta = await obtener_notificaciones({pagina: 1});
				vm.notificaciones = (respuesta && respuesta.notificaciones) || [];
				vm.resumen = (respuesta && respuesta.resumen) || null;
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.notificaciones;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar las notificaciones.';
				vm.notificaciones = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.notificaciones;
			vm.cargando = true;
			try{
				const respuesta = await obtener_notificaciones({pagina: ((vm.pagina && vm.pagina.pagina) || 1) + 1});
				const conocidas = vm.notificaciones.map(notificacion => notificacion.id);
				vm.notificaciones = [...vm.notificaciones, ...((respuesta && respuesta.notificaciones) || []).filter(notificacion => !conocidas.includes(notificacion.id))];
				vm.pagina = (respuesta && respuesta.pagina) || null;
				return vm.notificaciones;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más notificaciones.';
				return vm.notificaciones;
			}finally{
				vm.cargando = false;
			}
		},
		// Marcar y recargar: el servidor es el que sabe cuantas quedan sin leer.
		marcar_leida: async function(notificacion_id = null){
			var vm = this;
			try{
				await marcar_notificaciones_leidas(notificacion_id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo marcar la notificación.';
				return false;
			}
		}
	}
});
