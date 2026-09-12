import { defineStore } from 'pinia';
import { obtener_resumen_productos } from '@/services/productos_service';

// El estado de la carta vive en el store y no en la pagina: la pagina solo lee y muestra.
export const use_productos_store = defineStore('productos', {
	state: () => ({
		resumen: null,
		categorias: [],
		productos: [],
		cargando: false,
		error: null
	}),
	getters: {
		hay_productos(state){
			return state.productos.length > 0;
		}
	},
	actions: {
		cargar_productos: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			try{
				const respuesta = await obtener_resumen_productos();
				vm.resumen = respuesta?.resumen || null;
				vm.categorias = respuesta?.categorias || [];
				vm.productos = respuesta?.productos || [];
				return vm.productos;
			}catch(error){
				// El mensaje ya viene normalizado de ajax_service: la pantalla no ve codigos HTTP.
				vm.error = error.mensaje || 'No se pudo cargar el catálogo.';
				vm.productos = [];
				vm.categorias = [];
				vm.resumen = null;
				return [];
			}finally{
				vm.cargando = false;
			}
		}
	}
});
