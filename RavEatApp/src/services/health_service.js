import { ajax_request } from './ajax_service';

// Sirve para separar dos fallas que se confunden: "la API no responde" y "la API responde pero el
// endpoint falla". Si /health anda y /api/productos no, el problema no es la conexion.
export function consultar_health(){
	return ajax_request({endpoint: '/health', metodo: 'GET', timeout: 5000});
}
