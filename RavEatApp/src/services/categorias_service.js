import { ajax_request } from './ajax_service';

export function obtener_categorias(){
	return ajax_request({endpoint: '/api/categorias', metodo: 'GET'});
}

export function crear_categoria(datos){
	return ajax_request({endpoint: '/api/categorias', metodo: 'POST', datos});
}

export function actualizar_categoria(id, datos){
	return ajax_request({endpoint: `/api/categorias/${encodeURIComponent(id)}`, metodo: 'PUT', datos});
}

export function eliminar_categoria(id){
	return ajax_request({endpoint: `/api/categorias/${encodeURIComponent(id)}`, metodo: 'DELETE'});
}
