import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// Los estados viajan como CSV en snake_case ('en_preparacion,listo'), igual que los serializa la
// API. Un valor desconocido lo ignora el servidor en vez de devolver 400: el filtro es de la
// pantalla, no una orden que haya que validar.
export function obtener_pedidos(opciones = {}){
	const query = construir_query({
		busqueda: opciones.busqueda,
		estados: Array.isArray(opciones.estados) ? opciones.estados.join(',') : opciones.estados,
		cliente_id: opciones.cliente_id,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/pedidos${query}`, metodo: 'GET'});
}

export function obtener_pedido(id){
	return ajax_request({endpoint: `/api/pedidos/${encodeURIComponent(id)}`, metodo: 'GET'});
}

// El cuerpo NO lleva precios: los pone el servidor leyendolos de la base. Si el precio viajara
// desde el telefono, cualquiera podria pedir una pizza a un peso.
export function crear_pedido(datos){
	return ajax_request({endpoint: '/api/pedidos', metodo: 'POST', datos});
}

export function cambiar_estado_pedido(id, estado){
	return ajax_request({endpoint: `/api/pedidos/${encodeURIComponent(id)}/estado`, metodo: 'PUT', datos: {estado}});
}

export function registrar_pago_pedido(id, medio_pago, propina_importe = 0){
	return ajax_request({endpoint: `/api/pedidos/${encodeURIComponent(id)}/pago`, metodo: 'PUT', datos: {medio_pago, propina_importe}});
}

export function cancelar_pedido(id){
	return ajax_request({endpoint: `/api/pedidos/${encodeURIComponent(id)}`, metodo: 'DELETE'});
}
