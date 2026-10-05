import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { PushNotifications } from '@capacitor/push-notifications';

// Las notificaciones NATIVAS: la local (la programa la propia app) y la push (viaja por Firebase).
// La tercera "notificacion" del modulo, la bandeja, no pasa por aca: es una tabla en la API.
let inicializado = false;
// El canal de los avisos, con importancia alta: asi Android los muestra como banner flotante sobre
// la pantalla, y no solo callados en la cortina. Es el mismo canal para la push (la API lo nombra
// y el manifiesto lo tiene como predeterminado) y para el aviso local que la repite con la app
// abierta. Se crea una vez al arrancar; si ya existe, Android no lo toca.
const CANAL_AVISOS = {id: 'avisos', name: 'Avisos de pedidos', description: 'Cambios de estado de los pedidos y las entregas.', importance: 5, visibility: 1, vibration: true};
// El token de FCM de este telefono. Lo pide la sesion para anotarlo en la API al entrar y para
// darlo de baja al salir.
let token_push = null;

// `acciones` las pone main.js, que tiene a mano la sesion y el router; este service solo habla con
// el sistema operativo. Las tres son: al_registrar (llego el token), al_recibir (llego una push con
// la app abierta) y al_tocar (el usuario toco el aviso, con los datos que mando la API).
export async function inicializar_notificaciones_nativas(acciones){
	if(inicializado) return;
	inicializado = true;
	await preparar_notificaciones_locales(acciones);
	await preparar_push(acciones);
}

export function obtener_token_push(){
	return token_push;
}

// Notificacion local: la dispara la app como respuesta a algo que el usuario acaba de hacer.
// No viaja por internet ni necesita servidor — por eso funciona hasta en modo avion.
//
// Devuelve {ok, mensaje} como la camara y la ubicacion: sin eso, el permiso denegado dejaba la
// funcion muda y parecia que la app estaba rota. La pantalla decide si lo muestra.
//
// `datos` viaja pegado al aviso y vuelve al tocarlo: es lo que usa la push recibida con la app
// abierta para abrir el mismo pedido que habria abierto la push.
export async function notificar_evento_local(titulo, mensaje, datos = {}){
	if(!Capacitor.isPluginAvailable('LocalNotifications')){
		// En el navegador no hay notificaciones del sistema, y no es una falla que haya que contar.
		return {ok: false, mensaje: null};
	}
	const permisos = await LocalNotifications.checkPermissions();
	if(permisos.display != 'granted'){
		const solicitados = await LocalNotifications.requestPermissions();
		if(solicitados.display != 'granted'){
			return {ok: false, mensaje: 'Sin permiso de notificaciones: no vas a recibir los avisos del teléfono. Habilitalo en los ajustes.'};
		}
	}
	await LocalNotifications.schedule({
		notifications: [{
			// El id es obligatorio y tiene tope de int32; el resto del timestamp no hace falta.
			id: Date.now() % 2147483647,
			title: titulo,
			body: mensaje,
			extra: datos,
			channelId: CANAL_AVISOS.id
			// Sin `schedule`: se muestra al instante. Con `schedule.at` Android 12+ la toma como
			// alarma inexacta (falta el permiso de alarmas exactas) y el aviso llegaba ~25 s tarde.
		}]
	});
	return {ok: true, mensaje: null};
}

async function preparar_notificaciones_locales(acciones){
	if(!Capacitor.isPluginAvailable('LocalNotifications')) return;
	// Solo las que vinieron de una push traen pedido_codigo: tocar un aviso local comun no navega.
	LocalNotifications.addListener('localNotificationActionPerformed', accion => {
		const datos = accion.notification.extra;
		if(datos && datos.pedido_codigo != null) acciones.al_tocar(datos);
	});
	// Los canales son de Android: en el navegador no existen y el plugin tira error.
	if(Capacitor.getPlatform() == 'android') await LocalNotifications.createChannel(CANAL_AVISOS);
	const permisos = await LocalNotifications.checkPermissions();
	if(permisos.display != 'granted') await LocalNotifications.requestPermissions();
}

// Push: la envia NUESTRA API cada vez que escribe un aviso en la bandeja, y FCM la lleva hasta el
// telefono. La app se registra en FCM, le pasa el token a la API (eso lo hace la sesion con
// al_registrar) y reacciona cuando llega o cuando la tocan. Necesita google-services.json (no
// versionado) y la bandera en el .env; sin ellos, este paso se saltea y todo lo demas sigue andando.
async function preparar_push(acciones){
	// Prendido por defecto y se apaga a mano con 'false': al reves —opt-in— el que seguia la
	// unidad al pie de la letra terminaba con el push mudo por una linea que nunca copio, y sin
	// ningun error que se lo dijera. Sin google-services.json el registro falla y lo agarra el
	// catch de abajo, asi que apagarlo no hace falta para que la app ande.
	if(import.meta.env.VITE_PUSH_NOTIFICATIONS_ENABLED == 'false') return;
	if(!Capacitor.isNativePlatform() || !Capacitor.isPluginAvailable('PushNotifications')) return;
	try{
		// Los listeners van ANTES de registrar: el token puede llegar enseguida, y el toque que abrio
		// la app desde cerrada se entrega apenas hay alguien escuchando.
		PushNotifications.addListener('registration', token => {
			token_push = token.value;
			acciones.al_registrar(token_push);
		});
		PushNotifications.addListener('registrationError', error => console.warn('No se pudo registrar el push', error));
		// Con la app abierta Android no muestra la push: se la repite como local, con los mismos
		// datos, para que el aviso se vea igual que con la app cerrada.
		PushNotifications.addListener('pushNotificationReceived', push => {
			notificar_evento_local(push.title || 'RavEat', push.body || '', push.data || {});
			acciones.al_recibir();
		});
		PushNotifications.addListener('pushNotificationActionPerformed', accion => acciones.al_tocar(accion.notification.data || {}));

		const permisos = await PushNotifications.requestPermissions();
		if(permisos.receive != 'granted'){
			// Se deja dicho en la consola: sin permiso no hay token, y el push no va a llegar
			// nunca aunque la API lo mande.
			console.warn('Push sin permiso: el dispositivo no se registra en FCM.');
			return;
		}
		await PushNotifications.register();
	}catch(error){
		// Sin Firebase configurado el registro falla: se avisa por consola y la app sigue.
		console.warn('Push no inicializado. Revisá Firebase/google-services.json.', error);
	}
}
