import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// No hay funcion para escribir historial: lo escribe la API en las acciones que registra. Este
// service solo lee.
export function obtener_historial(opciones = {}){
	const query = construir_query({
		tipo: opciones.tipo,
		busqueda: opciones.busqueda,
		solo_mios: opciones.solo_mios,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/historial${query}`, metodo: 'GET'});
}
