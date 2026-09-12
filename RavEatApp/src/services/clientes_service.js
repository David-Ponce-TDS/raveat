import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// La busqueda viaja a la API, no se resuelve en la pantalla: la lista esta paginada, asi que
// filtrar en memoria solo miraria la pagina ya descargada.
export function obtener_clientes(opciones = {}){
	const query = construir_query({
		busqueda: opciones.busqueda,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/clientes${query}`, metodo: 'GET'});
}

export function obtener_cliente(id){
	return ajax_request({endpoint: `/api/clientes/${encodeURIComponent(id)}`, metodo: 'GET'});
}

export function crear_cliente(datos){
	return ajax_request({endpoint: '/api/clientes', metodo: 'POST', datos});
}

export function actualizar_cliente(id, datos){
	return ajax_request({endpoint: `/api/clientes/${encodeURIComponent(id)}`, metodo: 'PUT', datos});
}

export function eliminar_cliente(id){
	return ajax_request({endpoint: `/api/clientes/${encodeURIComponent(id)}`, metodo: 'DELETE'});
}
