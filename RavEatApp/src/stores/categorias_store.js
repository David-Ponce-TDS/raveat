import { defineStore } from 'pinia';
import { crear_categoria, obtener_categorias } from '@/services/categorias_service';

// Todas las categorias activas, tengan productos o no: el resumen de la vitrina trae solo las que
// ya tienen alguno, y el formulario de producto y los chips del catalogo necesitan todas.
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
				vm.categorias = (respuesta && respuesta.categorias) || [];
				return vm.categorias;
			}catch(error){
				vm.error = error.mensaje || 'No se pudieron cargar las categorías.';
				vm.categorias = [];
				return [];
			}finally{
				vm.cargando = false;
			}
		},
		// Devuelve la categoria creada (o null): el formulario de producto la elige al vuelo.
		crear: async function(datos){
			var vm = this;
			vm.error = null;
			try{
				const respuesta = await crear_categoria(datos);
				// Despues de escribir se recarga desde la API: el orden lo decide el servidor.
				await vm.cargar_categorias();
				return (respuesta && respuesta.categoria) || null;
			}catch(error){
				vm.error = error.mensaje || 'No se pudo crear la categoría.';
				return null;
			}
		}
	}
});
