import { defineStore } from 'pinia';
import { obtener_categorias } from '@/services/categorias_service';

// Semilla de la version 9: hoy no lo usa ninguna pantalla, porque las categorias vienen dentro
// de /api/productos/resumen. Existe para que /api/categorias tenga su camino completo desde la
// version en que aparece el endpoint.
export const use_categorias_store = defineStore('categorias', {
	state: () => ({
		categorias: [],
		cargando: false,
		error: null
	}),
	actions: {
		cargar_categorias: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const respuesta = await obtener_categorias();
				vm.categorias = respuesta?.categorias || [];
				return vm.categorias;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar las categorías.';
				vm.categorias = [];
				return [];
			}finally{
				vm.cargando = false;
			}
		}
	}
});
