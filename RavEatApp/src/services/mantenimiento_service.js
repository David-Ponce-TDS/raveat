import { ajax_request } from './ajax_service';

// Herramienta de prueba, no una accion del local: deja la base como antes del primer pedido para
// volver a arrancar un ensayo desde cero. La API la abre solo al administrador. Pide red: no lleva
// la bandera `encolable`, asi que sin conexion falla en el acto en vez de borrar horas despues.
export function vaciar_pedidos(){
	return ajax_request({endpoint: '/api/mantenimiento/pedidos', metodo: 'DELETE'});
}
