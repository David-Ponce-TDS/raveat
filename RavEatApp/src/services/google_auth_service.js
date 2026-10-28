import { Capacitor, registerPlugin } from '@capacitor/core';

// Entrar con Google. Son dos caminos para lo mismo, porque el entorno es distinto:
//   - en el telefono, la pantalla de cuentas del sistema, con el plugin propio `GoogleAuthPlugin`;
//   - en el navegador, el boton oficial de Google Identity Services.
// Los dos terminan en lo unico que le importa a la API: un `id_token` firmado por Google.
//
// Sin `VITE_GOOGLE_WEB_CLIENT_ID` esto queda apagado y el login sigue con email y contraseña:
// mismo criterio que el push, el build no puede depender de una credencial.

const google_auth = registerPlugin('GoogleAuth');
const GOOGLE_IDENTITY_SRC = 'https://accounts.google.com/gsi/client';
let promesa_identity = null;

export function google_web_client_id(){
	return import.meta.env.VITE_GOOGLE_WEB_CLIENT_ID || '';
}

export function google_habilitado(){
	return Boolean(google_web_client_id());
}

export function google_es_nativo(){
	return Capacitor.isNativePlatform();
}

// Igual que la camara y el dictado: el resultado se devuelve, no se tira. Cancelar no es un error.
export async function entrar_con_google_nativo(){
	const web_client_id = google_web_client_id();
	if(!web_client_id) return {ok: false, id_token: null, mensaje: 'El ingreso con Google no está configurado.'};
	try{
		const respuesta = await google_auth.signIn({web_client_id});
		if(!(respuesta && respuesta.id_token)) return {ok: false, id_token: null, mensaje: 'Google no devolvió el token de identidad.'};
		return {ok: true, id_token: respuesta.id_token, mensaje: null};
	}catch(error){
		// El plugin devuelve SIGN_IN_CANCELLED (12501) cuando se cierra la pantalla de cuentas:
		// eso no se avisa, igual que cancelar la camara.
		if(error && error.code == '12501') return {ok: false, id_token: null, mensaje: null};
		return {ok: false, id_token: null, mensaje: (error && error.message) || 'No se pudo entrar con Google.'};
	}
}

// Soltar la cuenta elegida. Hace falta al cerrar sesion, para que el proximo ingreso vuelva a
// preguntar, y sobre todo cuando el ingreso FALLA: Google recuerda la ultima cuenta, y si la API la
// rechazo —un email que no esta dado de alta— el siguiente intento entraba con la misma sin mostrar
// el selector.
export async function olvidar_cuenta_google(){
	if(google_es_nativo()){
		try{
			await google_auth.signOut();
		}catch{
			// Soltar la cuenta es un extra: si falla, el resto sigue igual.
		}
		return;
	}
	// En el navegador el equivalente es cortar la seleccion automatica. Si el script de Google no
	// llego a cargar, no hay nada que olvidar.
	if(window.google && window.google.accounts) window.google.accounts.id.disableAutoSelect();
}

// En el navegador el boton lo dibuja Google: es un requisito de su marca y evita manejar el popup a
// mano. Devuelve false si no se pudo (sin client id o sin red).
export function renderizar_boton_google(contenedor, al_recibir_token){
	if(google_es_nativo() || !contenedor) return Promise.resolve(false);
	const web_client_id = google_web_client_id();
	if(!web_client_id) return Promise.resolve(false);
	return cargar_google_identity().then(function(){
		contenedor.innerHTML = '';
		window.google.accounts.id.initialize({
			client_id: web_client_id,
			callback: function(respuesta){
				if(respuesta && respuesta.credential) al_recibir_token(respuesta.credential);
			}
		});
		window.google.accounts.id.renderButton(contenedor, {
			theme: 'outline',
			size: 'large',
			type: 'standard',
			text: 'signin_with',
			shape: 'rectangular',
			width: Math.min(contenedor.clientWidth || 320, 380)
		});
		return true;
	}).catch(function(){
		return false;
	});
}

// El script de Google se carga una sola vez y la promesa queda guardada: el login se monta y se
// desmonta varias veces, y cada montaje no puede volver a bajar el mismo script.
function cargar_google_identity(){
	if(window.google && window.google.accounts) return Promise.resolve();
	if(promesa_identity) return promesa_identity;
	promesa_identity = new Promise(function(resolver, rechazar){
		const script = document.createElement('script');
		script.src = GOOGLE_IDENTITY_SRC;
		script.async = true;
		script.defer = true;
		script.onload = resolver;
		script.onerror = function(){
			// Sin red el script no llega: se suelta la promesa para que el proximo montaje reintente.
			promesa_identity = null;
			rechazar(new Error('No se pudo cargar Google Identity Services.'));
		};
		document.head.appendChild(script);
	});
	return promesa_identity;
}
