import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

export function obtener_usuarios(opciones = {}){
	const query = construir_query({
		busqueda: opciones.busqueda,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/usuarios${query}`, metodo: 'GET'});
}

export function crear_usuario(datos){
	return ajax_request({endpoint: '/api/usuarios', metodo: 'POST', datos});
}

export function cambiar_rol_usuario(id, rol_id){
	return ajax_request({endpoint: `/api/usuarios/${encodeURIComponent(id)}/rol`, metodo: 'PUT', datos: {rol_id}});
}

export function eliminar_usuario(id){
	return ajax_request({endpoint: `/api/usuarios/${encodeURIComponent(id)}`, metodo: 'DELETE'});
}
