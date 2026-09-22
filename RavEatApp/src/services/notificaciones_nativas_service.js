import { Capacitor } from '@capacitor/core';
import { LocalNotifications } from '@capacitor/local-notifications';

// La notificacion LOCAL: la dibuja el sistema operativo del telefono, pero la programa la propia
// app. No sale del dispositivo y no necesita servidor — por eso funciona hasta en modo avion.
// La tercera "notificacion" del modulo, la bandeja, no pasa por aca: es una tabla en la API.
let inicializado = false;

export async function inicializar_notificaciones_nativas(){
	if(inicializado) return;
	inicializado = true;
	await preparar_notificaciones_locales();
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
