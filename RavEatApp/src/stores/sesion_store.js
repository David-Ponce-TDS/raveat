import { defineStore } from 'pinia';
import { obtener_rol_por_codigo } from '@/config/roles';
import { cerrar_sesion, iniciar_sesion, obtener_mi_usuario } from '@/services/sesion_service';
import {
	borrar_sesion,
	guardar_sesion,
	obtener_refresh_token,
	restaurar_sesion
} from '@/services/token_service';

// La sesion del usuario. Es el store que el guard consulta para decidir si deja pasar.
//
// `rol_activo` es lo unico que el resto de la app necesita saber, y puede ser null de dos maneras
// distintas que NO son lo mismo:
//   - sin usuario  -> no inicio sesion; va al login
//   - con usuario y sin rol -> entro, pero un admin todavia no lo habilito
export const use_sesion_store = defineStore('sesion', {
	state: () => ({
		usuario: null,
		cargando: false,
		// Arranca en true: hasta que no se intento restaurar la sesion guardada no se sabe si hay
		// que mandar al login. Sin esto, el guard rebota al usuario en cada arranque de la app.
		restaurando: true,
		error: null
	}),
	getters: {
		autenticado(state){
			return Boolean(state.usuario);
		},
		// Se entrega ya traducido al id de navegacion ('administrador'), no al codigo de la API
		// ('ADMIN'): el guard, el menu y los tabs comparan contra navegacion.js, y la traduccion
		// vive en un solo lugar.
		rol_activo(state){
			const rol = obtener_rol_por_codigo(state.usuario ? state.usuario.rol_codigo : null);
			return rol ? rol.id : null;
		},
		// Autenticado pero sin rol: entro y no puede hacer nada hasta que lo habiliten. Mira el
		// codigo crudo a proposito: un rol que la app todavia no sabe traducir sigue siendo un rol.
		pendiente_de_habilitacion(state){
			return Boolean(state.usuario) && !state.usuario.rol_codigo;
		},
		es_admin(){
			return this.rol_activo == 'administrador';
		}
	},
	actions: {
		// Se llama una vez al arrancar la app, y NO entra: solo trae a memoria el token guardado
		// para que el login sepa si hay una sesion que desbloquear. Entrar es una decision del
		// usuario y pasa por la huella: la app siempre arranca en el login, y desde ahi la huella
		// desbloquea la sesion que ya existe en vez de volver a pedir email y contrasena.
		preparar: async function(){
			var vm = this;
			vm.restaurando = true;
			try{
				return Boolean(await restaurar_sesion());
			}finally{
				vm.restaurando = false;
			}
		},

		// Desbloquea la sesion guardada: pregunta a la API si el token todavia vale antes de dejar
		// pasar, porque el rol pudo haber cambiado desde la ultima vez y solo el servidor lo sabe.
		// La huella confirma quien es; esto confirma que la sesion sigue siendo valida.
		iniciar: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const token = await restaurar_sesion();
				if(!token){
					vm.usuario = null;
					return null;
				}
				const respuesta = await obtener_mi_usuario();
				vm.usuario = (respuesta && respuesta.usuario) || null;
				return vm.usuario;
			}catch(error){
				// Solo se borra cuando la API dijo que la sesion ya no vale: un 401 que ademas no se
				// pudo renovar. Un error de red no prueba nada sobre el token, y borrarlo ahi obligaria
				// a escribir la contrasena de nuevo por un problema de wifi.
				const revocada = (error && error.estado_http) == 401;
				if(revocada) await borrar_sesion();
				// El error se muestra si o si: la huella salio bien y el usuario se quedo afuera igual,
				// asi que desde afuera parece que fallo la huella. Hay que decirle que fue la sesion.
				vm.error = revocada
					? 'La sesión guardada ya no vale. Entrá con tu email y contraseña.'
					: (error && error.mensaje) || 'No se pudo reanudar la sesión.';
				vm.usuario = null;
				return null;
			}finally{
				vm.cargando = false;
			}
		},
		entrar: async function(email, password){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const respuesta = await iniciar_sesion(email, password);
				const sesion = (respuesta && respuesta.sesion) || null;
				if(!(sesion && sesion.token)){
					vm.error = 'No se pudo iniciar sesión.';
					return false;
				}
				await guardar_sesion(sesion.token, sesion.expira_en, sesion.refresh_token, sesion.refresh_expira_en);
				vm.usuario = sesion.usuario;
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo iniciar sesión.';
				return false;
			}finally{
				vm.cargando = false;
			}
		},
		salir: async function(todos = false){
			var vm = this;
			try{
				const refresh_token = await obtener_refresh_token();
				// Se avisa al servidor para que revoque el refresh. Si la llamada falla —sin red,
				// por ejemplo— igual se borra la sesion local: quedarse adentro porque no hubo
				// internet seria peor.
				if(refresh_token) await cerrar_sesion(refresh_token, todos);
			}catch{
				// Sin conexion no hay nada que hacer del lado del servidor.
			}
			await vm.limpiar();
		},
		limpiar: async function(){
			var vm = this;
			await borrar_sesion();
			vm.usuario = null;
			vm.error = null;
		}
	}
});
