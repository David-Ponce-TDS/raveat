import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';
import { PushNotifications } from '@capacitor/push-notifications';

// Las notificaciones NATIVAS: la local (la programa la propia app) y la push (viaja por Firebase).
// La tercera "notificacion" del modulo, la bandeja, no pasa por aca: es una tabla en la API.
let inicializado = false;

export async function inicializar_notificaciones_nativas(){
	if(inicializado) return;
	inicializado = true;
	await preparar_notificaciones_locales();
	await preparar_push();
}

// Notificacion local: la dispara la app como respuesta a algo que el usuario acaba de hacer.
// No viaja por internet ni necesita servidor — por eso funciona hasta en modo avion.
export async function notificar_evento_local(titulo, mensaje){
	if(!Capacitor.isPluginAvailable('LocalNotifications')) return;
	const permisos = await LocalNotifications.checkPermissions();
	if(permisos.display != 'granted'){
		const solicitados = await LocalNotifications.requestPermissions();
		if(solicitados.display != 'granted') return;
	}
	await LocalNotifications.schedule({
		notifications: [{
			// El id es obligatorio y tiene tope de int32; el resto del timestamp no hace falta.
			id: Date.now() % 2147483647,
			title: titulo,
			body: mensaje,
			schedule: {at: new Date(Date.now() + 250)}
		}]
	});
}

async function preparar_notificaciones_locales(){
	if(!Capacitor.isPluginAvailable('LocalNotifications')) return;
	const permisos = await LocalNotifications.checkPermissions();
	if(permisos.display != 'granted') await LocalNotifications.requestPermissions();
}

// Push: la app SOLO se registra y recibe. El envio no sale de nuestra API: se hace desde la
// consola de Firebase, porque el push necesita un intermediario (FCM) si o si — esa es la
// leccion. Necesita google-services.json (no versionado) y la bandera en el .env; sin ellos,
// este paso se saltea y todo lo demas sigue funcionando.
async function preparar_push(){
	if(import.meta.env.VITE_PUSH_NOTIFICATIONS_ENABLED != 'true') return;
	if(!Capacitor.isNativePlatform() || !Capacitor.isPluginAvailable('PushNotifications')) return;
	try{
		const permisos = await PushNotifications.requestPermissions();
		if(permisos.receive != 'granted') return;
		await PushNotifications.register();
		PushNotifications.addListener('registration', token => console.info('Push token registrado', token.value));
		PushNotifications.addListener('registrationError', error => console.warn('No se pudo registrar el push', error));
	}catch(error){
		// Sin Firebase configurado el registro falla: se avisa por consola y la app sigue.
		console.warn('Push no inicializado. Revisá Firebase/google-services.json.', error);
	}
}
