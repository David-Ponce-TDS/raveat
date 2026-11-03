import { SecureStorage } from '@aparajita/capacitor-secure-storage';

// Donde vive la sesion en el dispositivo. Es el unico modulo que la lee y la escribe.
//
// En el telefono usa el almacenamiento seguro del sistema (Keystore en Android), no localStorage:
// un token en localStorage lo lee cualquier script que se cuele en la webview. En el navegador el
// plugin cae a localStorage porque no hay nada mejor, y eso esta bien para desarrollo.
//
// El access token ademas se guarda en memoria: cada request lo necesita, y leer del Keystore en
// cada llamada es ir a disco de gusto.
const CLAVE_TOKEN = 'raveat_token';
const CLAVE_REFRESH = 'raveat_refresh';
const CLAVE_EXPIRA = 'raveat_expira';

let token_en_memoria = null;
let al_expirar_sesion = null;

async function guardar(clave, valor){
	if(valor == null){
		await SecureStorage.remove(clave);
		return;
	}
	await SecureStorage.set(clave, String(valor));
}

async function leer(clave){
	try{
		return await SecureStorage.get(clave);
	}catch{
		// El plugin tira si la clave no existe. "No hay sesion guardada" no es un error.
		return null;
	}
}

export async function guardar_sesion(token, expira_en, refresh_token, refresh_expira_en){
	token_en_memoria = token;
	await guardar(CLAVE_TOKEN, token);
	await guardar(CLAVE_EXPIRA, expira_en);
	await guardar(CLAVE_REFRESH, refresh_token);
	// refresh_expira_en no se guarda: la vigencia la decide el servidor. Guardarla invitaria a
	// tomar decisiones en el cliente sobre algo que solo la API sabe.
	return refresh_expira_en;
}

export async function restaurar_sesion(){
	token_en_memoria = await leer(CLAVE_TOKEN);
	return token_en_memoria;
}

export async function borrar_sesion(){
	token_en_memoria = null;
	await SecureStorage.remove(CLAVE_TOKEN);
	await SecureStorage.remove(CLAVE_REFRESH);
	await SecureStorage.remove(CLAVE_EXPIRA);
}

// En iPhone el llavero (Keychain) sobrevive a la desinstalacion de la app: sin esto, reinstalar
// RavEat encontraba la sesion de la instalacion anterior y arrancaba logueado con un token viejo.
// El localStorage del WebView, en cambio, se borra con la app: si la marca no esta, es una
// instalacion nueva y lo que haya en el almacenamiento seguro es un resto de la anterior.
const MARCA_INSTALACION = 'raveat_instalada';

export async function limpiar_restos_de_instalacion_anterior(){
	try{
		if(localStorage.getItem(MARCA_INSTALACION)) return;
		// La marca va primero: si borrar fallara, la proxima apertura no vuelve a borrar una
		// sesion nueva que el usuario ya haya iniciado.
		localStorage.setItem(MARCA_INSTALACION, '1');
	}catch{
		// Sin localStorage no se puede saber si la instalacion es nueva: no se toca nada.
		return;
	}
	token_en_memoria = null;
	for(const clave of [CLAVE_TOKEN, CLAVE_REFRESH, CLAVE_EXPIRA, 'raveat_bloqueo_biometrico']){
		try{
			await SecureStorage.remove(clave);
		}catch{
			// Una clave que no existe no es un error.
		}
	}
}

export function obtener_token(){
	return token_en_memoria;
}

export function obtener_refresh_token(){
	return leer(CLAVE_REFRESH);
}

// Se arma aca y no en cada service: si el dia de manana el esquema deja de ser Bearer, cambia
// una sola linea.
export function cabecera_autorizacion(){
	return token_en_memoria ? {Authorization: `Bearer ${token_en_memoria}`} : {};
}

// `ajax_service` avisa por aca cuando la sesion ya no se puede renovar. El store se suscribe y
// manda al login. Se hace con un callback y no importando el store para evitar el ciclo
// ajax_service -> sesion_store -> ajax_service.
export function al_expirar(callback){
	al_expirar_sesion = callback;
}

export function notificar_sesion_expirada(){
	token_en_memoria = null;
	if(al_expirar_sesion) al_expirar_sesion();
}
