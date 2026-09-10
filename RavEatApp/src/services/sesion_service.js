import { ajax_request } from './ajax_service';

export function iniciar_sesion(email, password){
	return ajax_request({endpoint: '/api/sesion/login', metodo: 'POST', datos: {email, password}});
}

export function cerrar_sesion(refresh_token, todos = false){
	return ajax_request({endpoint: '/api/sesion/logout', metodo: 'POST', datos: {refresh_token, todos}});
}

// Quien soy. La app lo llama al arrancar para saber si el token guardado todavia vale, y para
// enterarse de si un admin le cambio el rol desde la ultima vez.
export function obtener_mi_usuario(){
	return ajax_request({endpoint: '/api/sesion/yo', metodo: 'GET'});
}
