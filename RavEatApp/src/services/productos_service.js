import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// La vitrina: la carta entera con sus categorias y los contadores. No pagina porque una carta de
// restaurante entra en una respuesta.
export function obtener_resumen_productos(){
	return ajax_request({endpoint: '/api/productos/resumen', metodo: 'GET'});
}

// El catalogo de gestion: paginado, y con la busqueda y los filtros resueltos en la API.
export function obtener_listado_productos(opciones = {}){
	const query = construir_query({
		busqueda: opciones.busqueda,
		categoria_id: opciones.categoria_id,
		disponible: opciones.disponible,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/productos/listado${query}`, metodo: 'GET'});
}

export function obtener_producto(id){
	return ajax_request({endpoint: `/api/productos/${encodeURIComponent(id)}`, metodo: 'GET'});
}

export function crear_producto(datos){
	return ajax_request({endpoint: '/api/productos', metodo: 'POST', datos});
}

export function actualizar_producto(id, datos){
	return ajax_request({endpoint: `/api/productos/${encodeURIComponent(id)}`, metodo: 'PUT', datos});
}

export function eliminar_producto(id){
	return ajax_request({endpoint: `/api/productos/${encodeURIComponent(id)}`, metodo: 'DELETE'});
}
