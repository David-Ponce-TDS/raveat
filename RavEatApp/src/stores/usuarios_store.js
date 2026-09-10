import { defineStore } from 'pinia';
import {
	cambiar_rol_usuario,
	crear_usuario,
	eliminar_usuario,
	obtener_usuarios
} from '@/services/usuarios_service';

export const use_usuarios_store = defineStore('usuarios', {
	state: () => ({
		usuarios: [],
		// Los roles vienen con el listado, no en una segunda consulta: la pantalla los necesita
		// para el desplegable de asignacion.
		roles: [],
		pagina: null,
		filtros: {busqueda: ''},
		cargando: false,
		guardando: false,
		error: null
	}),
	getters: {
		hay_usuarios(state){
			return state.usuarios.length > 0;
		},
		hay_mas(state){
			return Boolean(state.pagina?.hay_mas);
		},
		total(state){
			return state.pagina?.total || 0;
		}
	},
	actions: {
		cargar: async function(filtros = null){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			if(filtros) vm.filtros = {...vm.filtros, ...filtros};
			try{
				const respuesta = await obtener_usuarios({...vm.filtros, pagina: 1});
				vm.usuarios = respuesta?.usuarios || [];
				vm.roles = respuesta?.roles || [];
				vm.pagina = respuesta?.pagina || null;
				return vm.usuarios;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar los usuarios.';
				vm.usuarios = [];
				vm.pagina = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		cargar_mas: async function(){
			var vm = this;
			if(vm.cargando || !vm.hay_mas) return vm.usuarios;
			vm.cargando = true;
			try{
				const respuesta = await obtener_usuarios({...vm.filtros, pagina: (vm.pagina?.pagina || 1) + 1});
				const conocidos = vm.usuarios.map(usuario => usuario.id);
				vm.usuarios = [...vm.usuarios, ...(respuesta?.usuarios || []).filter(usuario => !conocidos.includes(usuario.id))];
				vm.pagina = respuesta?.pagina || null;
				return vm.usuarios;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar más usuarios.';
				return vm.usuarios;
			}finally{
				vm.cargando = false;
			}
		},
		crear: async function(datos){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await crear_usuario(datos);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo crear el usuario.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		// Cambiar el rol invalida el token que ese usuario tenga en la mano: la API compara el rol
		// del token contra el de la base en cada request.
		cambiar_rol: async function(id, rol_id){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await cambiar_rol_usuario(id, rol_id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo cambiar el rol.';
				return false;
			}finally{
				vm.guardando = false;
			}
		},
		eliminar: async function(id){
			var vm = this;
			vm.guardando = true;
			vm.error = null;
			try{
				await eliminar_usuario(id);
				await vm.cargar();
				return true;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo dar de baja al usuario.';
				return false;
			}finally{
				vm.guardando = false;
			}
		}
	}
});
