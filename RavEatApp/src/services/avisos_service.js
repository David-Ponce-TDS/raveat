import { toastController } from '@ionic/vue';

// El aviso corto de la app: el cartelito que confirma que algo salio bien o que fallo. Es el
// tercer canal y el mas barato de los tres del modulo — no es la notificacion local (esa la
// dibuja el sistema operativo y sobrevive a cerrar la app) ni la bandeja (esa es una tabla en la
// API). Este vive dentro de la pantalla y dura dos segundos.
//
// Arriba y no abajo: abajo estan los tabs, y el aviso los tapaba justo cuando el usuario iba a
// tocar el siguiente paso.

export function avisar_exito(mensaje){
	return mostrar(mensaje, 'success', 2000);
}

export function avisar_error(mensaje){
	return mostrar(mensaje, 'danger', 3500);
}

async function mostrar(mensaje, color, duracion){
	const aviso = await toastController.create({
		message: mensaje,
		color,
		duration: duracion,
		position: 'top',
		buttons: [{text: 'OK', role: 'cancel'}]
	});
	await aviso.present();
	return aviso;
}
