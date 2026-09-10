import { defineStore } from 'pinia';
import { cerrar_sesion, iniciar_sesion, obtener_mi_usuario } from '@/services/sesion_service';
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
		rol_activo(state){
			return state.usuario?.rol_codigo || null;
		},
		// Autenticado pero sin rol: entro y no puede hacer nada hasta que lo habiliten.
		pendiente_de_habilitacion(state){
			return Boolean(state.usuario) && !state.usuario.rol_codigo;
		},
		es_admin(){
			return this.rol_activo === 'ADMIN';
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
				vm.usuario = respuesta?.usuario || null;
				return vm.usuario;
			}catch{
				// Token vencido o revocado: no es un error que haya que mostrar, es que no hay sesion.
				await borrar_sesion();
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
				const sesion = respuesta?.sesion || null;
				if(!sesion?.token){
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
