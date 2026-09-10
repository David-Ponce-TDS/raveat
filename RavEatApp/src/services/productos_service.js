import { ajax_request } from './ajax_service';

// La vitrina: la carta entera con sus categorias y los contadores.
export function obtener_resumen_productos(){
	return ajax_request({endpoint: '/api/productos/resumen', metodo: 'GET'});
}

export function obtener_producto(id){
	return ajax_request({endpoint: `/api/productos/${encodeURIComponent(id)}`, metodo: 'GET'});
}
