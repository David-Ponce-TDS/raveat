import { ajax_request } from './ajax_service';

export function iniciar_sesion(email, password){
	return ajax_request({endpoint: '/api/sesion/login', metodo: 'POST', datos: {email, password}, renovar: false});
}

// El token de FCM viaja con el logout: es cuando el telefono deja de ser de este usuario, y la API
// lo da de baja en el mismo paso, sin un endpoint aparte.
export function cerrar_sesion(refresh_token, todos = false, token_push = null){
	return ajax_request({endpoint: '/api/sesion/logout', metodo: 'POST', datos: {refresh_token, todos, token_push}, renovar: false});
}

// Quien soy. La app lo llama al arrancar para saber si el token guardado todavia vale, y para
// enterarse de si un admin le cambio el rol desde la ultima vez. `guardable`: sin red la app
// arranca con el usuario de la ultima vez, en lugar de mandar al login.
export function obtener_mi_usuario(){
	return ajax_request({endpoint: '/api/sesion/yo', metodo: 'GET', guardable: true});
}
