import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

export function obtener_comandas(opciones = {}){
	const query = construir_query({
		estados: Array.isArray(opciones.estados) ? opciones.estados.join(',') : opciones.estados,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/comandas${query}`, metodo: 'GET'});
}

export function cambiar_estado_item(id, estado){
	return ajax_request({endpoint: `/api/comandas/items/${id}/estado`, metodo: 'PUT', datos: {estado}});
}

// "Salio todo": el atajo de la cocina cuando el pedido sale completo.
export function marcar_comanda_lista(pedido_id){
	return ajax_request({endpoint: `/api/comandas/${pedido_id}/listo`, metodo: 'PUT'});
}
