import { defineStore } from 'pinia';
import { obtener_rol_por_codigo } from '@/config/roles';
import { borrar_copias, guardar_copia } from '@/services/copia_local_service';
import { cerrar_sesion, iniciar_sesion, obtener_mi_usuario } from '@/services/sesion_service';
import { use_offline_store } from '@/stores/offline_store';
import {
	al_expirar,
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
		// La API habla en codigos (`ADMIN`) y el resto de la app en ids (`administrador`). La
		// traduccion se hace aca, en el borde con el servidor, y no en cada consumidor: mientras
		// devolvia el codigo crudo, el menu y los tabs comparaban 'ADMIN' contra 'administrador'
		// y no le mostraban una sola pantalla a nadie.
		rol_activo(state){
			const rol = obtener_rol_por_codigo(state.usuario && state.usuario.rol_codigo);
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
		// Se llama una vez al arrancar la app. Si hay un token guardado, pregunta a la API si
		// todavia vale: el rol pudo haber cambiado desde la ultima vez, y solo el servidor lo sabe.
		iniciar: async function(){
			var vm = this;
			vm.restaurando = true;
			// El service avisa por callback cuando ya no se puede renovar. Se conecta aca y no en
			// ajax_service para no crear un ciclo de imports entre los dos.
			al_expirar(() => vm.limpiar());
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
				// La sesion guardada se borra SOLO si el servidor dijo que ya no vale (401): token
				// vencido, usuario dado de baja. Un error de red —la API todavia no levanto, se
				// solto el cable— no prueba nada sobre el token, y borrarlo obligaba a escribir la
				// contrasena de nuevo por un problema que no era de la sesion. Sin usuario igual se
				// va al login, pero el token sigue ahi y la huella lo desbloquea.
				if(error && error.estado_http == 401) await borrar_sesion();
				vm.usuario = null;
				return null;
			}finally{
				vm.restaurando = false;
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
				// El login ya trae al usuario: se guarda como la copia de "quien soy", asi la app
				// puede arrancar sin red desde la primera vez.
				guardar_copia('/api/sesion/yo', {usuario: sesion.usuario});
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
			// Lo guardado para mirar sin red y lo que esperaba en la cola son de quien los cargo:
			// con la sesion se van los dos, y nada pasa de un usuario al siguiente.
			borrar_copias();
			use_offline_store().guardar_cola([]);
			vm.usuario = null;
			vm.error = null;
		}
	}
});
