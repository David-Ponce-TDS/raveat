import { ajax_request } from './ajax_service';

export function obtener_categorias(){
	return ajax_request({endpoint: '/api/categorias', metodo: 'GET'});
}
