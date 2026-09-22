import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

export function obtener_entregas(opciones = {}){
	const query = construir_query({
		estados: Array.isArray(opciones.estados) ? opciones.estados.join(',') : opciones.estados,
		solo_mias: opciones.solo_mias,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/entregas${query}`, metodo: 'GET'});
}

// Sin repartidor_id la toma el que llama; con id la asigna un rol que vende. Quien puede hacer
// cada cosa lo valida el servidor.
export function asignar_entrega(id, repartidor_id = null){
	return ajax_request({endpoint: `/api/entregas/${id}/asignar`, metodo: 'PUT', datos: {repartidor_id}});
}

export function cambiar_estado_entrega(id, estado){
	return ajax_request({endpoint: `/api/entregas/${id}/estado`, metodo: 'PUT', datos: {estado}});
}
