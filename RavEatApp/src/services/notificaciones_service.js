import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// La bandeja es siempre la del token: no se manda usuario_id porque la API no lo acepta — con un
// id en la query, cualquiera leeria la bandeja de cualquiera.
export function obtener_notificaciones(opciones = {}){
	const query = construir_query({
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/notificaciones${query}`, metodo: 'GET'});
}

// Con id marca una sola; sin id, todas las pendientes.
export function marcar_notificaciones_leidas(notificacion_id = null){
	return ajax_request({endpoint: '/api/notificaciones/leer', metodo: 'PUT', datos: {notificacion_id}});
}
