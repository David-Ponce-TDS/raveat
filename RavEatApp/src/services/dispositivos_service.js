import { ajax_request } from './ajax_service';

// Le dice a la API en que telefono esta abierta la sesion, para que le mande ahi las push. El
// usuario sale del token: el body lleva solo el token de FCM.
export function registrar_dispositivo(token){
	return ajax_request({endpoint: '/api/dispositivos', metodo: 'PUT', datos: {token}});
}
