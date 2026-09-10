import $ from 'jquery';
import { obtener_api_url } from '@/config/debug';
import {
	cabecera_autorizacion,
	guardar_sesion,
	notificar_sesion_expirada,
	obtener_refresh_token
} from '@/services/token_service';

// El unico modulo de la app que habla con la API. Ninguna pagina ni componente llama a la red por
// su cuenta: el camino es siempre pagina -> store o service -> ajax_service -> API. Concentrarlo
// aca es lo que permite que el token y la renovacion vivan en un solo archivo.
const api_url = obtener_api_url();

// Una sola renovacion a la vez: si diez requests reciben 401 juntas, todas esperan la MISMA
// llamada a /refresh en vez de gastar (y rotar) diez refresh tokens. Con rotacion, diez llamadas
// en paralelo se revocan entre si y terminan cerrando la sesion de un usuario que no hizo nada mal.
let renovacion_en_curso = null;

function construir_url(endpoint){
	if(!api_url) throw new Error('No se configuro la URL de la API.');
	return `${api_url}/${String(endpoint).replace(/^\/+/, '')}`;
}

// Los errores de red llegan en varias formas distintas (timeout, cancelado, respuesta de la API) y
// cada pantalla tendria que distinguirlas. Se normalizan a un solo objeto para que el store solo
// tenga que leer `mensaje`.
function normalizar_error(xhr, text_status, error_thrown){
	const respuesta = xhr.responseJSON || null;
	if(text_status === 'timeout'){
		return {
			estado_http: xhr.status || 0,
			codigo: 'tiempo_espera_agotado',
			mensaje: 'La solicitud tardó demasiado en responder. Intenta nuevamente.',
			respuesta
		};
	}
	if(text_status === 'abort'){
		return {
			estado_http: xhr.status || 0,
			codigo: 'cancelado',
			mensaje: 'La solicitud fue cancelada.',
			respuesta
		};
	}
	// Un status 0 no es "error 0": es que la request nunca llegó. API apagada, IP equivocada o CORS.
	if(!xhr.status){
		return {
			estado_http: 0,
			codigo: 'sin_conexion',
			mensaje: 'No se pudo conectar con la API. Revisá que esté levantada y que la URL sea la correcta.',
			respuesta
		};
	}
	// 403 no es 401: el token es valido, pero el rol no alcanza. Renovarlo no cambiaria nada, y
	// mandar al login seria mentirle al usuario sobre lo que pasó.
	if(xhr.status === 403){
		return {
			estado_http: 403,
			codigo: respuesta?.codigo || 'sin_permiso',
			mensaje: respuesta?.mensaje || 'Tu rol no tiene permiso para esta acción.',
			respuesta
		};
	}
	return {
		estado_http: xhr.status,
		codigo: respuesta?.codigo || text_status || 'error_ajax',
		mensaje: respuesta?.mensaje || error_thrown || 'No se pudo completar la solicitud.',
		respuesta
	};
}

// Cambia el access token vencido por uno nuevo usando el refresh. Vive aca y no en un service de
// sesion porque ajax_service es el unico que habla con la API, y un import al reves seria circular.
// Devuelve true si la sesion quedo renovada.
function renovar_sesion(){
	if(renovacion_en_curso) return renovacion_en_curso;
	renovacion_en_curso = (async function(){
		const refresh_token = await obtener_refresh_token();
		if(!refresh_token) return false;
		try{
			const respuesta = await new Promise((resolve, reject) =>{
				$.ajax({
					url: construir_url('/api/sesion/refresh'),
					method: 'POST',
					data: JSON.stringify({refresh_token}),
					dataType: 'json',
					contentType: 'application/json; charset=utf-8',
					timeout: 15000,
					headers: {Accept: 'application/json'}
				}).done(resolve).fail(() => reject(new Error('refresh_fallido')));
			});
			const sesion = respuesta?.sesion || null;
			if(!sesion?.token) return false;
			await guardar_sesion(sesion.token, sesion.expira_en, sesion.refresh_token, sesion.refresh_expira_en);
			return true;
		}catch{
			return false;
		}
	})().finally(() =>{
		renovacion_en_curso = null;
	});
	return renovacion_en_curso;
}

// Un 401 significa dos cosas distintas: que vencio el access token (se renueva y se reintenta una
// sola vez) o que la sesion ya no vale (usuario dado de baja, rol cambiado, refresh vencido) y hay
// que volver al login. El reintento nunca se encadena: si el segundo intento tambien da 401, corta.
async function manejar_401(permitir_renovar, reintentar){
	if(!permitir_renovar){
		notificar_sesion_expirada();
		throw {estado_http: 401, codigo: 'sesion_expirada', mensaje: 'La sesión expiró. Volvé a iniciar sesión.', respuesta: null};
	}
	const renovada = await renovar_sesion();
	if(!renovada){
		notificar_sesion_expirada();
		throw {estado_http: 401, codigo: 'sesion_expirada', mensaje: 'La sesión expiró. Volvé a iniciar sesión.', respuesta: null};
	}
	return reintentar();
}

export function ajax_request(configuracion = {}){
	return ejecutar_request(configuracion, true);
}

function ejecutar_request(configuracion, permitir_renovar){
	const {
		endpoint,
		metodo = 'GET',
		datos = null,
		timeout = 15000,
		headers = {}
	} = configuracion;
	const metodo_normalizado = metodo.toUpperCase();
	const envia_json = datos !== null && !['GET', 'HEAD'].includes(metodo_normalizado);
	return new Promise((resolve, reject) =>{
		$.ajax({
			url: construir_url(endpoint),
			method: metodo_normalizado,
			data: envia_json ? JSON.stringify(datos) : datos,
			dataType: 'json',
			contentType: envia_json ? 'application/json; charset=utf-8' : undefined,
			timeout,
			headers: {
				Accept: 'application/json',
				...cabecera_autorizacion(),
				...headers
			}
		})
			.done(respuesta => resolve(respuesta))
			.fail((xhr, text_status, error_thrown) =>{
				if(xhr.status === 401){
					manejar_401(permitir_renovar, () => ejecutar_request(configuracion, false))
						.then(resolve, error => reject(error?.estado_http ? error : normalizar_error(xhr, text_status, error_thrown)));
					return;
				}
				reject(normalizar_error(xhr, text_status, error_thrown));
			});
	});
}
